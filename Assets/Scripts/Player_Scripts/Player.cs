using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    
    
    private const string ITEM_TAG = "Item";
    
    [SerializeField] private float speed;
    [SerializeField] private Vector2 moveDir;
    [SerializeField] private bool isMeleeing = false;
    [SerializeField] private GameObject meleeArea;
    [SerializeField] private BaseItem collectedItem;
    
    
    // [SerializeField] private int health = 10;
    // [SerializeField] private HealthText healthText;

    private bool canMove = true;

    private float timeToMelee = .25f;
    private float meleeTimer = 0f;

    private void Start()
    {
        meleeArea.SetActive(false);
        RoomExits.OnRoomExit += DisableMovement;
        LevelExit.OnLevelExit += DisableMovement;
        
        // healthText.UpdateHealth(health);
    }

    private void Update()
    {
        if (!canMove) return;
        Move();

        if (isMeleeing)
        {
            meleeTimer += Time.deltaTime;

            if (meleeTimer >= timeToMelee)
            {
                meleeTimer = 0;
                isMeleeing = false;
                meleeArea.SetActive(isMeleeing);
            }
        }
    }

    private void DisableMovement()
    {
        canMove = !canMove;
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

    // public void AddHealth(int heal)
    // {
    //     health += heal;
    //     healthText.UpdateHealth(health);
    // }


    public void OnDestroy()
    {
        RoomExits.OnRoomExit -= DisableMovement;
        LevelExit.OnLevelExit -= DisableMovement;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.transform.TryGetComponent(out BaseItem item))
        {
            // set collectedItem to anything that inherits the item base class
            collectedItem = item;
            // call collectedItem.Collect(this)
            collectedItem.Collect(this);
            // reset collectedItem back to null
            collectedItem = null;
        }
    }
}
