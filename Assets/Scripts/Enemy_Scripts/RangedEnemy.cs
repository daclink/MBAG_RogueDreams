using System.Collections;
using Enemy_Scripts;
using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    
    
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float firingInterval;

    
    private bool readyToShoot;


    protected override void PostStart()
    {
        enableMovement = true;
        readyToShoot = true;
    }

    protected override void Update()
    {
        base.Update(); 
        
    }
    
    /**
     * Idle state will have no movement 
     */
    protected override void HandleIdle()
    {
        if (!enableMovement) return;
        
        rb.linearVelocity = Vector3.zero;
        
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

        Vector3 direction = new Vector3(transform.position.x - playerTransform.position.x, transform.position.y - playerTransform.position.y, 0).normalized;
        rb.linearVelocity = -direction * (Time.deltaTime * moveSpeed);   
    }
    
    /**
     * Agro state will move towards the player at half speed and fire bullets at the correct interval
     */
    protected override void HandleAgro()
    {
        if (!enableMovement) return;
        
        if (distanceToPlayer > agroRange)
        {
            rb.linearVelocity = Vector3.zero;
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        //move at half speed towards the player
        Vector3 direction = new Vector3(transform.position.x - playerTransform.position.x, transform.position.y - playerTransform.position.y, 0).normalized;
        rb.linearVelocity = -direction * (Time.deltaTime * (moveSpeed/2));

        //check if enemy is allowed to shoot and start the bullet shooting CR

        if (readyToShoot)
        {
            readyToShoot = false;
            StartCoroutine(FireProjectileCR());
        }

    }

    /**
     * Coroutine to handle the bullet firing
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
        ChangeState(EnemyState.Idle);

    }

    /**
     * This state handles when the enemy dies
     */
    protected override void HandleDead()
    {
        Destroy(gameObject);
    }

    /**
     * Handles collisions with the enemy such as bullet collisions
     */
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
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
