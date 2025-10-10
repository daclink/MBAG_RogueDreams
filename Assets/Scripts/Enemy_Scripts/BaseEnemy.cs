using Enemy_Scripts;
using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{

    // Event to damage the player
    public delegate void DamagePlayer(float attackDamage);
    public static event DamagePlayer OnDamagePlayer;
    
    [Header("Enemy Settings")]
    [SerializeField] protected float health;
    [SerializeField] protected float attackDmg;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float agroRange;
    [SerializeField] protected float patrolRange;
    [SerializeField] protected Knockback knockback;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected EnemyState currentState;


    //non-serializable fields
    protected Transform playerTransform;
    protected float distanceToPlayer;
    protected float dmgTaken;
    protected bool isAgroed;
    protected bool enableMovement;
    protected bool playerDead;
    protected float validIdleRange;
    
    // To be used in each child class.
    protected abstract void PostStart();

    /**
     * Initialize globally used variables for the class and child classes and call postStart
     */
    protected virtual void Start()
    {
        // This should assign player correctly assuming the player is spawned properly BEFORE the enemy is
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        
        PostStart();

        // Initially put the enemy in its idle state
        ChangeState(EnemyState.Idle);
        enableMovement = true;
        isAgroed = false;
        validIdleRange = patrolRange + 1f;
    }
    

    /**
     * When exiting knockback, enemies movement needs to be enabled 
     */
    public void ExitKnockback()
    {
        enableMovement = true;
    }

    /**
     * In child classes, use 'protected override void Update()' and call 'base.Update();'
     * at the top of the update
     */
    protected virtual void Update()
    {
        HandleState();
        // If the player transform is not set then keep the enemy idle
        // Otherwise, calculate the distance to the player
        if (playerTransform == null)
        {
            Debug.Log("Player is inactive, keeping state in idle");
            currentState = EnemyState.Idle;
            distanceToPlayer = validIdleRange;
        }
        else
        {
            distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
        }
    }

    /**
     * On collision, damage the player the correct amount and change state to attacking
     */
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnDamagePlayer?.Invoke(attackDmg);
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Attacking);
        }
        
    }

    /**
     * This is for incoming damage to enemies from the player or other sources
     * Updates the enemy state when hit
     */
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // TODO: player bullets/ranged weapons that should knockback enemies include here
        if (other.gameObject.CompareTag("PlayerWeapon"))
        {
            enableMovement = false;
            knockback.KnockbackObject(gameObject, other.gameObject);
            dmgTaken = 2f;
            ChangeState(EnemyState.TakeDamage);
        }
    }
    
    // This is used when the enemy takes damage from the player
    protected virtual void TakeDamage(float dmgAmount)
    {
        health -= dmgAmount;
        if (health <= 0)
        {
            ChangeState(EnemyState.Dead);
            // Add any other code to kill the enemy gameObject
        }
    }

    // Enemy state abstract methods to be implemented
    protected abstract void HandleIdle();
    protected abstract void HandleAgro();
    protected abstract void HandleDead();
    protected abstract void HandleAttacking();
    protected abstract void HandleDamage();
    protected abstract void HandlePatrol();

    /**
     * Used to change the enemy state
     * Make sure to only use this when changing states just for continuity
     */
    public void ChangeState(EnemyState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            #if UNITY_EDITOR
                Debug.Log("State changed to " + newState);
            #endif
        }
    }

    /**
     * Called in the update function
     * Determines which state the enemy is in and calls the appropriate abstract method
     */
    protected void HandleState()
    {
        
        // #if UNITY_EDITOR
        //         Debug.Log("PARENT:State changed to " + currentState);
        // #endif
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Agro:
                HandleAgro();
                break;
            case EnemyState.Attacking:
                HandleAttacking();
                break;
            case EnemyState.Dead:
                HandleDead();
                break;
            case EnemyState.TakeDamage:
                HandleDamage();
                break;
            case EnemyState.Patrol:
                HandlePatrol();
                break;
        }
    }
}
