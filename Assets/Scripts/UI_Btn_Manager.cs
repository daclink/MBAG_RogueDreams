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
