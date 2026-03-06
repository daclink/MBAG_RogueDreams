using UnityEngine;
using DataSchemas.PackedItem;

/// <summary>
/// Renders equipped weapon/armor sprite on top of the player. Add to same GameObject as PlayerEquipment.
/// </summary>
[RequireComponent(typeof(PlayerEquipment))]
public class EquippedItemVisual : MonoBehaviour
{
    [SerializeField] Transform _attachPoint;
    [SerializeField] Vector3 _weaponOffset = new Vector3(0.4f, 0.2f, 0);
    [SerializeField] int _sortingOrder = 5;

    PlayerEquipment _equipment;
    GameObject _weaponVisual;
    SpriteRenderer _weaponRenderer;

    void Awake()
    {
        _equipment = GetComponent<PlayerEquipment>();
    }

    void OnEnable()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged += OnEquipmentChanged;
        RefreshVisual();
    }

    void OnDisable()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
    }

    void OnEquipmentChanged(ItemType slotType, PackedItemReference? reference)
    {
        RefreshVisual();
    }

    void RefreshVisual()
    {
        Transform parent = _attachPoint != null ? _attachPoint : transform;
        var weapon = _equipment?.Weapon;

        if (!weapon.HasValue)
        {
            if (_weaponVisual != null)
                _weaponVisual.SetActive(false);
            return;
        }

        var bootstrap = ItemTableBootstrap.Instance;
        if (bootstrap == null || bootstrap.Table?.SpriteTable == null) return;

        Sprite sprite = bootstrap.Table.SpriteTable.Get(weapon.Value.Type, weapon.Value.BiomeFlags, weapon.Value.Key);
        if (sprite == null) return;

        if (_weaponVisual == null)
        {
            _weaponVisual = new GameObject("EquippedWeaponVisual");
            _weaponVisual.transform.SetParent(parent, false);
            _weaponVisual.transform.localPosition = _weaponOffset;
            _weaponVisual.transform.localRotation = Quaternion.identity;
            _weaponVisual.transform.localScale = Vector3.one;
            _weaponRenderer = _weaponVisual.AddComponent<SpriteRenderer>();
            _weaponRenderer.sortingOrder = _sortingOrder;
        }

        _weaponRenderer.sprite = sprite;
        _weaponVisual.SetActive(true);
    }
}
