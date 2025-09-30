using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Inherits Rotator class
/// </summary>
public class PlayerRotator : Rotator
{
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject objectToRotate;

    void Start()
    {
        cam = Camera.main;
    }
    /// <summary>
    /// New Input System function, just takes in mouse position as a Vector 2
    /// </summary>
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
