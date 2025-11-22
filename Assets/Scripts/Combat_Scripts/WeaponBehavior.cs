using System.Collections;
using UnityEngine;

public class WeaponBehavior : MonoBehaviour
{
    [SerializeField] private Weapon weaponDataSO;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D weaponCollider;


    private Transform frontIndicator;
    private Transform meleeArea;
    private float attackCooldown;
    private float currCooldown;


    void Start()
    {
        spriteRenderer.sprite = weaponDataSO.weaponSprite;
        weaponCollider.enabled = false;
        attackCooldown = weaponDataSO.attackCooldown;
        currCooldown = 0f;
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
    }

    /**
     * starts the attack, animates, and starts coroutine that will end the attack
     */
    private void StartAttack()
    {
        weaponCollider.enabled = true;
        
        //Play the attack animation here
        //use a coroutine to call endAttack after a set amount of time which would be after the animation is done
        StartCoroutine(AttackWindow());
    }

    /**
     * at the end of the attack, set the colleder back to disabled
     */
    private void EndAttack()
    {
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
        Debug.Log("Hit an enemy");
    }

    private void DealDamage()
    {
        
    }
    
    //any getters for weaponstats if necessary


}