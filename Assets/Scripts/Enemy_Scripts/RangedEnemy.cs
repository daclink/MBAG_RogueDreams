using System.Collections;
using Enemy_Scripts;
using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    private bool readyToShoot;
    private bool movingInPatrolState;
    private bool enablePatrolStateMovement;
    private Vector2 patrolMoveDirection;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletInterval;

    protected override void PostStart()
    {
        health = 10f;
        attackDmg = 2f;
        agroRange = 5f;
        patrolRange = 8f;
        movingInPatrolState = false; 
        enablePatrolStateMovement = false;
        enableMovement = true;
        readyToShoot = true;

    }

    protected override void Update()
    {
        base.Update();

        distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
        
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
            float randomX = UnityEngine.Random.Range(-1.0f, 1.0f);
            float randomY = UnityEngine.Random.Range(-1.0f, 1.0f);
            
            patrolMoveDirection = new Vector2(randomX, randomY).normalized;
            
            
            //Debug.Log("not moving in patrol state, picking direction: " + patrolMoveDirection);
            movingInPatrolState = true;
            StartCoroutine(PatrolMovementCR());
        }
        
        
    }

    private IEnumerator PatrolMovementCR()
    {
        enablePatrolStateMovement = true;
        yield return new WaitForSeconds(0.5f);
        enablePatrolStateMovement = false;
        rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(1f);
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

        //instantiate a bullet, and shoot it in the direction from above

        if (readyToShoot)
        {
            readyToShoot = false;
            StartCoroutine(FireProjectileCR());
        }

    }

    private IEnumerator FireProjectileCR()
    {
        
        Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(2f);
        readyToShoot = true;
        
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
        Debug.Log("Deaddddddd");
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
