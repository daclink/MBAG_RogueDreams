using System;
using System.Collections;
using Enemy_Scripts;
using UnityEngine;

public class MeleeEnemy : BaseEnemy
{
    // TODO: Might be useful to keep track of the previous state
    
    [Header("Patrol Settings")]
    [SerializeField] private float patrolMovementDuration;
    [SerializeField] private float patrolMovementInterval;
    
    private bool movingInPatrolState;
    private bool enablePatrolStateMovement;
    private Vector2 patrolMoveDirection;
    private float rangeMin = -1f;
    private float rangeMax = 1f;
    
    /**
     * Abstract method implementation
     * Initialize melee enemy bools
     */
    protected override void PostStart()
    {
        movingInPatrolState = false;
        enablePatrolStateMovement = false;
        enableMovement = true;
    }

    /**
    * Abstract method implementation
    * Calls the base update method 
    */
    protected override void Update()
    {
        base.Update();
        
        // Handles patrol state movements 
        if (enablePatrolStateMovement)
        {
            PatrolMovement();   
        }
    }

    /**
     * Abstract method implementation for idle state
     */
    protected override void HandleIdle()
    {
        if (!enableMovement) return;
        
        // Checks for if changing the state is necessary
        if (distanceToPlayer < agroRange)
        {
            ChangeState(EnemyState.Agro);
            return;
        }
        if (distanceToPlayer < patrolRange)
        {
            ChangeState(EnemyState.Patrol);
        }
    }
    
    /**
    * Abstract method implementation for patrol state
    */
    protected override void HandlePatrol()
    {
        if (!enableMovement) return;

        // Checks for if changing the state is necessary
        if (distanceToPlayer < agroRange)
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Agro);
        }

        if (distanceToPlayer > patrolRange)
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Idle);
        }
        
        // Random movement while in patrol state
        if (!movingInPatrolState)
        {
            // Pick direction to move in patrol state
            float randomX = UnityEngine.Random.Range(rangeMin, rangeMax);
            float randomY = UnityEngine.Random.Range(rangeMin, rangeMax);
            
            // Calculate the movement direction
            patrolMoveDirection = new Vector2(randomX, randomY).normalized;

            movingInPatrolState = true;
            StartCoroutine(PatrolMovementCR());
        }
    }

    /**
     * Move for patrolMovementDuration
     * Stop moving for patrolMovementInterval
     */
    private IEnumerator PatrolMovementCR()
    { 
        
        enablePatrolStateMovement = true;
        yield return new WaitForSeconds(patrolMovementDuration);
        enablePatrolStateMovement = false;
        rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(patrolMovementInterval);
        movingInPatrolState = false;
    }

    /**
     * Handles the patrol state velocity
     */
    private void PatrolMovement()
    {
        rb.linearVelocity = patrolMoveDirection * (Time.deltaTime * moveSpeed);
    }
    
    /**
     * Abstract method implementation for the agro state
     */
    protected override void HandleAgro()
    {
        if (!enableMovement) return;
        
        // Checks for if changing state is necessary
        if (distanceToPlayer > agroRange)
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        // While in the agro state, move towards the player
        // Calculate the direction that the player is from the enemy
        Vector3 direction = new Vector3(transform.position.x - playerTransform.position.x, transform.position.y - playerTransform.position.y, 0).normalized;
        // Move Towards the player
        rb.linearVelocity = -direction * (Time.deltaTime * moveSpeed);
    }

    /**
     * Abstract method implementation for the attacking state
     * Currently unused so this instantly sets the state to the idle state
     */
    protected override void HandleAttacking()
    {
       ChangeState(EnemyState.Idle);
    }

    /**
     * Abstract method implementation for the taking damage state
     * Calls the base class take Damage method and then changes the state to idle
     */
    protected override void HandleDamage()
    {
        // base.TakeDamage(dmgTaken);
        // if (health <= 0)
        // {
        //     return;
        // }
        // ChangeState(EnemyState.Idle);
    }

    /**
     * Abstract method implementation for the dead state
     * This just needs to destroy the enemy game object
     */
    protected override void HandleDead()
    {
        Debug.Log("Enemy Dead");
        Destroy(gameObject);
    }
    
    /**
     * This overrides the base class OnTriggerEnter method
     * The purpose of this is to check if movement is enabled or not to determine if the collision should occur
     */
    // protected override void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (!enableMovement) return;
    //     
    //     base.OnTriggerEnter2D(collision);
    // }
}
