using UnityEngine;

public class PauseManager : MonoBehaviour
{
    //[SerializeField] private GameObject optionsMenuPrefab;
    [SerializeField] private GameObject optionsMenu;
    
    private static PauseManager instance;
    public static PauseManager Instance { get {return instance; } }


    
    private PauseAction action;
    private bool paused = false;

    private void Awake()
    {
        action = new PauseAction();
        //optionsMenu = Instantiate(optionsMenuPrefab, transform.position, Quaternion.identity);
        //optionsMenu.SetActive(false);
    }

    private void OnEnable()
    {
        action.Enable();
    }

    private void OnDisable()
    {
        action.Disable();
    }

    private void Start()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("Destroying duplicate menu object.");
            Destroy(gameObject);
        }
        else
        {
            // Debug.Log("Set to dont destroy this object");
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        action.Pause.PauseGame.performed += _ => DeterminePause();
    }

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
    
    public void PauseGame()
    {
        Debug.Log("pause manager: Game Paused");
        Time.timeScale = 0;
        AudioListener.pause = true;
        paused = true;
        optionsMenu.SetActive(true);
        Debug.Log("OptionsMenu Active");
    }

    public void ResumeGame()
    {
        Debug.Log("Game Resumed");
        Time.timeScale = 1;
        AudioListener.pause = false;
        paused = false;
        optionsMenu.SetActive(false);
    }
    
}
