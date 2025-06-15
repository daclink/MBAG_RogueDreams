using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public delegate void CameraInstantiated();
    public static event CameraInstantiated OnCameraInstantiated;
    
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject playerPrefab;
    
    void Start()
    {
        Instantiate(mainCamera, new Vector3(-10, 23, -10), Quaternion.identity);
        Instantiate(playerPrefab, new Vector3(-10, 23, 0), Quaternion.identity);
        OnCameraInstantiated?.Invoke();
        
    }
}
