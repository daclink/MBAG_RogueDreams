using UnityEngine;

public class AmmoItem : BaseItem
{
    public delegate void AddAmmo(int ammoAmount);
    public static event AddAmmo OnAddAmmo;
    
    [SerializeField] private int ammoAmount = 4;
    public override void Collect()
    {
        OnAddAmmo?.Invoke(ammoAmount);
        
        Destroy(gameObject);
    }
}
