using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DataSchemas.PackedItem;

/// <summary>
/// Equipment slots for weapon and armor. Equip from <see cref="PlayerInventory"/> via
/// <see cref="TryEquipFromInventoryIndex"/> (e.g. from the tab inventory screen). U unequips weapon.
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] PlayerInventory _inventory;
    [SerializeField] Key _unequipKey = Key.U;

    PackedItemReference? _weaponSlot;
    PackedItemReference? _armorSlot;

    public PackedItemReference? Weapon => _weaponSlot;
    public PackedItemReference? Armor => _armorSlot;

    public event Action<ItemType, PackedItemReference?> OnEquipmentChanged;

    void Awake()
    {
        if (_inventory == null)
            _inventory = GetComponent<PlayerInventory>();
    }

    void OnEnable()
    {
        if (_inventory == null)
            _inventory = GetComponent<PlayerInventory>();
        if (_inventory != null)
            _inventory.ItemPickedUp += OnItemPickedUp;
    }

    void OnDisable()
    {
        if (_inventory != null)
            _inventory.ItemPickedUp -= OnItemPickedUp;
    }

    void OnItemPickedUp(PackedItemReference picked)
    {
        if (_inventory == null) return;
        if (_inventory.Count == 0) return;
        int last = _inventory.Count - 1;
        if (!picked.Equals(_inventory.Slots[last])) return;
        if (picked.Type == ItemType.Weapon && !_weaponSlot.HasValue)
            TryEquipFromInventoryIndex(last);
        else if (picked.Type == ItemType.Armor && !_armorSlot.HasValue)
            TryEquipFromInventoryIndex(last);
    }

    void Update()
    {
        if (_inventory == null) return;
        var k = Keyboard.current;
        if (k == null) return;
        if (k[_unequipKey].wasPressedThisFrame)
        {
            if (Unequip(ItemType.Weapon)) Debug.Log("[PlayerEquipment] Unequipped weapon");
        }
    }

    /// <summary>Equips the item at a bag index if it is a Weapon or Armor. Otherwise false.</summary>
    public bool TryEquipFromInventoryIndex(int inventoryIndex)
    {
        if (_inventory == null) return false;
        if (inventoryIndex < 0 || inventoryIndex >= _inventory.Count) return false;
        var r = _inventory.GetAt(inventoryIndex);
        if (!r.HasValue) return false;
        if (r.Value.Type == ItemType.Weapon)
        {
            EquipFromInventory(inventoryIndex, ItemType.Weapon);
            return true;
        }
        if (r.Value.Type == ItemType.Armor)
        {
            EquipFromInventory(inventoryIndex, ItemType.Armor);
            return true;
        }
        return false;
    }

    void EquipFromInventory(int inventoryIndex, ItemType slotType)
    {
        var r = _inventory.GetAt(inventoryIndex);
        if (!r.HasValue) return;

        PackedItemReference? oldEquipped = slotType == ItemType.Weapon ? _weaponSlot : _armorSlot;
        if (oldEquipped.HasValue)
            _inventory.Add(oldEquipped.Value);

        _inventory.RemoveAt(inventoryIndex);

        if (slotType == ItemType.Weapon)
            _weaponSlot = r.Value;
        else
            _armorSlot = r.Value;

        Debug.Log($"[PlayerEquipment] Equipped {slotType}: ({r.Value.Type},{r.Value.BiomeFlags},{r.Value.Key})");
        OnEquipmentChanged?.Invoke(slotType, r);
    }

    public bool Unequip(ItemType slotType)
    {
        if (_inventory == null || _inventory.Count >= _inventory.MaxSlots) return false;

        PackedItemReference? current = slotType == ItemType.Weapon ? _weaponSlot : _armorSlot;
        if (!current.HasValue) return false;

        _inventory.Add(current.Value);
        if (slotType == ItemType.Weapon)
            _weaponSlot = null;
        else
            _armorSlot = null;

        OnEquipmentChanged?.Invoke(slotType, null);
        return true;
    }

    /// <summary>Clears weapon slot without returning to inventory (e.g. when thrown).</summary>
    public bool ConsumeWeapon()
    {
        if (!_weaponSlot.HasValue) return false;
        _weaponSlot = null;
        OnEquipmentChanged?.Invoke(ItemType.Weapon, null);
        return true;
    }
}
