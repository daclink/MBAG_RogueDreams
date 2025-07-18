using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Windows.WebCam;

public class RoomExits : MonoBehaviour
{
    public delegate void RoomExit();
    public static event RoomExit OnRoomExit;
    
    [SerializeField] private Vector2 cameraPanDistance;
    [SerializeField] private float cameraLerpDuration;
    private Camera mainCamera;
    private Vector3 oldCameraPosition;
    private Vector3 newCameraPosition;
    private float timeElapsed = 0f;
    private bool lerpInProgress = false;
    
    [SerializeField] private Vector2 newPlayerPosition;
    [SerializeField] private Vector2 oldPlayerPosition;

    void Start()
    {
        GameManager.OnCameraInstantiated += FindCamera;
    }

    private void FindCamera()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        //brute force fix, camera not finding immediately
        if (mainCamera == null)
        {
            FindCamera();
        }
        
        // --------------- CAMERA LERP ----------------
        if (lerpInProgress)
        {   
            //keep track of elapsed time for camera panning
            timeElapsed += Time.deltaTime;
            //t is the amount the camera should move for interpolation each frame
            float t = timeElapsed / cameraLerpDuration;
            //lerp the cameras position
            mainCamera.gameObject.transform.position = Vector3.Lerp(oldCameraPosition, newCameraPosition, t);
            
            if (timeElapsed >= cameraLerpDuration)
            {
                //once lerp is done, reset lerp variables
                timeElapsed = 0f;
                lerpInProgress = false;
                OnRoomExit?.Invoke();
            }
        }

        // --------------------------------------------
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if not collided with the player then return
        if(!collision.CompareTag("Player"))return;

        if (mainCamera == null) return;
        
        //this event disables and enables player movement
        OnRoomExit?.Invoke();
        //move the player into the next room
        MovePlayer(collision.gameObject);
        //lerp the camera into the next room
        LerpCamera();
    }

    private void MovePlayer(GameObject player)
    {
        oldPlayerPosition = player.transform.position;
        player.transform.position = new Vector2(oldPlayerPosition.x + newPlayerPosition.x, oldPlayerPosition.y + newPlayerPosition.y);
    }

    private void LerpCamera()
    {
        oldCameraPosition = mainCamera.transform.position;
        newCameraPosition = new Vector3(cameraPanDistance.x + oldCameraPosition.x, cameraPanDistance.y + oldCameraPosition.y, oldCameraPosition.z);
        lerpInProgress = true;
    }
}
