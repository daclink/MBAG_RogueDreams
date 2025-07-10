using System.Collections;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    //knockback should only be APPLIED to player or enemy
    //knockback should only be between the player and enemy/projectiles
    
    // public delegate void TriggerMovement();
    // public static event TriggerMovement OnTriggerMovement;
    
    
    private bool inKnockback = false;
    [SerializeField] private float knockbackStrength = 15f;
    [SerializeField] private float knockbackDuration = 0.15f;
    
    public void KnockbackObject(GameObject target, GameObject incomingObject)
    {
        if (inKnockback) return;
        
        Debug.Log("In knockback script");
        
        Rigidbody2D targetRB = target.GetComponent<Rigidbody2D>();
        Rigidbody2D incomingRB = incomingObject.GetComponent<Rigidbody2D>();
        if (targetRB == null || incomingRB == null)
        {
            Debug.Log("targetRB or incomingRB is null");
            return;
        }
        
        //TODO: disable movement of target and incoming objects instead of the line below
        targetRB.velocity = Vector2.zero;
        incomingRB.velocity = Vector2.zero;
        
        
        //calculate knockback direction
        Vector2 direction = (target.transform.position - transform.position).normalized;
        //apply knockback
        targetRB.linearVelocity = new Vector2(direction.x * knockbackStrength, direction.y * knockbackStrength);

        if (!inKnockback)
        {
            inKnockback = true;
            StartCoroutine(KnockbackCR(target, targetRB));
        }
    }
    
    private IEnumerator KnockbackCR(GameObject target, Rigidbody2D targetRB)
    {
        //knockback the target for the set duration
        yield return new WaitForSeconds(knockbackDuration);
        //after the wait for delay seconds, stop the enemy movement
        if (target != null)
        {
            targetRB.linearVelocity = Vector3.zero;
            //enable movement for the gameobjects target and incomingobject
            
            inKnockback = false;
        }
    }
    
    
    
    
}
