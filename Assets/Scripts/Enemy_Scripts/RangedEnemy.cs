using System.Collections;
using Enemy_Scripts;
using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    [Header("Ranged Enemy Fields")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float firingInterval;
    
    private bool readyToShoot;
    
    /**
     * Defines the PostStart method from the base class and sets proper variables
     */
    protected override void PostStart()
    {
        enableMovement = true;
        readyToShoot = true;
    }
    
    /**
     * Idle state will have no movement 
     */
    protected override void HandleIdle()
    {
        if (!enableMovement) return;
        
        rb.linearVelocity = Vector3.zero;
        
        //Checks if state needs to be changed
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
     * Patrol state will move towards the enemy at the moveSpeed speed
     */
    protected override void HandlePatrol()
    {
        if (!enableMovement) return;

        // Checks if the state needs to be changed
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
        
        // Calculates the patrol movement direction and then moves in that direction
        Vector3 direction = new Vector3(transform.position.x - playerTransform.position.x, transform.position.y - playerTransform.position.y, 0).normalized;
        rb.linearVelocity = -direction * (Time.deltaTime * moveSpeed);   
    }
    
    /**
     * Agro state will move towards the player at half speed and fire bullets at the correct interval
     */
    protected override void HandleAgro()
    {
        if (!enableMovement || playerTransform == null) return;
        
        // Checks if needing to change state
        if (distanceToPlayer > agroRange)
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        // Move at half speed towards the player
        Vector3 direction = new Vector3(transform.position.x - playerTransform.position.x, transform.position.y - playerTransform.position.y, 0).normalized;
        rb.linearVelocity = -direction * (Time.deltaTime * (moveSpeed/2));

        // Check if enemy is allowed to shoot and start the bullet shooting Coroutine
        if (readyToShoot)
        {
            readyToShoot = false;
            StartCoroutine(FireProjectileCR());
        }
    }

    /**
     * Coroutine to handle the bullet firing
     * Instantiates a bullet prefab and waits a set amount of time before ready to shoot again
     */
    private IEnumerator FireProjectileCR()
    {
        Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(firingInterval);
        readyToShoot = true;
    }

    /**
     * Entered when the player and enemy come into direct contact, handled in the base class
     */
    protected override void HandleAttacking()
    {
       ChangeState(EnemyState.Idle);
    }

    /**
     * Handles taking damage
     */
    protected override void HandleDamage()
    {
        base.TakeDamage(dmgTaken);
        if (health <= 0)
        {
            return;
        }
        // After damage is taken, set the state to idle
        ChangeState(EnemyState.Idle);
    }

    /**
     * This state handles when the enemy dies by destroying the enemy game object
     */
    protected override void HandleDead()
    {
        Destroy(gameObject);
    }

    /**
     * This handles trigger object hitting the enemy such as player weapon
     */
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (!enableMovement) return;
        
        base.OnTriggerEnter2D(collision);
    }

}
