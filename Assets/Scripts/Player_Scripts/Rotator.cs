using UnityEngine;

/// <summary>
/// A standalone class because it can be inherited by anything that needs to look at something and needs to rotate towards it
/// </summary>
public class Rotator : MonoBehaviour
{
    /// <summary>
    /// Will make the current object look at the target
    /// </summary>
    protected void LookAt(Vector3 target)
    {
        //Get the angle between the transform position and the target, added 180 to deal with angle offset
        float lookAngle = AngleBetweenTwoPoints(transform.position, target) + 180;
        
        //set new rotation
        transform.eulerAngles = new Vector3(0, 0, lookAngle);
    }
    /// <summary>
    /// Returns angle between pointA and pointB
    /// </summary>
    private float AngleBetweenTwoPoints(Vector3 pointA, Vector3 pointB)
    {
        //Atan2 function to get the angle between the points and Rad2Deg for the conversion constant
        return Mathf.Atan2(pointA.y - pointB.y, pointA.x - pointB.x) * Mathf.Rad2Deg;
    }
}
