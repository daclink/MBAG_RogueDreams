using System;
using Enemy_Scripts;
using UnityEngine;

public class MeleeEnemy : BaseEnemy
{
    //might be useful to keep track of the previous state
    
    
    protected override void PostStart()
    {
        health = 10f;
        attackDmg = 2f;
        moveSpeed = 5f;
        agroRange = 3f;
        patrolRange = 5f;
    }

    protected override void Update()
    {
        base.Update();
        
        distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);
        //Debug.Log("Distance to player " + distanceToPlayer);
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
            ChangeState(EnemyState.Agro);
        }

        if (distanceToPlayer > patrolRange)
        {
            ChangeState(EnemyState.Idle);
        }
    }
    
    protected override void HandleAgro()
    {
        if (distanceToPlayer > agroRange)
        {
            ChangeState(EnemyState.Patrol);
        }
        
        //while in the agro state, move towards the player
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
