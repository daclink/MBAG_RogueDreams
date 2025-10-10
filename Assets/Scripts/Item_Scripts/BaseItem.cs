using System;
using UnityEngine;

public class BaseItem : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 90;
    
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
    
    // TODO: Ensure only the player can collect items
    public virtual void Collect() {}
}