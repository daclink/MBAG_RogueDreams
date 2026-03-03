using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DataSchemas.PackedItem;

/// <summary>
/// Equipment slots that hold packed item references. Press Tab to equip first available weapon/armor from inventory.
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] PlayerInventory _inventory;
    [SerializeField] Key _equipKey = Key.Tab;
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

    void Update()
    {
        if (_inventory == null) return;
        var k = Keyboard.current;
        if (k == null) return;
        // Tab can be consumed by UI; I is fallback for equip
        if (k[_equipKey].wasPressedThisFrame || k[Key.I].wasPressedThisFrame)
        {
            Debug.Log("[PlayerEquipment] Equip key pressed");
            TryEquipFirst();
        }
        // U to unequip weapon
        else if (k[_unequipKey].wasPressedThisFrame)
        {
            if (Unequip(ItemType.Weapon)) Debug.Log("[PlayerEquipment] Unequipped weapon");
        }
    }

        /// <summary>Equips the first Weapon or Armor from inventory into the matching slot.</summary>
        public bool TryEquipFirst()
        {
            if (_inventory == null) { Debug.LogWarning("[PlayerEquipment] TryEquipFirst: _inventory is null"); return false; }
            if (_inventory.Count == 0) { Debug.Log("[PlayerEquipment] TryEquipFirst: inventory empty"); return false; }

            for (int i = 0; i < _inventory.Count; i++)
            {
                var r = _inventory.GetAt(i);
                if (!r.HasValue) continue;

                if (r.Value.Type == ItemType.Weapon)
                {
                    EquipFromInventory(i, ItemType.Weapon);
                    return true;
                }
                if (r.Value.Type == ItemType.Armor)
                {
                    EquipFromInventory(i, ItemType.Armor);
                    return true;
                }
            }
            Debug.Log("[PlayerEquipment] TryEquipFirst: no Weapon or Armor in inventory");
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
