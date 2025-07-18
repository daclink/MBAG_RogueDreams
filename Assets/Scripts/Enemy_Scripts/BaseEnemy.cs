using Enemy_Scripts;
using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{

    //delegates
    public delegate void DamagePlayer(float attackDamage);
    public static event DamagePlayer OnDamagePlayer;
    
    //serializeable fields
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
    
    // to be used in each child class.
    protected abstract void PostStart();

    //initialize globally used variables for the class and child classes and call postStart
    protected virtual void Start()
    {
        //this should assign player correctly assuming the player is spawned properly BEFORE the enemy is
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        
        PostStart();

        ChangeState(EnemyState.Idle);
        enableMovement = true;
        isAgroed = false;
    }

    // when exiting knockback, enemies movement needs to be put back to true
    public void ExitKnockback()
    {
        enableMovement = true;
    }

    // in child classes, use 'protected override void Update()' and call 'base.Update();'
    // at the top of the update
    protected virtual void Update()
    {
        HandleState();
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

    //this is for incoming damage to enemies from the player or other sources
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // TODO: player bullets/ranged weapons that should knockback enemies include here
        if (other.gameObject.CompareTag("PlayerWeapon"))
        {
            enableMovement = false;
            
            Debug.Log("HIT BY PLAYER WEAPON");
            knockback.KnockbackObject(gameObject, other.gameObject);
            dmgTaken = 2f;
            ChangeState(EnemyState.TakeDamage);
        }
    }
    
    //this is used when the enemy takes damage from the player
    protected virtual void TakeDamage(float dmgAmount)
    {
        health -= dmgAmount;
        if (health <= 0)
        {
            Debug.Log("Enemy is dead");
            ChangeState(EnemyState.Dead);
            //any other code to kill the enemy gameObject
        }
    }

    // enemy state abstract methods
    protected abstract void HandleIdle();
    protected abstract void HandleAgro();
    protected abstract void HandleDead();
    protected abstract void HandleAttacking();
    protected abstract void HandleDamage();
    protected abstract void HandlePatrol();

    // used to change the enemy state, public so it can be accessed elsewhere if necessary
    // make sure to only use this when changing states just for continuity
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

    // called in the update function
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
