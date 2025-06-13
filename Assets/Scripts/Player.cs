using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private Vector2 moveDir;

    private void Update()
    {
        Move();
    }

    /// <summary>
    /// Handles movement with a Vector2 move direction and a float speed variable
    /// </summary>
    private void Move()
    {
        //store current position
        Vector3 pos = transform.position;
        //create new position to update with
        Vector2 newPos = new Vector2(pos.x + (moveDir.x * speed * Time.deltaTime), pos.y + (moveDir.y * speed * Time.deltaTime));
        //change current position to new position
        transform.position = new Vector3(newPos.x, newPos.y, pos.z);
    }

    /// <summary>
    /// New Input system function, reads values from configured input file
    /// </summary>
    public void OnMovePlayer(InputAction.CallbackContext context)
    {
        moveDir = context.ReadValue<Vector2>();
    }
}
