using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

//TODO: implement different weapon types. Not sure if we should have multiple weapon behavior scripts or what the clean way to do this 
// is since I had to remove the baseWeapon class. Possibly refactor this into a type of base weapon class to handle each weapons behavior

public class WeaponBehavior : MonoBehaviour
{
    [SerializeField] private Weapon weaponDataSO;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D weaponCollider;


    private Transform frontIndicator;
    private Transform meleeArea;
    private float attackCooldown;
    private float currCooldown;
    private bool inAttack;


    void Start()
    {
        spriteRenderer.sprite = weaponDataSO.weaponSprite;
        weaponCollider.enabled = false;
        attackCooldown = weaponDataSO.attackCooldown;
        currCooldown = 0f;
        inAttack = false;

        Player.OnAttack += OnPlayerAttack;

    }

    /**
     * This is called from the weapon manager and initializes a weapon with the proper pieces when equipped
     */
    public void Initialize(Transform _frontIndicator, Transform _meleeArea)
    {
        Debug.Log("Initialize called in weaponBehavior script");
        if (_frontIndicator == null || _meleeArea == null)
        {
            Debug.Log("Passed in initialize vals are null!");
        }
        frontIndicator = _frontIndicator;
        meleeArea = _meleeArea;
    }

    void Update()
    {
        if (currCooldown > 0f)
        {
            currCooldown -= Time.deltaTime;
        }
        
        //Use the frontIndicator to determine the weapon facing direction and rotation
        //This will be coded assuming the weapon icons are drawn facing vertically
        transform.position = frontIndicator.position;
        transform.rotation = frontIndicator.rotation;
    }

    /**
     * This is called when the player Attacks and is invoked from the player class OnPlayerMelee() method
     * All subsequent attack methods run from here by initially calling Attack
     */
    private void OnPlayerAttack()
    {
        Debug.Log("OnPlayerAttack invoked and called in weapon behavior script");
        Attack();
    }

    /**
     * attack method that calls the start attack and polls the attack cooldown 
     */
    public void Attack()
    {
        if (currCooldown <= 0f)
        {
            //attack code here

            currCooldown = attackCooldown;
            
            //reset the hit targets array that will be stored here

            StartAttack();
        }
        else
        {
            // Debug.Log("Cannot attack while in weapon cooldown. Weapon Cooldown has left: " + currCooldown);
        }
    }
    
    /**
     * starts the attack, animates, and starts coroutine that will end the attack
     */
    private void StartAttack()
    {
        inAttack = true;
        weaponCollider.enabled = true;
        
        //Play the attack animation here
        //use a coroutine to call endAttack after a set amount of time which would be after the animation is done
        StartCoroutine(AttackWindow());
    }

    /**
     * at the end of the attack, set the collider back to disabled
     */
    private void EndAttack()
    {
        inAttack = false;
        weaponCollider.enabled = false;
    }

    /**
     * allows the weapon collider to be active for a set amount of time during the attack
     */
    public IEnumerator AttackWindow()
    {
        yield return new WaitForSeconds(0.5f);
        EndAttack();
    }

    /**
     * This collects all hit objects, if they are enemies then they are damaged
     * This will be designed once I refactor enemies to be SO's and when I rediscover how they work lol
     * Calls DealDamage on each enemy hit
     */
    void OnTriggerEnter2D(Collider2D other)
    {
        //deal with adding collided enemies to an array of hit enemies
        if (inAttack)
        {
            Debug.Log("Hit an enemy");
            if (other.CompareTag("Enemy"))
            {
                BaseEnemy enemy = other.gameObject.GetComponent<BaseEnemy>();
                DealDamage(enemy);
            }
        }
    }

    /**
     * This method takes in an enemy from the Trigger method and damages it the correct amount based on the weaponDataSO
     * This calls the public TakeDamage method in the baseEnemy class
     */
    private void DealDamage(BaseEnemy enemy)
    {
        if (enemy != null)
        {
            Debug.Log("Enemy taking damage, called from weaponBehavior");
            enemy.TakeDamage(weaponDataSO.damage);
        }
    }

    private void OnDestroy()
    {
        Player.OnAttack -= OnPlayerAttack;
    }

    //any getters for weaponstats if necessary


}