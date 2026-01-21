using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class Player : MonoBehaviour
{
    public delegate void Attack();
    public static event Attack OnAttack;
    
    private const string ITEM_TAG = "Item";
    private const string ENEMY_TAG = "Enemy";
    private const string ENEMY_BULLET_TAG = "EnemyBullet";
    
    [Header("Player Settings")]
    [SerializeField] private float speed;
    [SerializeField] private Vector2 moveDir;
    [SerializeField] private bool isMeleeing = false;
    [SerializeField] private GameObject meleeArea;
    // [SerializeField] private BaseItem collectedItem;
    [SerializeField] private Knockback knockback;
    // [SerializeField] private float timeToMelee;
    [SerializeField] private Animator animator;

    private bool enableMovement = true;
    // private float meleeTimer;
    private bool isMoving;

    private void Start()
    {
        // meleeTimer = 0f;
        // meleeArea.SetActive(false);
        RoomExits.OnRoomExit += DisableMovement;
        LevelExit.OnLevelExit += DisableMovement;
        BaseItem.OnItemCollected += ItemCollected;
    }

    /**
     * Parse through itemData object to apply stat changes if applicable
     */
    private void ItemCollected(ItemData itemData)
    {
        
    }

    private void Update()
    {
        if (!enableMovement) return;
        Move();
        Animate();

        // Check if the player is meleeing and set the timer
        // if (isMeleeing)
        // {
        //     meleeTimer += Time.deltaTime;
        //
        //     if (meleeTimer >= timeToMelee)
        //     {
        //         meleeTimer = 0f;
        //         isMeleeing = false;
        //         meleeArea.SetActive(isMeleeing);
        //     }
        // }
    }

    private void DisableMovement()
    {
        enableMovement = !enableMovement;
    }

    public void ExitKnockback()
    {
        enableMovement = true;
    }

    /**
     * This method controls the animations between walking and idle
     */
    private void Animate()
    {
        if (moveDir.magnitude < -0.1 || moveDir.magnitude > 0.1)
        {
            isMoving = true;
            if (enableMovement)
            {
                animator.SetFloat("X", moveDir.x);
                animator.SetFloat("Y", moveDir.y);
            }
        }
        else
        {
            isMoving = false;
        }
        animator.SetBool("IsWalking", isMoving);
    }

    
    /**
     * Handles movement with a Vector2 move direction and a float speed variable
     */
    private void Move()
    {
        // Store current position
        Vector3 pos = transform.position;
        // Create new position to update with
        Vector2 newPos = new Vector2(pos.x + (moveDir.x * speed * Time.deltaTime), pos.y + (moveDir.y * speed * Time.deltaTime));
        // Change current position to new position
        transform.position = new Vector3(newPos.x, newPos.y, pos.z);
    }
    
    /**
     * Handles melee when player attacks
     */
    // private void Melee()
    // {
    //     isMeleeing = true;
    //     meleeArea.SetActive(isMeleeing);
    // }
    
    //attack method 

    
    /**
     * New Input system function, reads values from configured input file for movement, WASD to move
     */
    public void OnPlayerMove(InputAction.CallbackContext context)
    {
        moveDir = context.ReadValue<Vector2>();
    }
    
    
    /**
     * New Input system function, just calls the Melee function when activated, left click or 'E'
     */
    public void OnPlayerMelee(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Melee();
            OnAttack?.Invoke();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        // if (other.transform.TryGetComponent(out BaseItem item))
        // {
        //     // Set collectedItem to anything that inherits the item base class
        //     collectedItem = item;
        //     // Call collectedItem.Collect(this)
        //     collectedItem.Collect();
        //     // Reset collectedItem back to null
        //     collectedItem = null;
        // }

        // Knockback player/this
        // TODO: add tags for things such as projectiles
        if (other.gameObject.CompareTag(ENEMY_TAG) || other.gameObject.CompareTag(ENEMY_BULLET_TAG))
        {
            if (knockback != null && gameObject.activeInHierarchy)
            {
                enableMovement = false;
                knockback.KnockbackObject(gameObject, other.gameObject);
            }
        }
    }
    
    public void OnDestroy()
    {
        RoomExits.OnRoomExit -= DisableMovement;
        LevelExit.OnLevelExit -= DisableMovement;
    }
}