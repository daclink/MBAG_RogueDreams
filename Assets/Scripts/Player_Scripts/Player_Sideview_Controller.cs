using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Sideview_Controller : MonoBehaviour
{
    public delegate void JumpSound();
    public static event JumpSound OnJump;
    
    public delegate void WalkSound();
    public static event WalkSound OnWalk;

    public delegate void StopWalkSound();
    public static event StopWalkSound OnStopWalk;
    
    private bool enableMovement = true;
    private bool isGrounded;
    private float rayDistance;
    private float horizontalInput;
    private float newVelocityX;
    private float rotationValue;
    private SpriteRenderer spriteRenderer;
    private bool isFacingRight = true;
    private Animator animator;
    private bool isWalking;
    
    [Header("Player Side View Settings")]
    [SerializeField] private float airControlFactor;
    [SerializeField] private float groundControlFactor;
    [SerializeField] private float speed;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float jumpForce;
    [SerializeField] private CapsuleCollider2D playerVisualCollider;
    
    
    private void Start()
    {
        rotationValue = 0;
        RoomExits.OnRoomExit += DisableMovement;
        LevelExit.OnLevelExit += DisableMovement;
        rayDistance = playerVisualCollider.bounds.extents.y + .05f;
        Transform visualTransform = transform.Find("PlayerVisual");
        animator = visualTransform.GetComponent<Animator>();
        spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();
    }
    
    private void DisableMovement()
    {
        enableMovement = !enableMovement;
    }
    
    /**
     * Needs to check if the player is on the ground every frame
     */
    private void Update()
    {
        isGrounded = IsGrounded();
        UpdateAnimator();
        UpdateSpriteDirection();
    }

    private void FixedUpdate()
    {
        if (!enableMovement)
        {
            // If movement is disabled, ensure horizontal velocity is zeroed out.
            // The reason for this is when movement is disabled, there is another script taking over the velocity control (knockback)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float targetSpeed = horizontalInput * speed;
        float currentVelocityX = rb.linearVelocity.x;
        float currentVelocityY = rb.linearVelocity.y;
        
        // If we are in the air...
        if (!isGrounded)
        {
            //stop playing the walking sound and set isWalking to false
            if (isWalking)
            {
                // Debug.Log("Player jumped, isWalking set to false");
                isWalking = false;
                OnStopWalk?.Invoke();
            }
            newVelocityX = Mathf.Lerp(currentVelocityX, targetSpeed, airControlFactor);
        }
        else // If we are on the ground...
        {
            //  if horizontal velocity.abs > 0.01 && isWalking is false, play the sound
            if (Math.Abs(horizontalInput) > 0.1 && !isWalking)
            {
                // Debug.Log("Player started walking, isWalking set to true");
                isWalking = true;
                OnWalk?.Invoke();
            } 
            else if (Math.Abs(horizontalInput) < 0.1 && isWalking)
            {
                // Debug.Log("Player Stopped Walking, isWalking set to false");
                //stop playing the walking sound and set isWalking to false
                isWalking = false;
                OnStopWalk?.Invoke();
            }
            
            newVelocityX = Mathf.Lerp(currentVelocityX, targetSpeed, groundControlFactor);
        }
        
        rb.linearVelocity = new Vector2(newVelocityX, currentVelocityY);
    }
    
    /**
     * Sets the animator bool for isWalking accordingly
     */
    private void UpdateAnimator()
    {
        bool isWalking = Mathf.Abs(horizontalInput) > 0.01f && enableMovement;
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsGrounded", isGrounded);
    }

    /**
     * This method determines when to flip the sprite
     */
    private void UpdateSpriteDirection()
    {
        if (horizontalInput < -0.01f && isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput > 0.01f && !isFacingRight)
        {
            Flip();
        }
    }

    /**
     * This method flips the sprite and sets the facing bool cariable
     */
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = !isFacingRight;
    }
    
    /**
     * New input system function
     */
    public void OnPlayerMove(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
        // OnWalk?.Invoke();
    }

    /**
     * New input system function
     */
    public void OnPlayerJump(InputAction.CallbackContext context)
    {
        if (!enableMovement || !isGrounded)
        {
            return;
        }
        
        if (context.performed)
        {
            // Play jump sound here and trigger jump animation here
            OnJump?.Invoke();
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
    
    /**
     * Uses a raycast to see if the player is touching the ground
     */
    private bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, rayDistance, LayerMask.GetMask("Ground"));
    }
    
    public void OnDestroy()
    {
        RoomExits.OnRoomExit -= DisableMovement;
        LevelExit.OnLevelExit -= DisableMovement;
    }
}
