using UnityEngine;


/**
 * A standalone class because it can be inherited by anything that needs to look at something and needs to rotate towards it
 */
public class Rotator : MonoBehaviour
{
    
    /**
     * Will make the current object look at the target
     */
    protected void LookAt(GameObject objectToRotate, Vector3 target)
    {
        float lookAngle = ComputeLookAngleZ(transform.position, target);
        objectToRotate.transform.eulerAngles = new Vector3(0, 0, lookAngle);
    }

    /// <summary>Same Z convention as <see cref="LookAt"/> (degrees).</summary>
    protected static float ComputeLookAngleZ(Vector3 fromWorld, Vector3 towardWorld)
    {
        return AngleBetweenTwoPoints(fromWorld, towardWorld) + 180f;
    }

    protected static void SetWorldZRotation(Transform t, float zDegrees)
    {
        var e = t.eulerAngles;
        t.eulerAngles = new Vector3(e.x, e.y, zDegrees);
    }
    
    /**
     * Returns angle between pointA and pointB
     */
    private static float AngleBetweenTwoPoints(Vector3 pointA, Vector3 pointB)
    {
        // Atan2 function to get the angle between the points and Rad2Deg for the conversion constant
        return Mathf.Atan2(pointA.y - pointB.y, pointA.x - pointB.x) * Mathf.Rad2Deg;
    }
}
