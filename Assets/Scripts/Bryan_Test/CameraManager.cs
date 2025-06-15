using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

public class CameraManager : MonoBehaviour
{
    public delegate void CameraInstantiated();
    public static event CameraInstantiated OnCameraInstantiated;
    [SerializeField] private Camera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instantiate(mainCamera, new Vector3(-10, 23, -10), Quaternion.identity);
        OnCameraInstantiated?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // void OnDestroy()
    // {
    //     Destroy(mainCamera);
    // }
}
