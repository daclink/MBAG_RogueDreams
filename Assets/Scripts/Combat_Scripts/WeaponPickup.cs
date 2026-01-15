/**
 * This needs to be able to be selected/collected by the player
 *
 * When this pickup is collected (by pressing the collect key when on it), the weaponPrefab associated with it is
 * passed to another script to instantiate the weapon onto the player, then the pickup is destroyed
 */



using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "WeaponPickupSO")]
public class WeaponPickup : ScriptableObject
{
    public Sprite weaponPickupSprite;
    public GameObject weaponPrefab;
    public string weaponName;
    //add a field for biome that the weapon appears in
    
}
