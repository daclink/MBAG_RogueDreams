// using UnityEngine;
//
// public class AmmoItem : BaseItem
// {
//     // Event for adding ammo
//     public delegate void AddAmmo(int ammoAmount);
//     public static event AddAmmo OnAddAmmo;
//
//     [SerializeField] private int ammoAmount = 4;
//     
//     /**
//      * When collected, fire event to add ammo to the player and then destory the ammo object
//      */
//     public override void Collect()
//     {
//         // TODO: Ensure only the player can collect an item
//         OnAddAmmo?.Invoke(ammoAmount);
//         Destroy(gameObject);
//     }
// }
