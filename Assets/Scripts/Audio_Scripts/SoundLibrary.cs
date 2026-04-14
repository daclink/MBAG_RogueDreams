using UnityEngine;
 
/** 
/// ScriptableObject that acts as the central registry for all sounds in the game.
/// Create via: Right-click in Project > Create > Audio > Sound Library
/// Assign this asset to Sound_Manager in the Inspector.
**/
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    public Sound[] sounds;
 
    /**
     * Returns a sound or null
     */
    public Sound GetSound(string soundName)
    {
        foreach (Sound s in sounds)
        {
            if (s.name == soundName)
                return s;
        }
        Debug.LogWarning($"[SoundLibrary] Sound '{soundName}' not found.");
        return null;
    }
}