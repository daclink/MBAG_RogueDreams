using UnityEngine;

public class HealingItem : BaseItem
{
    // Event to add health to the player
    public delegate void AddHealth(int healAmount);
    public static event AddHealth OnAddHealth;
    
    [SerializeField] private int healAmount = 2;
    
    /**
     * Handles when the health item is collected then destroys the object
     */
    public override void Collect()
    {
        // TODO: Ensure only the player can collect health
        // Fire event to add healAmount health
        // This communicates with the playerHealthManager
        OnAddHealth?.Invoke(healAmount);
        
        Destroy(gameObject);
    }
}
