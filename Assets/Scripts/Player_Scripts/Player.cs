using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    
    private const string ITEM_TAG = "Item";
    private const string ENEMY_TAG = "Enemy";
    private const string ENEMY_BULLET_TAG = "EnemyBullet";
    
    [SerializeField] private float speed;
    [SerializeField] private Vector2 moveDir;
    [SerializeField] private bool isMeleeing = false;
    [SerializeField] private GameObject meleeArea;
    [SerializeField] private BaseItem collectedItem;
    [SerializeField] private Knockback knockback;
    [SerializeField] private float timeToMelee;
    

    private bool enableMovement = true;
    private float meleeTimer;

    private void Start()
    {
        meleeTimer = 0f;
        meleeArea.SetActive(false);
        RoomExits.OnRoomExit += DisableMovement;
        LevelExit.OnLevelExit += DisableMovement;
    }

    private void Update()
    {
        // Debug.Log("PLAYER Enable Movement: " + enableMovement);

        if (!enableMovement) return;
        Move();

        if (isMeleeing)
        {
            meleeTimer += Time.deltaTime;

            if (meleeTimer >= timeToMelee)
            {
                meleeTimer = 0f;
                isMeleeing = false;
                meleeArea.SetActive(isMeleeing);
            }
        }
    }

    private void DisableMovement()
    {
        enableMovement = !enableMovement;
    }

    public void ExitKnockback()
    {
        enableMovement = true;
    }

    /// <summary>
    /// Handles movement with a Vector2 move direction and a float speed variable
    /// </summary>
    private void Move()
    {
        //store current position
        Vector3 pos = transform.position;
        //create new position to update with
        Vector2 newPos = new Vector2(pos.x + (moveDir.x * speed * Time.deltaTime), pos.y + (moveDir.y * speed * Time.deltaTime));
        //change current position to new position
        transform.position = new Vector3(newPos.x, newPos.y, pos.z);
    }
    
    private void Melee()
    {
        isMeleeing = true;
        meleeArea.SetActive(isMeleeing);
    }

    /// <summary>
    /// New Input system function, reads values from configured input file for movement, WASD to move
    /// </summary>
    public void OnPlayerMove(InputAction.CallbackContext context)
    {
        moveDir = context.ReadValue<Vector2>();
    }
    
    /// <summary>
    /// New Input system function, just calls the Melee function when activated, left click or 'E'
    /// </summary>
    public void OnPlayerMelee(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Melee();
        }
    }
    
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        // Debug.Log("Player hit by : " + other.gameObject.tag);
        
        if (other.transform.TryGetComponent(out BaseItem item))
        {
            // set collectedItem to anything that inherits the item base class
            collectedItem = item;
            // call collectedItem.Collect(this)
            collectedItem.Collect();
            // reset collectedItem back to null
            collectedItem = null;
        }

        //knockback player/this
        //TODO: add tags for things such as projectiles
        if (other.gameObject.CompareTag(ENEMY_TAG) || other.gameObject.CompareTag(ENEMY_BULLET_TAG))
        {
            if (knockback != null && gameObject.activeInHierarchy)
            {
                // Debug.Log("Knockback called on player");
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
