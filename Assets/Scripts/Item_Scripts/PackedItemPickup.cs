using UnityEngine;
using DataSchemas.PackedItem;

/// <summary>
/// World pickup for packed items. Set reference in inspector. On collision with player, adds to inventory and destroys self.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PackedItemPickup : MonoBehaviour
{
    [SerializeField] PackedItemReference _reference;
    [SerializeField] SpriteRenderer _spriteRenderer;

    void Start()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Debug.Log($"[PackedItemPickup] Start on {gameObject.name} @ {transform.position}, ref=({_reference.Type},{_reference.BiomeFlags},{_reference.Key}), sr={(_spriteRenderer != null)}");
        ApplySprite();
    }

    void ApplySprite()
    {
        var bootstrap = ItemTableBootstrap.Instance;
        if (bootstrap == null) { Debug.LogWarning($"[PackedItemPickup] ItemTableBootstrap.Instance is null on {gameObject.name}"); return; }
        if (bootstrap.Table == null) { Debug.LogWarning($"[PackedItemPickup] bootstrap.Table is null on {gameObject.name}"); return; }
        if (_spriteRenderer == null) { Debug.LogWarning($"[PackedItemPickup] SpriteRenderer is null on {gameObject.name}"); return; }

        var table = bootstrap.Table;
        Sprite sprite = table.SpriteTable?.Get(_reference.Type, _reference.BiomeFlags, _reference.Key);
        if (sprite != null)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sortingOrder = 10; // Draw above ground/tiles
            Debug.Log($"[PackedItemPickup] Sprite applied on {gameObject.name} '{sprite.name}' {sprite.rect.width}x{sprite.rect.height}");
        }
        else
            Debug.LogWarning($"[PackedItemPickup] No sprite for ({_reference.Type},{_reference.BiomeFlags},{_reference.Key}) on {gameObject.name}");

        var collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
            collider.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning($"[PackedItemPickup] Player touched but no PlayerInventory on {other.gameObject.name}");
            return;
        }

        bool added = inventory.Add(_reference);
        Debug.Log($"[PackedItemPickup] Collected by player, Add={added} (inv count={inventory.Count})");
        Destroy(gameObject);
    }

    /// <summary>Set reference at runtime (e.g. from spawner). Call before or in Start.</summary>
    public void SetReference(PackedItemReference r)
    {
        _reference = r;
        if (_spriteRenderer != null || GetComponentInChildren<SpriteRenderer>() != null)
            ApplySprite();
    }
}
