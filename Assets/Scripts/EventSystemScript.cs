using UnityEngine;

public class EventSystemScript : MonoBehaviour
{
    private static EventSystemScript instance;
    public static EventSystemScript Instance { get {return instance; } }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
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
    
}
