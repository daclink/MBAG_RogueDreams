using Enemy_Scripts;
using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour
{
    
    [SerializeField] protected float health;
    [SerializeField] protected float attackDmg;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float agroRange;
    [SerializeField] protected float patrolRange;

    protected GameObject player;
    protected float distanceToPlayer;
    protected float dmgTaken;
    
    protected Rigidbody2D rb;
    protected bool isAgroed;
    protected bool enableMovement;
    
    [SerializeField] protected EnemyState currentState;

    // to be used in each child class.
    protected abstract void PostStart();

    //initialize globally used variables for the class and child classes and call postStart
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        //this should assign player correctly assuming the player is spawned properly BEFORE the enemy is
        player = GameObject.FindGameObjectWithTag("Player");
        
        PostStart();

        ChangeState(EnemyState.Idle);
        enableMovement = false;
        isAgroed = false;
    }

    // in child classes, use 'protected override void Update()' and call 'base.Update();' at the top of the update
    protected virtual void Update()
    {
        if (currentState != null)
        {
            HandleState();
        }
        
    }
    
    protected virtual void TakeDamage(float dmgAmount)
    {
        //ChangeState(EnemyState.TakeDamage);
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
        #if UNITY_EDITOR
                Debug.Log("PARENT:State changed to " + currentState);
        #endif
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
