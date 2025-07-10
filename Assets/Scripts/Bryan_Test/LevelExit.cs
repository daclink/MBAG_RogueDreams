using System;
using UnityEngine;

public class LevelExit : MonoBehaviour
{

    public delegate void LevelExitEvent();
    public static event LevelExitEvent OnLevelExit;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Collision object tag " + collision.gameObject.tag);
        if(!collision.CompareTag("Player"))return;

        OnLevelExit?.Invoke();
        // add a cool effect such as a camera fade to black while going to the next level
        
    }
}
