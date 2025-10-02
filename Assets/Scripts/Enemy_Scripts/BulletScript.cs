using System;
using UnityEditor;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    // Event to damage the player
    public delegate void DamagePlayer(float damage);
    public static event DamagePlayer OnDamagePlayer;
    
    [Header("Bullet Settings")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float damage;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletLifeTime;

    private Transform target;
    private float currTime;


    void Start()
    {
        // Set the target for the bullet to aim at
        target = GameObject.FindGameObjectWithTag("Player").transform;
        
        // Set the starting bullet direction and velocity
        Vector3 direction = new Vector3(transform.position.x - target.position.x, transform.position.y - target.position.y, 0).normalized;
        rb.linearVelocity = -direction * bulletSpeed;
        
        // Start the timer to track how long the bullet is active for
        currTime = 0f;

    }

    /**
     * Ensures the bullet is not on screen forever but instead for a set max amount of time
     */
    void Update()
    {
        currTime += Time.deltaTime;
        if (currTime >= bulletLifeTime)
        {
            Destroy(this.gameObject);
        }

    }

    /**
     * WHen the bullet collides with an object, determine what it collided with and act accordingly
     */
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Call to damage the player here. This communicates with the playerHealthManager
            // The player script directly handles collisions and applies knockback so don't do that here
            OnDamagePlayer?.Invoke(damage);
        }
        Destroy(this.gameObject);
    }
}
