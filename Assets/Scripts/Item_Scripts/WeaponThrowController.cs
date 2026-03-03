using UnityEngine;
using UnityEngine.InputSystem;
using DataSchemas.PackedItem;

/// <summary>
/// When weapon is equipped, click to throw it in an arc to the cursor. Adds to same GameObject as PlayerEquipment.
/// </summary>
[RequireComponent(typeof(PlayerEquipment))]
public class WeaponThrowController : MonoBehaviour
{

    [SerializeField] Transform _weaponHandPoint;
    [SerializeField] float _throwDuration = 0.4f;
    [SerializeField] float _arcHeight = 2f;
    [SerializeField] float _spinSpeed = 720f;

    PlayerEquipment _equipment;
    Camera _cam;

    void Awake()
    {
        _equipment = GetComponent<PlayerEquipment>();
    }

    void Update()
    {
        if (_equipment?.Weapon == null) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryThrowWeapon();
    }

    void TryThrowWeapon()
    {
        var weapon = _equipment.Weapon;
        if (!weapon.HasValue) return;

        var bootstrap = ItemTableBootstrap.Instance;
        if (bootstrap?.Table?.SpriteTable == null) return;

        Sprite sprite = bootstrap.Table.SpriteTable.Get(weapon.Value.Type, weapon.Value.BiomeFlags, weapon.Value.Key);
        if (sprite == null) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // Start: weapon hand position (or player position with offset)
        Vector3 start = _weaponHandPoint != null
            ? _weaponHandPoint.position
            : transform.position + (Vector3)Vector2.right * 0.5f;

        // End: cursor world position (z=0 for 2D)
        Vector2 screenPos = Mouse.current.position.ReadValue();
        float depth = Mathf.Abs(_cam.transform.position.z);
        Vector3 end = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        end.z = 0;

        ThrownWeaponProjectileImpl.Launch(start, end, sprite, _throwDuration, _arcHeight, _spinSpeed);
        _equipment.ConsumeWeapon(); // Weapon is thrown, not returned to inventory
    }
}

/// <summary>
/// Moves a weapon sprite in an arc from start to target, spinning clockwise. Destroyed on arrival.
/// In same file as WeaponThrowController to avoid assembly/compilation issues.
/// </summary>
static class ThrownWeaponProjectileImpl
{
    public static void Launch(Vector3 start, Vector3 target, Sprite sprite, float duration = 0.4f, float arcHeight = 2f, float spinSpeed = 720f)
    {
        var go = new GameObject("ThrownWeapon");
        go.transform.position = start;
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 20;

        go.AddComponent<ThrownWeaponProjectileBehaviour>().Init(start, target, duration, arcHeight, -spinSpeed);
    }
}

class ThrownWeaponProjectileBehaviour : MonoBehaviour
{
    Vector3 _start, _target;
    float _duration, _arcHeight, _spinSpeed, _elapsed;

    public void Init(Vector3 start, Vector3 target, float duration, float arcHeight, float spinSpeed)
    {
        _start = start; _target = target; _duration = duration; _arcHeight = arcHeight; _spinSpeed = spinSpeed;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        Vector3 linear = Vector3.Lerp(_start, _target, t);
        float arc = 4f * _arcHeight * t * (1f - t);
        transform.position = linear + Vector3.up * arc;
        transform.Rotate(0, 0, _spinSpeed * Time.deltaTime);
        if (t >= 1f) Destroy(gameObject);
    }
}
