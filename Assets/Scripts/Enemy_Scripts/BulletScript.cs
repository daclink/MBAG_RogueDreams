using System;
using UnityEditor;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public delegate void DamagePlayer(float damage);
    public static event DamagePlayer OnDamagePlayer;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float damage;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletLifeTime;

    private Transform target;
    private float currTime;


    void Start()
    {
        //where the bullet is going
        target = GameObject.FindGameObjectWithTag("Player").transform;
        
        //starting bullet direction and velocity
        Vector3 direction = new Vector3(transform.position.x - target.position.x, transform.position.y - target.position.y, 0).normalized;
        rb.linearVelocity = -direction * bulletSpeed;
        
        //start timer
        currTime = 0f;

    }

    void Update()
    {
        //ensures the bullet is not on screen forever but instead for a set max amount of time
        currTime += Time.deltaTime;
        if (currTime >= bulletLifeTime)
        {
            Destroy(this.gameObject);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            //call to damage the player here. This communicates with the playerHealthManager
            //The player script directly handles collisions and applies knockback so that does not need to be 
            // done here.
            OnDamagePlayer?.Invoke(damage);
        }
        
        Destroy(this.gameObject);
    }

    
    
}
