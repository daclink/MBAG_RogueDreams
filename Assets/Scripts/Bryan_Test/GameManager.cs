using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    
    private static GameManager instance;
    public static GameManager Instance { get {return instance; } }
    
    public delegate void CameraInstantiated();
    public static event CameraInstantiated OnCameraInstantiated;
    
    [SerializeField] private Camera mainCameraPrefab;
    [SerializeField] private GameObject playerPrefab;

    private int sceneNumber;

    
    private void Awake()
    {
        //singleton instance of the gamemanager
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    
    void Start()
    {
        sceneNumber = 0;
        LevelExit.OnLevelExit += LoadNextScene;
        SceneManager.sceneLoaded += OnSceneLoaded;

        LoadScene(sceneNumber);
    }

    private void LoadNextScene()
    {
        if (sceneNumber == 0)
        {
            sceneNumber++;
        }
        else
        {
            sceneNumber--;
        }
        LoadScene(sceneNumber);
        //possibly need the player prefab and the main camera to be singletons
    }

    private void LoadScene(int sceneNumber)
    {
        Debug.Log("Loading scene " + sceneNumber);
        SceneManager.LoadScene(sceneNumber);
    }

    //everytime a scene is loaded, this function will be called
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene " + scene.name + " is loaded");

        if (scene.buildIndex == 0)
        {
            Instantiate(mainCameraPrefab, new Vector3(0, 0, -10), Quaternion.identity);
            Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        } 
        else if (scene.buildIndex == 1)
        {
            Instantiate(mainCameraPrefab, new Vector3(-10, 23, -10), Quaternion.identity);
            Instantiate(playerPrefab, new Vector3(-10, 23, 0), Quaternion.identity);
            OnCameraInstantiated?.Invoke();
        }
    }

}
