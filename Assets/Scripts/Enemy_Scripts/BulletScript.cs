using System;
using UnityEditor;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public delegate void DamagePlayer(float damage);

    public static event DamagePlayer OnDamagePlayer;

    private Transform target;
    private Rigidbody2D rb;
    private float currTime;
    private float damage;

    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletLifeTime;



    void Start()
    {
        // Debug.Log("Bullet instantiated");
        target = GameObject.FindGameObjectWithTag("Player").transform;
        Vector3 direction =
            new Vector3(transform.position.x - target.position.x, transform.position.y - target.position.y, 0)
                .normalized;
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = -direction * bulletSpeed;
        //Debug.Log("Bullet velocity " + rb.linearVelocity );

        currTime = 0f;
        bulletLifeTime = 5f;
        damage = 5f;
    }

    // Update is called once per frame
    void Update()
    {

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
