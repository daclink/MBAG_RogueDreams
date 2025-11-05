using System;
using UnityEngine;

public class Central_Sound_Script : MonoBehaviour
{
    [SerializeField] private Sound_Manager soundManager;
    
    /**
     * Listen to all scripts that will play a sound through events
     */
    private void Start()
    {
        Player_Sideview_Controller.OnJump += JumpSound;
        Player_Sideview_Controller.OnWalk += WalkSound;
        Player_Sideview_Controller.OnStopWalk += StopWalkSound;
    }
    
    // Below: For each event subscribed to, add a method to play specific sounds
    private void JumpSound()
    {
        Debug.Log("Playing the player jump sound");
        soundManager.Play("Player_Jump");
    }

    private void WalkSound()
    {
        Debug.Log("Playing the player walking sound");
        soundManager.Play("Player_Walking");
    }

    private void StopWalkSound()
    {
        soundManager.Stop("Player_Walking");
    }

    private void OnDestroy()
    {
        Player_Sideview_Controller.OnJump -= JumpSound;
        Player_Sideview_Controller.OnWalk -= WalkSound;
    }
}
