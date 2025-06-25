using System;
using UnityEngine;

public class LevelExit : MonoBehaviour
{

    public delegate void LevelExitEvent();
    public static event LevelExitEvent OnLevelExit;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!collision.CompareTag("Player"))return;
        
        
        Debug.Log("Player exiting level");
        
        //event should disable player movement, currently is not enabling it back
        //could use a different event to handle that as well
        OnLevelExit?.Invoke();
        
        // add a cool effect such as a camera fade to black while going to the next level
        // Scene switching will be handled through the event in a manager script
        
    }
}
