using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;



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
    public static Sound_Manager Instance { get; private set; }

    [Header("Sound Library")]
    [SerializeField] private SoundLibrary soundLibrary;
    
    [Header("AudioSource Pool")]
    [SerializeField] private int poolSize = 10;
    
    // Pool of reusable AudioSources for one-shot sounds
    private Queue<AudioSource> sourcePool = new Queue<AudioSource>();

    // Per-category volume multipliers (0-1)
    private Dictionary<SoundCategory, float> categoryVolumes = new Dictionary<SoundCategory, float>
    {
        { SoundCategory.SFX,      1f },
        { SoundCategory.Music,    1f },
        { SoundCategory.Ambience, 1f },
        { SoundCategory.UI,       1f }
    };
    
    // Active fade coroutines keyed by sound name (prevents double-fade)
    private Dictionary<string, Coroutine> _activeFades = new Dictionary<string, Coroutine>();
    
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

        InitializeSounds();
        InitializePool();
    }
    
    /**
     * This creates an AudioSource for each looping sound.
     * One-Shot sounds use the audioPool
     */
    private void InitializeSounds()
    {
        if (soundLibrary == null)
        {
            Debug.LogError("[Sound_Manager] No SoundLibrary assigned!");
            return;
        }
 
        foreach (Sound s in soundLibrary.sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.GetVolume();
            s.source.pitch = s.GetPitch();
            s.source.loop = s.loop;
 
            if (s.mixerGroup != null)
                s.source.outputAudioMixerGroup = s.mixerGroup;
        }
    }
    
    /**
     * Prepares the audioPool for one-shot sounds
     */
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sourcePool.Enqueue(src);
        }
    }
    

    //-----------------------------------------------------------
    
    /**
     * Used to Play an audio clip
     */
    public void Play(string soundName)
    {
        Sound s = soundLibrary.GetSound(soundName);
        if (s == null) return;
 
        float categoryVol = categoryVolumes[s.category];
        s.source.volume = s.GetVolume() * categoryVol;
        s.source.pitch  = s.GetPitch();
        s.source.Play();
    }

    /// <summary>
    /// Plays a sound as a one-shot — supports overlapping calls (e.g. footsteps).
    /// Borrows from the pool and returns the source when done.
    /// </summary>
    /**
     * This plays a one-shot sound and supports overlapping calls.
     * This also utilizes the audio pool and returns the audio source when it is done
     */
    public void PlayOneShot(string soundName)
    {
        Sound s = soundLibrary.GetSound(soundName);
        if (s == null) return;
 
        if (sourcePool.Count == 0)
        {
            Debug.LogWarning("[Sound_Manager] Pool exhausted — increasee pool size.");
            return;
        }
 
        AudioSource src = sourcePool.Dequeue();
        src.clip    = s.clip;
        src.volume  = s.GetVolume() * categoryVolumes[s.category];
        src.pitch   = s.GetPitch();
        src.loop    = false;
 
        if (s.mixerGroup != null)
            src.outputAudioMixerGroup = s.mixerGroup;
 
        src.Play();
        StartCoroutine(ReturnToPool(src, s.clip.length));
    }
    
    /**
     * Immediately stops playing a sound
     */
    public void Stop(string soundName)
    {
        Sound s = soundLibrary.GetSound(soundName);
        if (s == null) return;
        s.source.Stop();
    }  

    /**
     * This fades a sound in, from 0 to set volume, over a set number of seconds (duration)
     */
    public void FadeIn(string soundName, float duration)
    {
        Sound s = soundLibrary.GetSound(soundName);
        if (s == null) return;
 
        CancelActiveFade(soundName);
 
        float targetVolume = s.GetVolume() * categoryVolumes[s.category];
        s.source.volume = 0f;
        s.source.Play();
 
        Coroutine c = StartCoroutine(FadeRoutine(s.source, 0f, targetVolume, duration));
        _activeFades[soundName] = c;
    }
 
    /**
     * This fades a sound out from its current volume to 0 over a set duration
     */
    public void FadeOut(string soundName, float duration)
    {
        Sound s = soundLibrary.GetSound(soundName);
        if (s == null) return;
 
        CancelActiveFade(soundName);
 
        Coroutine c = StartCoroutine(FadeOutAndStop(s, duration));
        _activeFades[soundName] = c;
    }
 
    /**
     * This fades one sound into another sound over a set number of seconds.
     * This will be useful for switching songs or even fading in a death song over the background music for the current biome
     */
    public void Crossfade(string fromSound, string toSound, float duration)
    {
        FadeOut(fromSound, duration);
        FadeIn(toSound, duration);
    }
    
    /**
     * Sets the volume multiplier for a specific category and takes affect immediately
     */
    public void SetCategoryVolume(SoundCategory category, float volume)
    {
        volume = Mathf.Clamp01(volume);
        categoryVolumes[category] = volume;
 
        foreach (Sound s in soundLibrary.sounds)
        {
            if (s.category == category && s.source != null)
                s.source.volume = s.volume * volume;
        }
    }
 
    /**
     * Sets the overall volume of all categories
     */
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        foreach (Sound s in soundLibrary.sounds)
        {
            if (s.source != null)
                s.source.volume = s.volume * volume;
        }
    }
 
    /**
     * Checks if a sound is currently playing or not.
     */
    public bool IsPlaying(string soundName)
    {
        Sound s = soundLibrary.GetSound(soundName);
        return s?.source != null && s.source.isPlaying;
    }
    
    // --------------- COROUTINES AND HELPERS -------------------
    
    private IEnumerator FadeRoutine(AudioSource src, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        src.volume = to;
    }
 
    private IEnumerator FadeOutAndStop(Sound s, float duration)
    {
        float startVolume = s.source.volume;
        yield return FadeRoutine(s.source, startVolume, 0f, duration);
        s.source.Stop();
    }
 
    private IEnumerator ReturnToPool(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        src.clip = null;
        sourcePool.Enqueue(src);
    }
 
    private void CancelActiveFade(string soundName)
    {
        if (_activeFades.TryGetValue(soundName, out Coroutine existing))
        {
            if (existing != null) StopCoroutine(existing);
            _activeFades.Remove(soundName);
        }
    }
    
    
}