using System;
using UnityEngine;

public class BaseItem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private float rotateSpeed = 90;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BoxCollider2D boxCollider2D;

    
    //create a listener method in player class that will parse through the data that is passed and change players stats
    public delegate void ItemCollected(ItemData data);
    public static event ItemCollected OnItemCollected;

    void Start()
    {
        if (spriteRenderer == null)
        {
            Debug.Log("Sprite Renderer is null on item pickup");
        }
        else
        {
            //set the sprite and collider bounds here
            spriteRenderer.sprite = itemData.icon;
            boxCollider2D.size = itemData.icon.bounds.size;
        }
    }
    
    /**
     * Update is called once per frame to rotate the item
     */
    void Update()
    {
        ItemRotate();
    }

    /**
     * Rotates the item using euler angles
     */
    void ItemRotate()
    {
        Vector3 rot = transform.eulerAngles;
        transform.eulerAngles = new Vector3(rot.x, rot.y, rot.z + (-rotateSpeed * Time.deltaTime));
    }

    /**
     * Collect Item Logic
     */
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        OnItemCollected?.Invoke(itemData);
        
        Destroy(gameObject);
        
    }
    
}