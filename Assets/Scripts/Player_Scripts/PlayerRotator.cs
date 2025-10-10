using System;
using UnityEngine;
using UnityEngine.InputSystem;


/**
 * Inherits Rotator class
 */
public class PlayerRotator : Rotator
{
    [Header("Player Rotator Objects")]
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject objectToRotate;

    void Start()
    {
        cam = Camera.main;
    }
    
    /**
     * New Input System function, just takes in mouse position as a Vector 2
     */
    public void OnPlayerLook(InputAction.CallbackContext context)
    {
        if (cam == null)
        {
            cam = Camera.main;
            return;
        }
        
        Vector2 mousePos = cam.ScreenToWorldPoint(context.ReadValue<Vector2>());
        LookAt(objectToRotate, mousePos);
    }
}
