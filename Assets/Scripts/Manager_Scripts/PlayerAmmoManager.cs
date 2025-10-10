using UnityEngine;

public class PlayerAmmoManager : MonoBehaviour
{
    [SerializeField] private int maxAmmo = 6;
    
    private int currAmmo;
    
    void Start()
    {
        // When ammo is picked up, set the current ammo to max
        AmmoItem.OnAddAmmo += AddAmmo;
        SetAmmo(maxAmmo);
    }

    /**
     * Setter for ammo that sets the current ammo to the passed in int
     */
    private void SetAmmo(int ammo)
    {
        currAmmo = ammo;
    }
    
    /**
     * If adding ammo, add the proper amount and cap out at the max ammo
     */
    private void AddAmmo(int ammo)
    {
        Debug.Log("Adding " + ammo + " health");
        currAmmo += ammo;
        if (currAmmo > maxAmmo)
        {
            currAmmo = maxAmmo;
        }
    }
}
