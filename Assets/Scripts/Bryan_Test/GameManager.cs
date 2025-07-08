using TMPro;
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
    [SerializeField] private GameObject playerSideViewPrefab;
    [SerializeField] private Canvas UICanvasPrefab;
    [SerializeField] private GameObject meleeEnemyPrefab;

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
        sceneNumber = SceneManager.GetActiveScene().buildIndex;
        LevelExit.OnLevelExit += LoadNextScene;
        SceneManager.sceneLoaded += OnSceneLoaded;

        LoadScene(sceneNumber);
    }

    //loads the next available scene - hard coded for now with just 2 scenes
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
    }

    //loads a scene based on the passed in index
    private void LoadScene(int sceneNumber)
    {
        SceneManager.LoadScene(sceneNumber);
    }
    
    //everytime a scene is loaded, this function will be called automatically
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        if (scene.buildIndex == 0)
        {
            Instantiate(mainCameraPrefab, new Vector3(0, 0, -10), Quaternion.identity);
            Instantiate(playerSideViewPrefab, new Vector3(0, -2.45f, 0), Quaternion.identity);
        } 
        else if (scene.buildIndex == 1)
        {
            Instantiate(UICanvasPrefab, new Vector3(0, 0, -10), Quaternion.identity);
            Instantiate(mainCameraPrefab, new Vector3(-10, 23, -10), Quaternion.identity);
            Instantiate(playerPrefab, new Vector3(-10, 23, 0), Quaternion.identity);
            Instantiate(meleeEnemyPrefab, new Vector3(-4, 20, 0), Quaternion.identity);
            OnCameraInstantiated?.Invoke();
        }
    }
    

}
