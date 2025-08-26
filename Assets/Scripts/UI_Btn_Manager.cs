using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_Btn_Manager : MonoBehaviour
{
    
    private static UI_Btn_Manager instance;
    public static UI_Btn_Manager Instance { get {return instance; } }

    public delegate void NewGamePress();
    public static event NewGamePress OnNewGamePress;

    public delegate void LoadGamePress();
    public static event LoadGamePress OnLoadGamePress;


    void Start()
    {
        //singleton
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
    
    public void NewGame()
    {
        Debug.Log("New Game");
        OnNewGamePress?.Invoke();
    }

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

    //when pressing the menu button in the main menu, call the pause game function in the pause manager and display the pause menu in the main menu scene
    public void MainMenu()
    {
        Debug.Log("Main Menu");
        OpenPauseMenu();
    }

    private void OpenPauseMenu()
    {
        // GameManager.Instance.OpenPauseMenu();
        PauseManager.Instance.PauseGame();
    }
    
    
    // -------------------------------------- Pause Menu -----------------------------------//
    public void ExitPauseMenu()
    {
        ClosePauseMenu();
    }

    private void ClosePauseMenu()
    {
        PauseManager.Instance.ResumeGame();
        //mainMenu.SetActive(true);
    }
    
}
