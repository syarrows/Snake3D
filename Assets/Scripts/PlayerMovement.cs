using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    private Transform apple;
    private Rigidbody player;
    private Transform movePoint;
    private GameObject playerObj;
    private GameObject boundingBox;
    private Vector3Int lastGridPosition;
    private const int startingSnakeLength = 5;
    private const float lateralMovementSpeed = 1.8f;
    private int Score = 0;
    private Directions directionToGo;
    private List<TrailingSphere> prevPositions;
    
    private Vector3 currPlayerPosition => player.position;
    private Vector3 currTransformPosition => transform.position;
    private Vector3Int currPositionInt => Vector3Int.RoundToInt(currPlayerPosition);
    private float distanceToMovePoint => Vector3.Distance(currPlayerPosition, movePoint.position);

    private IEnumerable<Vector3Int> targetPositions => prevPositions.Select(y => y.targetPosition);
    private bool EatingYourself => targetPositions.Contains(currPositionInt);
    private bool OutsideBoundingBox => (currPositionInt.x > 9 || currPositionInt.x < 0) || (currPositionInt.y > 9 || currPositionInt.y < 1) || (currPositionInt.z > 9 || currPositionInt.z < 0);
    private Camera mainCamera;
    private Camera mountedCam1;
    private Camera mountedCam2;

    void Start()
    {
        movePoint = (new GameObject()).transform;
        movePoint.parent = null;

        playerObj = GameObject.Find("Player");
        player = playerObj.GetComponent<Rigidbody>();
        apple = GameObject.Find("Apple").transform;
        boundingBox = GameObject.Find("BoundingBox");

        mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
        mountedCam1 = GameObject.Find("MountedCam1").GetComponent<Camera>();
        mountedCam2 = GameObject.Find("MountedCam2").GetComponent<Camera>();

        mainCamera.enabled = true;
        mountedCam1.enabled = true;
        mountedCam2.enabled = true;

        Instantiate();
        apple.position = NewApplePos();
    }

    void Update()
    {
        if(distanceToMovePoint == 0f){
            if(EatingYourself || OutsideBoundingBox){
                Die();
                return;
            }

            switch(directionToGo){
                case Directions.Up:
                    transform.eulerAngles = Vector3Int.RoundToInt(transform.eulerAngles + new Vector3(-90, 0, 0));
                    break;
                case Directions.Down:
                    transform.eulerAngles = Vector3Int.RoundToInt(transform.eulerAngles + new Vector3(90f, 0, 0));
                    break;
                case Directions.Left:
                    transform.eulerAngles = Vector3Int.RoundToInt(transform.eulerAngles + new Vector3(0, -90, 0));
                    break;
                case Directions.Right:
                    transform.eulerAngles = Vector3Int.RoundToInt(transform.eulerAngles + new Vector3(0, 90, 0));
                    break;
            }

            CascadeTargetLocations();
            directionToGo = Directions.Forward;

            if(currPlayerPosition == apple.position){
                AddNewTrailingSphere();
                apple.position = NewApplePos();
                ChangeScore(Score + 1);
            }

        } else {
            if(Input.GetKeyDown(KeyCode.W)){
                directionToGo = Directions.Up;
            } else if(Input.GetKeyDown(KeyCode.S)){
                directionToGo = Directions.Down;
            } else if(Input.GetKeyDown(KeyCode.A)){
                directionToGo = Directions.Left;
            } else if(Input.GetKeyDown(KeyCode.D)){
                directionToGo = Directions.Right;
            }

            if(Input.GetKeyDown(KeyCode.Escape)){
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
            }
        }

        UpdatePlayerVector();
    }

    private void UpdatePlayerVector()
    {
        player.position = Vector3.MoveTowards(currPlayerPosition, movePoint.position, lateralMovementSpeed * Time.deltaTime);

        foreach(TrailingSphere ts in prevPositions){
            ts.SetPosition(lateralMovementSpeed);
        }
    }

    private void CascadeTargetLocations()
    {
        for(int i = prevPositions.Count - 1; i > 0; i--){
            prevPositions[i].targetPosition = prevPositions[i-1].targetPosition;
        }
        
        lastGridPosition = Vector3Int.RoundToInt(currPlayerPosition);
        prevPositions[0].targetPosition = lastGridPosition;
        movePoint.position = currPlayerPosition + transform.forward;
    }

    private void AddNewTrailingSphere()
    {
        if(prevPositions.Count == 0){
            prevPositions.Add(new TrailingSphere(Vector3Int.RoundToInt(currTransformPosition), Vector3Int.RoundToInt(currTransformPosition)));
        } else {
            TrailingSphere currLast = prevPositions[prevPositions.Count - 1];
            prevPositions.Add(new TrailingSphere(Vector3Int.RoundToInt(currLast.Sphere.transform.position), currLast.targetPosition));
        }
    }

    private Vector3Int NewApplePos()
    {
        List<Vector3Int> allPossiblePositions = new List<Vector3Int>();
        
        for(int x = 1; x < 10; x++){
            for(int y = 1; y < 10; y++){
                for(int z = 1; z < 10; z++){
                    allPossiblePositions.Add(new Vector3Int(x,y,z));
                }
            }
        }

        return allPossiblePositions.Random(x => !targetPositions.Contains(x));
    }

    private void InstantiateTail()
    {
        for(int i = 1; i <= startingSnakeLength; i++){
            AddNewTrailingSphere();
        }
    }

    private void Die()
    {
        Debug.Log("You died");
        prevPositions.ToList().ForEach(x => Object.Destroy(x.Sphere));
        transform.position = new Vector3(2, 1, 2);
        transform.eulerAngles = new Vector3(0, 0, 0);
        ChangeScore(0);

        Instantiate();
    }

    private void Instantiate()
    {
        directionToGo = Directions.Forward;
        prevPositions = new List<TrailingSphere>();
        movePoint.position = currTransformPosition + new Vector3(0, 0, 1);
        lastGridPosition = Vector3Int.RoundToInt(currPlayerPosition);
        InstantiateTail();
    }

    private void ChangeScore(int newValue)
    {
        Score = newValue;
        GameObject.Find("Score").GetComponent<TMPro.TextMeshProUGUI>().text = Score.ToString();
    }
}

public static class IEnumerableExtensions
{
    public static T Random<T>(this IEnumerable<T> input)
    {
        return input.ElementAt((new System.Random()).Next(input.Count()));
    }

    public static T Random<T>(this IEnumerable<T> input, System.Func<T, bool> func)
    {
        return input.Where(func).Random();
    }
}


public enum Directions
{
    Up,
    Down,
    Left,
    Right,
    Forward
}

public class TrailingSphere
{
    private GameObject sphere;
    public Vector3Int targetPosition { get; set;}
    public GameObject Sphere => sphere;

    public TrailingSphere(Vector3Int pos, Vector3Int _targetPosition)
    {
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = pos;
        targetPosition = _targetPosition;
    }

    public void SetPosition(float lateralMovementSpeed) => Sphere.transform.position = Vector3.MoveTowards(Sphere.transform.position, targetPosition, lateralMovementSpeed * Time.deltaTime);
}