using System;
using System.Collections;
using Enemy_Scripts;
using UnityEngine;

public class MeleeEnemy : BaseEnemy
{
    //might be useful to keep track of the previous state

    private bool movingInPatrolState;
    private bool enablePatrolStateMovement;
    private Vector2 patrolMoveDirection;
    
    protected override void PostStart()
    {
        health = 10f;
        attackDmg = 2f;
        // moveSpeed = 50f;
        agroRange = 3f;
        patrolRange = 8f;
        movingInPatrolState = false;
        enablePatrolStateMovement = false;
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
        enableMovement = false;
        if (distanceToPlayer < patrolRange)
        {
            ChangeState(EnemyState.Patrol);
        }
        
        
        
    }
    
    protected override void HandlePatrol()
    {
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
            float randomX = UnityEngine.Random.Range(-1.0f, 1.0f);
            float randomY = UnityEngine.Random.Range(-1.0f, 1.0f);
            
            patrolMoveDirection = new Vector2(randomX, randomY).normalized;
            
            
            Debug.Log("not moving in patrol state, picking direction: " + patrolMoveDirection);
            movingInPatrolState = true;
            StartCoroutine(PatrolMovementCR());
        }
        
        
    }

    private IEnumerator PatrolMovementCR()
    {
        Debug.Log("Started patrol movement Coroutine");
        enablePatrolStateMovement = true;
        yield return new WaitForSeconds(0.5f);
        enablePatrolStateMovement = false;
        rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(1f);
        movingInPatrolState = false;
    }

    private void PatrolMovement()
    {
        Debug.Log("Started patrol movement for 0.5 seconds");
        rb.linearVelocity = patrolMoveDirection * (Time.deltaTime * moveSpeed);
    }
    
    
    
    protected override void HandleAgro()
    {
        if (distanceToPlayer > agroRange)
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        //while in the agro state, move towards the player
        
        //calculate the direction that the player is from the enemy
        Vector3 direction = new Vector3(transform.position.x - playerTransform.position.x, transform.position.y - playerTransform.position.y, 0).normalized;
        rb.linearVelocity = -direction * (Time.deltaTime * moveSpeed);   
        
        
        // Debug.Log(" linearVelocity of enemy: " + rb.linearVelocity + " move speed : " + moveSpeed);
    }

    protected override void HandleAttacking()
    {
       // TODO: apply knockback to the enemy
       
       // TODO: fire event to the player controller that will deal attackDmg to the player
       
       // AFTER knockback is completed, switch state back Patrol
       ChangeState(EnemyState.Patrol);
    }

    protected override void HandleDamage()
    {
        base.TakeDamage(dmgTaken);
        if (health <= 0)
        {
            return;
        }
        ChangeState(EnemyState.Patrol);

    }

    protected override void HandleDead()
    {
        Debug.Log("Deaddddddd");
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Attacking);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerWeapon"))
        {
            dmgTaken = 2f;
            ChangeState(EnemyState.TakeDamage);
        }
    }
    
}
