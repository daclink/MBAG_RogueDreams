using System;
using UnityEngine;

public class BaseItem : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 90;
    
    // Update is called once per frame
    void Update()
    {
        ItemRotate();
    }

    void ItemRotate()
    {
        Vector3 rot = transform.eulerAngles;
        transform.eulerAngles = new Vector3(rot.x, rot.y, rot.z + (-rotateSpeed * Time.deltaTime));
    }

    public virtual void Collect(Player player) {}
}