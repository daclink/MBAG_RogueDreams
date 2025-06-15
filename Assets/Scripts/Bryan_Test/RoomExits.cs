using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class RoomExits : MonoBehaviour
{
    [SerializeField] private Vector2 cameraPanDistance;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector2 newPlayerPosition;
    [SerializeField] private Vector2 oldPlayerPosition;

    void Start()
    {
        CameraManager.OnCameraInstantiated += FindCamera;
    }

    private void FindCamera()
    {
        mainCamera = Camera.main;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag("Player"))return;
        
        //stop player movement and move the player ahead into the next room 
        oldPlayerPosition = collision.gameObject.transform.position;
        
        collision.gameObject.transform.position = new Vector2(oldPlayerPosition.x + newPlayerPosition.x, oldPlayerPosition.y + newPlayerPosition.y);
        
        //interpolate the camera to the next point
        
        Debug.Log("moving camera");
        Vector3 newCameraPosition = mainCamera.transform.position;
        newCameraPosition = new Vector3(cameraPanDistance.x + newCameraPosition.x, cameraPanDistance.y + newCameraPosition.y, newCameraPosition.z);
        mainCamera.transform.position = newCameraPosition;
        
        //
    }
}
