using UnityEngine;


[CreateAssetMenu(fileName = "WeaponSO")]
public class Weapon : ScriptableObject
{
    [Header("Visual")] 
    public Sprite weaponSprite;
    public string weaponName;

    [Header("Stats")] 
    public float damage;
    public float attackRange;
    public float knockbackValue;
    public float attackCooldown;

}
