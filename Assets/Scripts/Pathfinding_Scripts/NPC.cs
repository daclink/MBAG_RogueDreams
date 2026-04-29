using System;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private float arrivedDistance = 0.02f;
    [SerializeField] private float collisionRadius = 0.22f;
    public Vector2 debugMove = new Vector2(10f, 10f);

    private Vector2 _targetPos;
    private bool _hasTarget;
    private Rigidbody2D _rigidbody2D;

    public event Action<NPC> Arrived;

    void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        if (_rigidbody2D == null)
            _rigidbody2D = gameObject.AddComponent<Rigidbody2D>();

        // Dynamic (not Kinematic) so static TilemapCollider2D walls actually block MovePosition.
        // Kinematic bodies ignore static contacts and pass through walls.
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.gravityScale = 0f;
        _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rigidbody2D.linearDamping = 10f;

        if (GetComponent<Collider2D>() == null)
        {
            var c = gameObject.AddComponent<CircleCollider2D>();
            c.radius = collisionRadius;
        }
    }

    void Start()
    {
        _hasTarget = false;
        if (debugMove != Vector2.zero)
            SetMoveTarget(debugMove);
    }

    void FixedUpdate()
    {
        UpdateMove();
    }

    private void UpdateMove()
    {
        if (!_hasTarget)
            return;

        float z = transform.position.z;
        Vector2 currentPos = _rigidbody2D != null ? _rigidbody2D.position : (Vector2)transform.position;
        Vector2 posToTarget = _targetPos - currentPos;
        float dist = posToTarget.magnitude;

        if (dist <= arrivedDistance)
        {
            if (_rigidbody2D != null)
                _rigidbody2D.MovePosition(new Vector2(_targetPos.x, _targetPos.y));
            else
                transform.position = new Vector3(_targetPos.x, _targetPos.y, z);
            _hasTarget = false;
            Arrived?.Invoke(this);
            return;
        }

        Vector2 dir = posToTarget / dist;
        float step = moveSpeed * Time.fixedDeltaTime;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, _targetPos, step);

        if (_rigidbody2D != null)
            _rigidbody2D.MovePosition(nextPos);
        else
            transform.position = new Vector3(nextPos.x, nextPos.y, z);

        float destAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, destAngle, turnSpeed * Time.fixedDeltaTime);
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

    /// <summary>Room-tree pathfinding may add NPC at runtime (e.g. on melee enemy prefab); use this to match chase feel.</summary>
    public void SetMoveSpeedForPathfinding(float speed) => moveSpeed = Mathf.Max(0.01f, speed);
}
