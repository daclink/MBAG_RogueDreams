using System.Collections;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    // Knockback should only be APPLIED to player or enemy
    // Knockback should only be between the player and enemy/projectiles
    
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackStrength;
    [SerializeField] private float knockbackDuration;
    
    private bool inKnockback = false;
    private Vector2 direction;
    
    /**
     * This function handles knocking back an object and takes in the parameters of
     * what is getting knocked back (target), and what is applying the knockback (incomingObject)
     */
    public void KnockbackObject(GameObject target, GameObject incomingObject)
    {
        if (inKnockback) return;
        
        // Get the knockback targets rigidbody
        Rigidbody2D targetRB = target.GetComponent<Rigidbody2D>();
        if (targetRB == null)
        {
            Debug.Log("targetRB is null");
            return;
        }
        
        //TODO: disable movement of target and incoming objects instead of the line below
        targetRB.linearVelocity = Vector2.zero;
        
        // Calculate the knockback direction
        direction = (target.transform.position - incomingObject.transform.position).normalized;
        
        // If the object is not in knockback then set knockback bool to true and begin the knockback sequence
        if (!inKnockback)
        {
            inKnockback = true;
            
            StartCoroutine(KnockbackCR(target, targetRB));
        }
    }
    
    /**
     * Coroutine to handle knocking back objects for a set duration then returning them to normal
     */
    private IEnumerator KnockbackCR(GameObject target, Rigidbody2D targetRB)
    {
        // Apply the knockback
        // Knockback the target for the set duration
        targetRB.linearVelocity = new Vector2(direction.x * knockbackStrength, direction.y * knockbackStrength);
        yield return new WaitForSeconds(knockbackDuration);
        // After the wait for delay seconds, stop the enemy movement
        if (target != null)
        {
            targetRB.linearVelocity = Vector3.zero;
            // Enable movement for the gameobjects target and incomingobject
            IdentifyTarget(target);
            inKnockback = false;
        }
    }

    /**
     * This is needed to identify weather the player or enemy needs movement enabled after the knockback
     * This is hardcoded but works fine
     */
    private void IdentifyTarget(GameObject target)
    {
        if (target.CompareTag("Player"))
        {
            target.gameObject.GetComponent<Player>().ExitKnockback();
        }
        else if (target.CompareTag("Enemy"))
        {
            target.gameObject.GetComponent<BaseEnemy>().ExitKnockback();
        }
    }
    
    
    
    
}
