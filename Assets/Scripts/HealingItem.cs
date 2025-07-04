using UnityEngine;

public class HealingItem : BaseItem
{
    [SerializeField] private int healAmount = 2;
    public override void Collect(Player player)
    {
        player.AddHealth(healAmount);
        Destroy(gameObject);
    }
}
