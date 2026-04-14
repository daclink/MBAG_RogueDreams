using UnityEngine;
using UnityEngine.Audio;

public enum SoundCategory
{
    SFX,
    Music,
    Ambience,
    UI
}


[System.Serializable]
public class Sound
{

    public string name;
    public SoundCategory category;
    public AudioClip clip;
    
    // Variance values are added to the corresponding variable at run time
    // Variance will be useful if we want to randomize the volume of certain sounds.
    // For example, if there are 2 of the same enemies in a room, we can randomize pitchVariance and volumeVariance to make them stand out from eachother.
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float volumeVariance = 0f;
    
    [Range(.1f, 3f)] public float pitch = 1f;
    [Range(0f, 1f)] public float pitchVariance = 0f;

    public bool loop = false;
    public AudioMixerGroup mixerGroup;

    [HideInInspector] public AudioSource source;
    
    //------------------- GETTERS ---------------------
    public float GetVolume()
    {
        return volume + volumeVariance;
    }

    public float GetPitch()
    {
        return pitch + pitchVariance;
    }
    
}