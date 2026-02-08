using System.Collections.Generic;
using UnityEngine;

namespace Tables
{
    /// <summary>
    /// Lookup table for sprites. Keys are 0-255 (byte). Entries can be removed.
    /// Defaulted slots are reused when adding new entries.
    /// </summary>
    [CreateAssetMenu(menuName = "Tables/Sprite Lookup Table", fileName = "SpriteLookupTable")]
    public class SpriteLookupTable : ScriptableObject
    {
        public const int MaxKey = 256;

        [SerializeField] private List<Sprite> entries = new List<Sprite>();

        /// <summary>Returns the sprite at the given key, or null if out of range or defaulted.</summary>
        public Sprite Get(byte key) => Get((int)key);

        /// <summary>Returns the sprite at the given key, or null if out of range or defaulted.</summary>
        public Sprite Get(int key)
        {
            if (key < 0 || key >= entries.Count) return null;
            return entries[key];
        }

        /// <summary>Adds a sprite and returns its key. Reuses first null slot, else appends (up to MaxKey). Returns -1 if full.</summary>
        public int Add(Sprite sprite)
        {
            if (sprite == null) return -1;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null)
                {
                    entries[i] = sprite;
                    return i;
                }
            }

            if (entries.Count >= MaxKey) return -1;
            entries.Add(sprite);
            return entries.Count - 1;
        }

        /// <summary>Removes the entry at key by setting it to null. The slot can be reused by Add.</summary>
        public void Remove(byte key) => Remove((int)key);

        /// <summary>Removes the entry at key by setting it to null. The slot can be reused by Add.</summary>
        public void Remove(int key)
        {
            if (key >= 0 && key < entries.Count)
                entries[key] = null;
        }

        /// <summary>Number of slots in the list (includes defaulted slots).</summary>
        public int Count => entries?.Count ?? 0;

        /// <summary>True if the key is in range and the slot is non-null.</summary>
        public bool HasEntry(byte key) => HasEntry((int)key);

        public bool HasEntry(int key)
        {
            if (key < 0 || key >= entries.Count) return false;
            return entries[key] != null;
        }
        
        /// <summary>Returns the key of the first entry that matches the sprite (reference equality), or -1 if not found.</summary>
        public int GetKeyOf(Sprite sprite)
        {
            if (sprite == null || entries == null) return -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == sprite) return i;
            }
            return -1;
        }
    }
}
