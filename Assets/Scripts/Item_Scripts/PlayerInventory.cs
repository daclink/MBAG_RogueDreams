using System.Collections.Generic;
using UnityEngine;
using DataSchemas.PackedItem;

/// <summary>
/// Holds packed item references. Add to player. Equipment swaps references with this.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
        [SerializeField] int _maxSlots = 20;

        readonly List<PackedItemReference> _slots = new List<PackedItemReference>();

        public IReadOnlyList<PackedItemReference> Slots => _slots;
        public int Count => _slots.Count;
        public int MaxSlots => _maxSlots;

        public bool Add(PackedItemReference reference)
        {
            if (_slots.Count >= _maxSlots)
            {
                Debug.LogWarning($"[PlayerInventory] Add rejected - full ({_slots.Count}/{_maxSlots})");
                return false;
            }
            _slots.Add(reference);
            Debug.Log($"[PlayerInventory] Added ({reference.Type},{reference.BiomeFlags},{reference.Key}), count={_slots.Count}");
            return true;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _slots.Count) return false;
            _slots.RemoveAt(index);
            return true;
        }

        public PackedItemReference? GetAt(int index)
        {
            if (index < 0 || index >= _slots.Count) return null;
            return _slots[index];
        }

        public bool TryRemove(PackedItemReference reference, out int removedIndex)
        {
            removedIndex = -1;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Equals(reference))
                {
                    _slots.RemoveAt(i);
                    removedIndex = i;
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(PackedItemReference reference)
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].Equals(reference)) return i;
            return -1;
        }
}
