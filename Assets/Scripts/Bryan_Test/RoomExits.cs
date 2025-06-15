using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class RoomExits : MonoBehaviour
{
    [SerializeField] private Vector2 cameraPanDistance;
    [SerializeField] private Camera mainCamera;
    private Vector3 oldCameraPosition;
    [SerializeField] private Vector2 newPlayerPosition;
    [SerializeField] private Vector2 oldPlayerPosition;
    [SerializeField] private float cameraLerpDuration;
    private Vector3 newCameraPosition;
    private float timeElapsed = 0f;
    private bool lerpInProgress = false;

    void Start()
    {
        CameraManager.OnCameraInstantiated += FindCamera;
    }

    private void FindCamera()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (lerpInProgress)
        {
            Debug.Log("Lerp is in progress.");
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / cameraLerpDuration;
            mainCamera.gameObject.transform.position = Vector3.Lerp(oldCameraPosition, newCameraPosition, t);
            Debug.Log("new camera position is : " + mainCamera.gameObject.transform.position);
        }

        if (timeElapsed >= cameraLerpDuration)
        {
            timeElapsed = 0f;
            lerpInProgress = false;
        }
    }
    
    
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag("Player"))return;
        
        //stop player movement and move the player ahead into the next room 
        oldPlayerPosition = collision.gameObject.transform.position;
        
        collision.gameObject.transform.position = new Vector2(oldPlayerPosition.x + newPlayerPosition.x, oldPlayerPosition.y + newPlayerPosition.y);
        
        //interpolate the camera to the next point
        LerpCamera();

        
        
        //
    }

    private void LerpCamera()
    {
        Debug.Log("moving camera");
        oldCameraPosition = mainCamera.transform.position;
        newCameraPosition = new Vector3(cameraPanDistance.x + oldCameraPosition.x, cameraPanDistance.y + oldCameraPosition.y, oldCameraPosition.z);
        lerpInProgress = true;
        
        //interpolate from oldCameraPos to new CameraPos
        //mainCamera.transform.position = newCameraPosition;
    }
}
