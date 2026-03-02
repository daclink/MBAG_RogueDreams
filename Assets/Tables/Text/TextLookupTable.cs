using System.Collections.Generic;
using UnityEngine;

namespace Tables
{
    /// <summary>
    /// Lookup table for display text. Keys are 0-255 (byte). Entries can be removed (set to default).
    /// Defaulted slots are reused when adding new entries.
    /// </summary>
    [CreateAssetMenu(menuName = "Tables/Text Lookup Table", fileName = "TextLookupTable")]
    public class TextLookupTable : ScriptableObject
    {
        public const int MaxKey = 256;

        [SerializeField] private List<string> entries = new List<string>();

        /// <summary>Returns the text at the given key, or empty string if out of range or defaulted.</summary>
        public string Get(byte key) => Get((int)key);

        /// <summary>Returns the text at the given key, or empty string if out of range or defaulted.</summary>
        public string Get(int key)
        {
            if (key < 0 || key >= entries.Count) return string.Empty;
            var value = entries[key];
            return string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        /// <summary>Adds text and returns its key. Reuses first defaulted slot, else appends (up to MaxKey). Returns -1 if full.</summary>
        public int Add(string text)
        {
            if (string.IsNullOrEmpty(text)) return -1;

            for (int i = 0; i < entries.Count; i++)
            {
                if (string.IsNullOrEmpty(entries[i]))
                {
                    entries[i] = text;
                    return i;
                }
            }

            if (entries.Count >= MaxKey) return -1;
            entries.Add(text);
            return entries.Count - 1;
        }

        /// <summary>Removes the entry at key by setting it to default. The slot can be reused by Add.</summary>
        public void Remove(byte key) => Remove((int)key);

        /// <summary>Removes the entry at key by setting it to default. The slot can be reused by Add.</summary>
        public void Remove(int key)
        {
            if (key >= 0 && key < entries.Count)
                entries[key] = string.Empty;
        }

        /// <summary>Number of slots in the list (includes defaulted slots).</summary>
        public int Count => entries?.Count ?? 0;

        /// <summary>True if the key is in range and the slot is non-default.</summary>
        public bool HasEntry(byte key) => HasEntry((int)key);

        public bool HasEntry(int key)
        {
            if (key < 0 || key >= entries.Count) return false;
            return !string.IsNullOrEmpty(entries[key]);
        }
        
        /// <summary>Returns the key of the first entry that matches the text, or -1 if not found.</summary>
        public int GetKeyOf(string text)
        {
            if (string.IsNullOrEmpty(text) || entries == null) return -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] == text) return i;
            }
            return -1;
        }
    }
}
