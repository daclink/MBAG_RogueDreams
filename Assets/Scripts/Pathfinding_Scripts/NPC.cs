using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class NPC : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float turnSpeed = 720f; // in degrees per second
    [SerializeField] private float arrivedDistance = 0.02f; // how close do you need to be to count as "arrived"
    public Vector2 debugMove = new Vector2(10f, 10f);

    private Vector2 _targetPos;
    private bool _hasTarget;

    public event Action<NPC> Arrived; // event that passes the NPC that arrived
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _hasTarget = false;
        if (debugMove != Vector2.zero)
        {
            SetMoveTarget(debugMove); 
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMove();
    }
    private void UpdateMove()
    {
        if (!_hasTarget)
        {
            return;
        }

        float z = transform.position.z; // shouldn't ever change
        Vector2 currentPos = transform.position;
        // vector points from current position to target position
        Vector2 posToTarget = _targetPos - currentPos; 

        // direct distance to target
        float dist = posToTarget.magnitude;

        if (dist <= arrivedDistance) // if we're close enough to our target
        {
            transform.position = new Vector3(_targetPos.x, _targetPos.y, z); // snap to target (maybe not needed)
            _hasTarget = false;
            Arrived?.Invoke(this);
            return;
        }
        
        // posToTarget vector to get the direction
        Vector2 dir = posToTarget / dist; 
        
        // Moving
        float step = moveSpeed * Time.deltaTime;
        // use built in function for moving
        Vector2 nextPos = Vector2.MoveTowards(currentPos, _targetPos, step); 
        transform.position = new Vector3(nextPos.x, nextPos.y, z);
        
        // Rotate to face dir
        // convert direction vector to angle in degrees, -90 because default faces up
        float destAngle = (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) - 90f; 

        // get the current z rotation angle (apparently z for 2D)
        float currentAngle = transform.eulerAngles.z;
        // use built in function to turn smoothly
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, destAngle, turnSpeed * Time.deltaTime);
        // Quaternion things, idk
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    
    public void SetMoveTarget(Vector2 dest)
    {
        _targetPos = dest;
        _hasTarget = true;
    }

    public void ClearTarget()
    {
        _hasTarget = false;
    }

    public bool HasTarget()
    {
        return _hasTarget;
    }

}
