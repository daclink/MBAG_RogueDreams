using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider musicSliderV;
    [SerializeField] private Slider musicSliderP;
    [SerializeField] private Slider musicSliderT;

    public void SetMusicVolume()
    {
        float volume = musicSliderV.value;
        myMixer.SetFloat("Volume", volume);
        //musicSource.volume = volume;
    }

    public void SetPitch()
    {
        float pitch = musicSliderP.value;
        myMixer.SetFloat("Pitch", pitch);
        //musicSource.pitch = pitch;
    }

    public void SetTempo()
    {
        float tempo =musicSliderT.value;
        musicSource.pitch = tempo;
    }
}