using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives damage for player-spawned thrown objects. Add (or use <see cref="EnsureKinematicAndTrigger2D"/>) on any
/// thrown GameObject, then <see cref="Initialize"/> with damage. Uses <see cref="OnTriggerEnter2D"/> against
/// <see cref="BaseEnemy"/> (any collider; resolves parent). Projectile motion should use <see cref="Rigidbody2D.MovePosition"/>
/// in <c>FixedUpdate</c> (see thrown-weapon behaviour) so triggers align with the physics step. Each enemy is damaged at most once; if
/// the projectile is not destroyed on first hit, it can damage additional enemies it touches later.
/// </summary>
[DisallowMultipleComponent]
public class ThrownProjectile2D : MonoBehaviour
{
    [SerializeField] float _damage = 2f;
    [SerializeField] bool _destroyOnFirstHit = true;

    Rigidbody2D _rigidbody;
    HashSet<int> _damaged;

    public void Initialize(float damage, bool destroyOnFirstHit = true)
    {
        _damage = damage;
        _destroyOnFirstHit = destroyOnFirstHit;
    }

    public static ThrownProjectile2D Setup(GameObject go, float damage, float triggerRadius = 0.35f, bool destroyOnFirstHit = true)
    {
        EnsureKinematicAndTrigger2D(go, triggerRadius);
        var t = go.GetComponent<ThrownProjectile2D>();
        if (t == null) t = go.AddComponent<ThrownProjectile2D>();
        t.Initialize(damage, destroyOnFirstHit);
        return t;
    }

    /// <summary>Call for any custom thrown object so trigger callbacks register with 2D physics.</summary>
    public static void EnsureKinematicAndTrigger2D(GameObject go, float circleRadius = 0.35f)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.GetComponent<Collider2D>();
        if (col == null)
        {
            var c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = circleRadius;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void Awake() => _rigidbody = GetComponent<Rigidbody2D>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.CompareTag("Player")) return;

        if (!other.TryGetComponent(out BaseEnemy enemy)) enemy = other.GetComponentInParent<BaseEnemy>();
        if (enemy == null) return;

        if (_damaged == null) _damaged = new HashSet<int>();
        if (!_damaged.Add(enemy.GetInstanceID())) return;

        enemy.TakeDamage(_damage);
        if (_destroyOnFirstHit) Destroy(gameObject);
    }
}
