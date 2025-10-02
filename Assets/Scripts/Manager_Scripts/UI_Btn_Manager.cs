using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_Btn_Manager : MonoBehaviour
{
    // Singleton object
    private static UI_Btn_Manager instance;
    public static UI_Btn_Manager Instance { get {return instance; } }

    // Events for menu button presses
    public delegate void NewGamePress();
    public static event NewGamePress OnNewGamePress;

    public delegate void LoadGamePress();
    public static event LoadGamePress OnLoadGamePress;


    void Start()
    {
        // Singleton pattern for the UI Button Manager
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
    
    // -------------------------------------- Main Menu Buttons -----------------------------------//
    
    /**
     * Fires the new game button press event to the GameManager script
     */
    public void NewGame()
    {
        Debug.Log("New Game");
        OnNewGamePress?.Invoke();
    }

    /**
     * Fires the load game button press script to the GameManager script
     */
    public void LoadGame()
    {
        Debug.Log("Load Game");
        OnLoadGamePress?.Invoke();
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }
    
    /**
     * When pause button is clicked, open the pause menu using the pause managers pauseGame function
     */
    private void OpenPauseMenu()
    {
        PauseManager.Instance.PauseGame();
    }

    /**
     * DEPRICATED
     * When pressing the menu button in the main menu, call the pause game function in the
     * pause manager and display the pause menu in the main menu scene
     */
    public void MainMenu()
    {
        Debug.Log("Main Menu");
        OpenPauseMenu();
    }
    
    // -------------------------------------- Pause Menu -----------------------------------//
    public void ExitPauseMenu()
    {
        ClosePauseMenu();
    }

    private void ClosePauseMenu()
    {
        PauseManager.Instance.ResumeGame();
    }
}
