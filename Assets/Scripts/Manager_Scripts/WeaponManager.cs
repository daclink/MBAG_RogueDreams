using UnityEngine;

public class WeaponManager : MonoBehaviour
{

    [SerializeField] private Transform meleeArea;
    [SerializeField] private Transform frontIndicator;
    
    private GameObject currentWeapon;
    

    /**
     * assign the meleeArea on start from the PlayerPrefab just in case
     */
    void Start()
    {
        if (meleeArea == null)
        {
            meleeArea = transform.Find("MeleeArea");
        }

        if (frontIndicator == null)
        {
            frontIndicator = meleeArea.Find("FrontIndicator");
            
            if (frontIndicator == null)
            {
                Debug.LogError("PlayerFrontIndicator not found under MeleeArea! Check the exact name and hierarchy.");
            }
            else
            {
                Debug.Log("FrontIndicator found: " + frontIndicator.name);
            }
        }
    }

    /**
     * Called from the weaponPickupBehavior script when a weapon gets collected by the player
     */
    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.Log("Passed in weaponPrefab is null!");
        }
        
        //check if there is currently a weapon on the player
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }
        
        //ready to instantiate and setup the new weapon on the player
        currentWeapon = Instantiate(weaponPrefab, meleeArea);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;

        //Set weapon specifics
        WeaponBehavior weaponBehavior = currentWeapon.GetComponent<WeaponBehavior>();
        // frontIndicator = meleeArea.transform.Find("FrontIndicator");
        weaponBehavior.Initialize(frontIndicator, meleeArea);
        
    }

    /**
     * Unequips the current weapon
     * could later add that this drops the current weapon's weaponPickup back on the ground.... would love this <3
     */
    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
        }
    }

}
