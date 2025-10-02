using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Windows.WebCam;

public class RoomExits : MonoBehaviour
{
    // Event for when exiting a room; used for panning the camera to the next room
    public delegate void RoomExit();
    public static event RoomExit OnRoomExit;
    
    [Header("Camera Panning Settings")]
    [SerializeField] private Vector2 cameraPanDistance;
    [SerializeField] private float cameraLerpDuration;
    [SerializeField] private Vector2 newPlayerPosition;
    [SerializeField] private Vector2 oldPlayerPosition;
    
    private Camera mainCamera;
    private Vector3 oldCameraPosition;
    private Vector3 newCameraPosition;
    private float timeElapsed = 0f;
    private bool lerpInProgress = false;
    
    void Start()
    {
        // Sets the camera object after the camera is instantiated
        GameManager.OnCameraInstantiated += FindCamera;
    }

    /**
     * Finds the main camera in the game and sets it to the mainCamera variable
     */
    private void FindCamera()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Brute force fix, camera not finding immediately
        if (mainCamera == null)
        {
            FindCamera();
            return;
        }
        
        // --------------- CAMERA LERP ----------------
        if (lerpInProgress)
        {   
            // Keep track of elapsed time for camera panning
            timeElapsed += Time.deltaTime;
            // 't' is the amount the camera should move for interpolation each frame
            float t = timeElapsed / cameraLerpDuration;
            // Lerp the cameras position
            mainCamera.gameObject.transform.position = Vector3.Lerp(oldCameraPosition, newCameraPosition, t);
            
            if (timeElapsed >= cameraLerpDuration)
            {
                // Once lerp is done, reset lerp variables
                timeElapsed = 0f;
                lerpInProgress = false;
                OnRoomExit?.Invoke();
            }
        }
        // --------------------------------------------
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If not collided with the player then return
        if(!collision.CompareTag("Player"))return;

        if (mainCamera == null) return;
        
        //This event disables and enables player movement
        OnRoomExit?.Invoke();
        // Move the player into the next room
        MovePlayer(collision.gameObject);
        // Lerp the camera into the next room
        LerpCamera();
    }

    /**
     * This method moves the player into the starting position of the next room
     * This is used when the camera lerps
     */
    private void MovePlayer(GameObject player)
    {
        oldPlayerPosition = player.transform.position;
        player.transform.position = new Vector2(oldPlayerPosition.x + newPlayerPosition.x, oldPlayerPosition.y + newPlayerPosition.y);
    }

    /**
     * This sets the old and new camera variables before the camera lerp occurs
     */
    private void LerpCamera()
    {
        oldCameraPosition = mainCamera.transform.position;
        newCameraPosition = new Vector3(cameraPanDistance.x + oldCameraPosition.x, cameraPanDistance.y + oldCameraPosition.y, oldCameraPosition.z);
        lerpInProgress = true;
    }
}
