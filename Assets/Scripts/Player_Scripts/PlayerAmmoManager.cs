using UnityEngine;

public class PlayerAmmoManager : MonoBehaviour
{
    [SerializeField] private int maxAmmo = 6;
    private int currAmmo;
    
    void Start()
    {
        AmmoItem.OnAddAmmo += AddAmmo;
        SetAmmo(maxAmmo);
    }

    private void SetAmmo(int ammo)
    {
        currAmmo = ammo;
    }
    
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
