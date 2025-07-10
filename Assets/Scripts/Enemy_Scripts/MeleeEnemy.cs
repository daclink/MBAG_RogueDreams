using System;
using System.Collections;
using Enemy_Scripts;
using UnityEngine;

public class MeleeEnemy : BaseEnemy
{
    //might be useful to keep track of the previous state
    
    [SerializeField] private float patrolMovementDuration;
    [SerializeField] private float patrolMovementInterval;
    
    private bool movingInPatrolState;
    private bool enablePatrolStateMovement;
    private Vector2 patrolMoveDirection;
    private float rangeMin = -1f;
    private float rangeMax = 1f;
    
    protected override void PostStart()
    {
        movingInPatrolState = false;
        enablePatrolStateMovement = false;
        enableMovement = true;
    }

    protected override void Update()
    {
        base.Update();
        
        distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);

        //handles patrol state movements 
        if (enablePatrolStateMovement)
        {
            PatrolMovement();   
        }
        
    }

    protected override void HandleIdle()
    {
        if (!enableMovement) return;
        
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
    
    protected override void HandlePatrol()
    {
        if (!enableMovement) return;

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

        
        //random movement while in patrol state
        if (!movingInPatrolState)
        {
            //pick direction to move in patrol state
            float randomX = UnityEngine.Random.Range(rangeMin, rangeMax);
            float randomY = UnityEngine.Random.Range(rangeMin, rangeMax);
            
            //calculate the movement direction
            patrolMoveDirection = new Vector2(randomX, randomY).normalized;

            movingInPatrolState = true;
            StartCoroutine(PatrolMovementCR());
        }
        
        
    }

    /**
     * move for patrolMovementDuration
     * stop moving for patrolMovementInterval
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

    private void PatrolMovement()
    {
        rb.linearVelocity = patrolMoveDirection * (Time.deltaTime * moveSpeed);
    }
    
    protected override void HandleAgro()
    {
        if (!enableMovement) return;
        
        if (distanceToPlayer > agroRange)
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        //while in the agro state, move towards the player
        //calculate the direction that the player is from the enemy
        Vector3 direction = new Vector3(transform.position.x - playerTransform.position.x, transform.position.y - playerTransform.position.y, 0).normalized;
        //move rowards the player
        rb.linearVelocity = -direction * (Time.deltaTime * moveSpeed);
    }

    protected override void HandleAttacking()
    {
       ChangeState(EnemyState.Idle);
    }

    protected override void HandleDamage()
    {

        base.TakeDamage(dmgTaken);
        if (health <= 0)
        {
            return;
        }
        ChangeState(EnemyState.Idle);

    }

    protected override void HandleDead()
    {
        Destroy(gameObject);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (!enableMovement) return;
        
        base.OnTriggerEnter2D(collision);

    }
    
}
