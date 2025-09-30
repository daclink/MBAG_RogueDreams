using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Sideview_Controller : MonoBehaviour
{
    private bool enableMovement = true;
    private bool isGrounded;
    private float rayDistance;
    private float horizontalInput;
    private float newVelocityX;

    
    [SerializeField] private float airControlFactor;
    [SerializeField] private float groundControlFactor;
    [SerializeField] private float speed;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float jumpForce;
    [SerializeField] private CapsuleCollider2D playerVisualCollider;
    


    private void Start()
    {
        RoomExits.OnRoomExit += DisableMovement;
        LevelExit.OnLevelExit += DisableMovement;
        rayDistance = playerVisualCollider.bounds.extents.y + .05f;
    }
    
    /**
     * Flips the bool value of enable movement
     */
    private void DisableMovement()
    {
        enableMovement = !enableMovement;
    }

    private void Update()
    {
        isGrounded = IsGrounded();
    }

    private void FixedUpdate()
    {
        if (!enableMovement)
        {
            // If movement is disabled, ensure horizontal velocity is zeroed out.
            // the reason for this is when movement is disabled, there is another script taking over the velocity control (knockback)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float targetSpeed = horizontalInput * speed;
        float currentVelocityX = rb.linearVelocity.x;
        float currentVelocityY = rb.linearVelocity.y;
        
        //if we are in the air...
        if (!isGrounded)
        {
            newVelocityX = Mathf.Lerp(currentVelocityX, targetSpeed, airControlFactor);
        }
        else // if we are on the ground...
        {
            newVelocityX = Mathf.Lerp(currentVelocityX, targetSpeed, groundControlFactor);
        }
        
        rb.linearVelocity = new Vector2(newVelocityX, currentVelocityY);
    }
    
    public void OnPlayerMove(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
    }

    public void OnPlayerJump(InputAction.CallbackContext context)
    {
        if (!enableMovement || !isGrounded)
        {
            return;
        }
        
        if (context.performed)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        
    }
    
    private bool IsGrounded()
    {
        //raycast to see if player is touching the ground
        return Physics2D.Raycast(transform.position, Vector2.down, rayDistance, LayerMask.GetMask("Ground"));

    }
    
    public void OnDestroy()
    {
        RoomExits.OnRoomExit -= DisableMovement;
        LevelExit.OnLevelExit -= DisableMovement;
    }
}
