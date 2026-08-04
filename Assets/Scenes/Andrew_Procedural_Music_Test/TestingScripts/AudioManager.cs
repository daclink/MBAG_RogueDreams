using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("--Audio Source--")]
    [SerializeField] private AudioSource musicSource;
    
    [Header("--Audio Clip--")]
    public AudioClip music;

    private void Start()
    {
        musicSource.clip = music;
        musicSource.Play();
    }
}
