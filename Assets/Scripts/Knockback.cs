using System.Collections;
using Unity.Hierarchy;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    //knockback should only be APPLIED to player or enemy
    //knockback should only be between the player and enemy/projectiles
    
    
    [SerializeField] private float knockbackStrength;
    [SerializeField] private float knockbackDuration;
    
    private bool inKnockback = false;
    private Vector2 direction;
    
    public void KnockbackObject(GameObject target, GameObject incomingObject)
    {
        if (inKnockback) return;
        
        // get the knockback targets RB 
        Rigidbody2D targetRB = target.GetComponent<Rigidbody2D>();
        if (targetRB == null)
        {
            Debug.Log("targetRB is null");
            return;
        }
        
        //TODO: disable movement of target and incoming objects instead of the line below
        targetRB.linearVelocity = Vector2.zero;
        
        //calculate knockback direction
        direction = (target.transform.position - incomingObject.transform.position).normalized;
        

        if (!inKnockback)
        {
            inKnockback = true;
            
            StartCoroutine(KnockbackCR(target, targetRB));
        }
    }
    
    private IEnumerator KnockbackCR(GameObject target, Rigidbody2D targetRB)
    {
        //knockback the target for the set duration
        //apply knockback
        targetRB.linearVelocity = new Vector2(direction.x * knockbackStrength, direction.y * knockbackStrength);
        yield return new WaitForSeconds(knockbackDuration);
        //after the wait for delay seconds, stop the enemy movement
        if (target != null)
        {
            targetRB.linearVelocity = Vector3.zero;
            //enable movement for the gameobjects target and incomingobject
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
