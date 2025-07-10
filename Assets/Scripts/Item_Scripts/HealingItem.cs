using UnityEngine;

public class HealingItem : BaseItem
{

    public delegate void AddHealth(int healAmount);
    public static event AddHealth OnAddHealth;
    
    [SerializeField] private int healAmount = 2;
    public override void Collect(Player player)
    {
        //fire event to add healAmount health
        // this communicates with the playerHealthManager
        OnAddHealth?.Invoke(healAmount);
        
        // player.AddHealth(healAmount);
        Destroy(gameObject);
    }
}
