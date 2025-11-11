using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponPickupSO")]
public class WeaponPickup : ScriptableObject
{
    public Sprite weaponPickupSprite;
    public GameObject weaponPrefab;
    
    /**
     * This needs to be able to be selected/collected by the player
     *
     * When this pickup is collected (by pressing the collect key when on it), the weaponPrefab associated with it is
     * passed to another script to instantiate the weapon onto the player, then the pickup is destroyed
     */
    
    /**
     * Update method, if the popup text is active and the player presses the collect key, the weaponPrefab associated
     * with this is passed to another script to instantiate the weapon onto the player, then the pickup is destroyed
     */
    
    /**
     * OnTriggerStay
     * displays a 'press 'collect button' to select weapon' when player is touching
     */
    
    /**
     * OnTriggerExit
     * set the popup collect text to 
     */
    
}
