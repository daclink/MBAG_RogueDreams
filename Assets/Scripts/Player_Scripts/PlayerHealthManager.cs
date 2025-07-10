using UnityEngine;

public class PlayerHealthManager : MonoBehaviour
{
    public delegate void SetHealthText(int health);
    public static event SetHealthText OnSetHealthText;
    
    [SerializeField] private int startHealth = 100;
    private int currHealth;
    
    void Start()
    {
        BulletScript.OnDamagePlayer += RemoveHealth;
        BaseEnemy.OnDamagePlayer += RemoveHealth;
        HealingItem.OnAddHealth += AddHealth;
        SetHealth(startHealth);
        //listen to 2 events
        //  - one event for adding health from the healing items
        //  - one event for taking damage from the enemy class
    }
    

    void SetHealthBar(int health)
    {
        //set the healthBarVisual here
        //fire event to the HealthText script with the health value
        OnSetHealthText?.Invoke(health);
    }
    
    /**
     * This function sets the player health directly,
     * does not add or subtract health
     * int health is the int value to set the health to
     */
    void SetHealth(int health)
    {
        currHealth = health;
        SetHealthBar(currHealth);
    }

    /**
     * This function takes in a health value and adds it to the current health
     * and checks that health does not go over the max
     * int health is the passed in int value to add to the current health
     */
    void AddHealth(int health)
    {
        Debug.Log("Adding " + health + " health");
        currHealth += health;
        if (currHealth > startHealth)
        {
            currHealth = startHealth;
        }
        SetHealthBar(currHealth);
    }

    /**
     * This function takes in a health value and subtracts it from the players
     * current health and makes sure health does not go below 0
     * int health is the passed in int value to subtract from the current health
     */
    void RemoveHealth(float health)
    {
        currHealth -= (int) health;
        if (currHealth <= 0)
        {
            currHealth = 0;
            SetHealthBar(currHealth);
            // kill player here either directly or through an event to another script
            return;
        }
        SetHealthBar(currHealth);
        
    }

    public void OnDestroy()
    {
        HealingItem.OnAddHealth -= AddHealth;
    }


}
