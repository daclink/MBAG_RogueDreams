using UnityEngine;
using UnityEngine.Audio;
using System;


/**
 * This is the audiomanager class. This utilizes the sound class and stores an array
 * of all the audioclips to be used within the game
 *
 * In order to play a sound in another script, just create an audioManager variable and
 * call .Play("soundName") in the script to play a sound.
 *
 *
 */
public class Sound_Manager : MonoBehaviour
{
    public static Sound_Manager Instance;
    
    [SerializeField] private AudioSource audioSource;
    
    public Sound[] sounds;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            // s.source = audioSource;
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    private void Start()
    {
        //mark as don't destroy on load for the audioManager gameobject
    }

    /**
     * Used to Play an audio clip
     */
    public void Play(string soundName)
    {
        Sound s = Array.Find(sounds, s => s.name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
            return;
        }
        Debug.Log("Playing sound: " + soundName);
        s.source.Play();
    }

    public void Stop(string soundName)
    {
        Sound s = Array.Find(sounds, s => s.name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + soundName + " not found!");
        }
        Debug.Log("Stop sound: " + soundName);
        s.source.Stop();
    }  

    public void SetVolume(float volume)
    {
        foreach (Sound s in sounds)
        {
            s.source.volume = volume;
        }
    }
    
    
}