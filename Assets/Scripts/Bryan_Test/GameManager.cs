using System.Collections;
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
    [SerializeField] private GameObject rangedEnemyPrefab;
    [SerializeField] private GameObject healingItemPrefab;

    private int sceneNumber;
    
    public int targetFrameRate = 60;

    
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
        
        //temporary for now until save game stuff is setup
        UI_Btn_Manager.OnNewGamePress += LoadNextScene;
        UI_Btn_Manager.OnLoadGamePress += LoadNextScene;
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerHealthManager.OnPlayerDeath += PlayerDeath;

        LoadScene(sceneNumber);
        
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    // public void OpenPauseMenu()
    // {
    //     Debug.Log("Game Manager open pause menu");
    //     PauseManager.Instance.PauseGame();
    // }
    

    void PlayerDeath()
    {
        StartCoroutine(DeathSequence());
    }

    public IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(3f);
        LoadNextScene();
    }

    //loads the next available scene - hard coded for now with just 2 scenes
    private void LoadNextScene()
    {
        if (sceneNumber == 0 || sceneNumber == 1)
        {
            sceneNumber++;
        }
        else
        {
            sceneNumber = 0;
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
            
        }
        if (scene.buildIndex == 1)
        {
            Instantiate(mainCameraPrefab, new Vector3(0, 0, -10), Quaternion.identity);
            Instantiate(playerSideViewPrefab, new Vector3(0, -2.45f, 0), Quaternion.identity);
        } 
        else if (scene.buildIndex == 2)
        {
            Instantiate(UICanvasPrefab, new Vector3(0, 0, -10), Quaternion.identity);
            Instantiate(mainCameraPrefab, new Vector3(-10, 23, -10), Quaternion.identity);
            Instantiate(playerPrefab, new Vector3(-10, 23, 0), Quaternion.identity);
            Instantiate(meleeEnemyPrefab, new Vector3(-5, 23, 0), Quaternion.identity);
            Instantiate(rangedEnemyPrefab, new Vector3(12, 21, 0), Quaternion.identity);
            Instantiate(healingItemPrefab, new Vector3(12, 12, 0), Quaternion.identity);
            OnCameraInstantiated?.Invoke();
        }
    }
    

}
