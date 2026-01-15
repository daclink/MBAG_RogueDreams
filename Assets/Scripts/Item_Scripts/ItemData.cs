using UnityEngine;


[CreateAssetMenu(fileName = "ItemDataSO")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    
    /**
     * Add any stats below that can be affected by items
     */
    [Header("Stat Effects")] 
    public int healthAmt;
    public int ammoAmt;
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
    
    /**
     * Any effects that happens when collecting such as particle effects/audio
     */
    [Header("Visual/Audio")]
    public AudioClip collectSound;

}