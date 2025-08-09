using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_Btn_Manager : MonoBehaviour
{
    
    public delegate void NewGamePress();
    public static event NewGamePress OnNewGamePress;

    public delegate void LoadGamePress();
    public static event LoadGamePress OnLoadGamePress;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    public void NewGame()
    {
        Debug.Log("New Game");
        OnNewGamePress?.Invoke();
    }

    public void LoadGame()
    {
        Debug.Log("Load Game");
        OnNewGamePress?.Invoke();
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }

    public void MainMenu()
    {
        Debug.Log("Main Menu");
    }
}
