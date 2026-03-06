using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DataSchemas.PackedItem;

namespace Tables
{
    /// <summary>
    /// Storage for text at (type, partition, key). Layout from PackedItemTableCore.
    /// PackedItemTable is canonical; use SetAt/ClearAt. GetText(data) for lookup.
    /// </summary>
    [CreateAssetMenu(menuName = "Tables/Text Table 2D", fileName = "TextTable2D")]
    public class TextTable2D : ScriptableObject
    {
        public const int TypeCount = PackedItemTableCore.TypeCount;
        public const int BiomePartitionCount = PackedItemTableCore.BiomePartitionCount;
        public const int SlotsPerBiome = PackedItemTableCore.SlotsPerBiome;
        public const int KeysPerType = BiomePartitionCount * SlotsPerBiome;
        public const int TotalSlots = PackedItemTableCore.TotalSlots;

        public static int GetPartitionIndex(BiomeFlags biomeFlags) => PackedItemTableCore.GetPartitionIndex(biomeFlags);

        [System.NonSerialized] private string[] _texts;

        void OnEnable()
        {
            EnsureCapacity();
        }

        void EnsureCapacity()
        {
            if (_texts == null || _texts.Length < TotalSlots)
            {
                var prev = _texts;
                _texts = new string[TotalSlots];
                if (prev != null)
                    Array.Copy(prev, _texts, Math.Min(prev.Length, TotalSlots));
            }
        }

        /// <summary>Number of non-empty slots. Used by custom inspector.</summary>
        public int GetUsedSlotCount()
        {
            if (_texts == null) return 0;
            int n = 0;
            for (int i = 0; i < _texts.Length; i++)
                if (!string.IsNullOrEmpty(_texts[i])) n++;
            return n;
        }

        /// <summary>Returns the text at [type][partition from biomeFlags][key].</summary>
        public string Get(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            EnsureCapacity();
            if (_texts == null) return string.Empty;
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int i = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (i < 0 || i >= _texts.Length) return string.Empty;
            return _texts[i] ?? string.Empty;
        }

        /// <summary>Convenience: returns the text for the given item data.</summary>
        public string GetText(PackedItemData data)
        {
            return Get(data.ItemType, data.BiomeFlags, data.TextKey);
        }

        /// <summary>Sets text at (type, partition, key). PackedItemTable allocates slots.</summary>
        public void SetAt(ItemType type, BiomeFlags biomeFlags, byte key, string text)
        {
            EnsureCapacity();
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int i = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (i >= 0 && i < _texts.Length)
                _texts[i] = text ?? string.Empty;
        }

        /// <summary>Clears text at (type, partition, key).</summary>
        public void ClearAt(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            EnsureCapacity();
            if (_texts == null) return;
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int i = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (i >= 0 && i < _texts.Length)
                _texts[i] = null;
        }

        /// <summary>True if the slot has non-empty text.</summary>
        public bool HasEntry(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            return !string.IsNullOrEmpty(Get(type, biomeFlags, key));
        }

        /// <summary>Saves used slots to file.</summary>
        public void SaveFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            EnsureCapacity();
            if (_texts == null) return;

            List<TextTableEntry> entries = new List<TextTableEntry>();
            for (int t = 0; t < TypeCount; t++)
            {
                for (int p = 0; p < BiomePartitionCount; p++)
                {
                    int baseIdx = t * KeysPerType + p * SlotsPerBiome;
                    for (byte k = 0; k < SlotsPerBiome; k++)
                    {
                        string text = _texts[baseIdx + k];
                        if (string.IsNullOrEmpty(text)) continue;
                        entries.Add(new TextTableEntry((byte)t, (byte)p, k, text));
                    }
                }
            }
            TextTableStorage.SaveToFile(path, entries);
        }

        /// <summary>Loads from file, populates table. Clears existing entries first.</summary>
        public void LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            TextTableLoadResult result = TextTableStorage.LoadFromFile(path);
            EnsureCapacity();
            Array.Clear(_texts, 0, _texts.Length);

            foreach (TextTableEntry entry in result.Entries)
            {
                int idx = PackedItemTableCore.GetFlatIndex((ItemType)entry.Type, entry.Partition, entry.Key);
                if (idx >= 0 && idx < _texts.Length)
                    _texts[idx] = entry.Text ?? string.Empty;
            }
        }
    }
}
