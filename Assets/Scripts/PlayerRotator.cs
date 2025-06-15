using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Inherits Rotator class
/// </summary>
public class PlayerRotator : Rotator
{
    [SerializeField] private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }
    /// <summary>
    /// New Input System function, just takes in mouse position as a Vector 2
    /// </summary>
    public void OnPlayerLook(InputAction.CallbackContext context)
    {
        Vector2 mousePos = cam.ScreenToWorldPoint(context.ReadValue<Vector2>());
        LookAt(mousePos);
    }
}
