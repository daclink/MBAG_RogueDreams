using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerController : MonoBehaviour
{
    private Vector2 movement;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movement = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = movement;
        // Debug.Log(rb.linearVelocity);
    }

    void OnMove(InputValue value)
    {
        movement = value.Get<Vector2>();
        movement = new Vector2(Mathf.Clamp(movement.x * moveSpeed, -5.0f, 5.0f), Mathf.Clamp(movement.y * moveSpeed, -5.0f, 5.0f));
    }
}
