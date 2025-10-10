using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    // Singleton object
    private static PauseManager instance;
    public static PauseManager Instance { get {return instance; } }
    
    [Header("Pause Prefabs")]
    [SerializeField] private GameObject optionsMenu;
    
    private PauseAction action;
    private bool paused = false;
    private bool gamePausable = false;

    private void Awake()
    {
        action = new PauseAction();
    }

    /**
     * Enables the pause
     */
    private void OnEnable()
    {
        action.Enable();
    }

    /**
     * Disables the pause
     */
    private void OnDisable()
    {
        action.Disable();
    }

    private void Start()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        
        int startScene = SceneManager.GetActiveScene().buildIndex;
        
        if(startScene != 0) gamePausable = true;
        
        // Singleton instance pattern for PauseManager
        if (instance != null && instance != this)
        {
            Debug.Log("Destroying duplicate menu object.");
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        action.Pause.PauseGame.performed += _ => DeterminePause();
    }

    /**
     * Handles custom logic for when the scene changes to set a bool
     */
    private void OnSceneChanged(Scene curr, Scene next)
    {
        // Check if the current scene is a pausable scene
        if (next.buildIndex != 0)
        {
            gamePausable = true;
        }
        else
        {
            gamePausable = false;
        }
    }
    
    /**
     * Used to find out if the game should be paused or playing currently
     */
    private void DeterminePause()
    {
        if (paused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    /*
     * When the game is paused, freeze the time and audio and activate the pause menu overlay
     */
    public void PauseGame()
    {
        if (!gamePausable) return;
        
        Debug.Log("pause manager: Game Paused");
        Time.timeScale = 0;
        AudioListener.pause = true;
        paused = true;
        optionsMenu.SetActive(true);
    }

    /**
     * When the game is resumed, unfreeze the time and audio and deactivate the pause menu overlay
     */
    public void ResumeGame()
    {
        if (!gamePausable) return;
        
        Debug.Log("Game Resumed");
        Time.timeScale = 1;
        AudioListener.pause = false;
        paused = false;
        optionsMenu.SetActive(false);
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
}
