using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    //enemy base class uses onTriggerEnter2D to damage enemies
    
    
    // Common weapons variables that any weapon type will have
    [Header("Weapon Settings")]
    [SerializeField] protected float damage;
    [SerializeField] protected float range;
    [SerializeField] protected Transform playerTransform;
    [SerializeField] protected float fireRate;
    
    //non-serializable fields
    protected bool canFire;
    
    
    // To be called in any child class
    protected abstract void PostStart();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        PostStart();
        Player.OnAttack += OnAttack;
        canFire = true;
        playerTransform = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        
    }

    //move all attack code from the player script to the weapon
    //the player class will just need to call the weapons OnAttack when the player attacks
    //if necessary, the player script can keep a bool that tracks when the player is in an attack
    protected virtual void OnAttack()
    {
        Debug.Log("Player OnAttack triggered in BaseWeapon");
        Attack();
    }

    protected abstract void Attack();

    protected virtual void OnDestroy()
    {
        Player.OnAttack -= OnAttack;
    }
}
