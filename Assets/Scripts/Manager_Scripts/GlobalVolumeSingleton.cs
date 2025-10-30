using UnityEngine;

public class GlobalVolumeSingleton : MonoBehaviour
{
    
    // Singleton object
    private static GlobalVolumeSingleton instance;
    public static GlobalVolumeSingleton Instance { get {return instance; } }

    void Awake()
    {
        // Singleton instance pattern for the GlobalVolume
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
