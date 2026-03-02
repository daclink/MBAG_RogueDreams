using System;
using System.Collections.Generic;
using Serialization;
using UnityEngine;
using Tables;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Canonical item table. Owns slot layout and free lists. Writes sprites to SpriteTable2D and text to TextTable2D at same (type, partition, key).
    /// Add allocates key, stores item, sets sprite and optionally text. Remove frees key, clears both.
    /// </summary>
    public class PackedItemTable
    {
        public const int TypeCount = PackedItemTableCore.TypeCount;
        public const int BiomePartitionCount = PackedItemTableCore.BiomePartitionCount;
        public const int SlotsPerBiome = PackedItemTableCore.SlotsPerBiome;
        public const int TotalSlots = PackedItemTableCore.TotalSlots;

        private readonly PackedItemData?[] _items;
        private readonly PackedItemTableCore _core;
        private SpriteTable2D _spriteTable;
        private TextTable2D _textTable;

        public PackedItemTable(SpriteTable2D spriteTable, TextTable2D textTable = null)
        {
            _spriteTable = spriteTable ?? throw new ArgumentNullException(nameof(spriteTable));
            _textTable = textTable;
            _items = new PackedItemData?[TotalSlots];
            _core = new PackedItemTableCore();
        }

        /// <summary>Sprite table reference. Can be set or changed.</summary>
        public SpriteTable2D SpriteTable
        {
            get => _spriteTable;
            set => _spriteTable = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Text table reference. Optional; when set, Add/Update/Remove also update text.</summary>
        public TextTable2D TextTable
        {
            get => _textTable;
            set => _textTable = value;
        }

        /// <summary>Gets partition index for biome flags.</summary>
        public static int GetPartitionIndex(BiomeFlags biomeFlags) => PackedItemTableCore.GetPartitionIndex(biomeFlags);

        /// <summary>Gets flat index for (type, partition, key).</summary>
        public static int GetFlatIndex(ItemType type, int partition, byte key) =>
            PackedItemTableCore.GetFlatIndex(type, partition, key);

        /// <summary>Rebuilds free lists from empty slots. Call after load.</summary>
        public void RebuildFreeLists()
        {
            _core.RebuildFreeLists(i => !_items[i].HasValue);
        }

        /// <summary>Adds an item. Allocates key, stores item with SpriteKey=TextKey=key, sets spritesheet and optionally text. Returns key or -1 if full.</summary>
        public int Add(ItemType type, BiomeFlags biomeFlags, Sprite[] spritesheet, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text = null)
        {
            if (spritesheet == null || spritesheet.Length == 0) return -1;

            int key = _core.Add(type, biomeFlags, i => !_items[i].HasValue);
            if (key < 0) return -1;

            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            byte keyByte = (byte)key;
            ulong block0 = PackedItemSchema.PackBlock0(type, aspectFlags, statusFlags, biomeFlags, rarityFlags, keyByte, keyByte);
            ulong block1 = PackedItemSchema.PackBlock1(health, power, armor, agility, vigor, fortune, range, rarity);

            int idx = PackedItemTableCore.GetFlatIndex(type, partition, keyByte);
            _items[idx] = new PackedItemData(block0, block1);
            _spriteTable.SetAt(type, biomeFlags, keyByte, spritesheet);
            if (_textTable != null)
                _textTable.SetAt(type, biomeFlags, keyByte, text ?? string.Empty);
            return key;
        }

        /// <summary>Adds an item with a single sprite (frame 0 only).</summary>
        public int Add(ItemType type, BiomeFlags biomeFlags, Sprite sprite, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text = null)
        {
            return Add(type, biomeFlags, sprite != null ? new[] { sprite } : null, aspectFlags, statusFlags, rarityFlags, health, power, armor, agility, vigor, fortune, range, rarity, text);
        }

        /// <summary>Removes item, sprite, and text at (type, biomeFlags, key). Frees the slot.</summary>
        public void Remove(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int idx = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (idx >= 0 && idx < _items.Length && _items[idx].HasValue)
            {
                _items[idx] = null;
                _spriteTable.ClearAt(type, biomeFlags, key);
                _textTable?.ClearAt(type, biomeFlags, key);
                _core.Remove(type, biomeFlags, key);
            }
        }

        /// <summary>Updates item at (type, oldBiomeFlags, key). If newBiomeFlags differs, moves to new partition.
        /// Returns (success, newKeyIfMoved). When moved, newKeyIfMoved has the new key; new partition = GetPartitionIndex(newBiomeFlags).</summary>
        public (bool success, byte? newKeyIfMoved) Update(ItemType type, BiomeFlags oldBiomeFlags, byte key, Sprite[] spritesheet, BiomeFlags newBiomeFlags, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text = null)
        {
            var existing = Get(type, oldBiomeFlags, key);
            if (!existing.HasValue) return (false, null);
            if (spritesheet == null || spritesheet.Length == 0) return (false, null);

            int oldPartition = PackedItemTableCore.GetPartitionIndex(oldBiomeFlags);
            int newPartition = PackedItemTableCore.GetPartitionIndex(newBiomeFlags);

            if (newPartition == oldPartition)
            {
                byte keyByte = key;
                ulong block0 = PackedItemSchema.PackBlock0(type, aspectFlags, statusFlags, newBiomeFlags, rarityFlags, keyByte, keyByte);
                ulong block1 = PackedItemSchema.PackBlock1(health, power, armor, agility, vigor, fortune, range, rarity);
                int idx = PackedItemTableCore.GetFlatIndex(type, oldPartition, keyByte);
                _items[idx] = new PackedItemData(block0, block1);
                _spriteTable.SetAt(type, newBiomeFlags, keyByte, spritesheet);
                if (_textTable != null)
                    _textTable.SetAt(type, newBiomeFlags, keyByte, text ?? string.Empty);
                return (true, null);
            }

            int newKey = Add(type, newBiomeFlags, spritesheet, aspectFlags, statusFlags, rarityFlags, health, power, armor, agility, vigor, fortune, range, rarity, text);
            if (newKey < 0) return (false, null);
            Remove(type, oldBiomeFlags, key);
            return (true, (byte)newKey);
        }

        /// <summary>Updates item with a single sprite (frame 0 only).</summary>
        public (bool success, byte? newKeyIfMoved) Update(ItemType type, BiomeFlags oldBiomeFlags, byte key, Sprite sprite, BiomeFlags newBiomeFlags, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text = null)
        {
            return Update(type, oldBiomeFlags, key, sprite != null ? new[] { sprite } : null, newBiomeFlags, aspectFlags, statusFlags, rarityFlags, health, power, armor, agility, vigor, fortune, range, rarity, text);
        }

        /// <summary>Returns the item at (type, biomeFlags, key), or null.</summary>
        public PackedItemData? Get(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int idx = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (idx < 0 || idx >= _items.Length) return null;
            return _items[idx];
        }

        /// <summary>True if the slot has an item.</summary>
        public bool HasEntry(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            return Get(type, biomeFlags, key).HasValue;
        }

        /// <summary>True if flat index has an item. Used for RebuildFreeLists callbacks.</summary>
        public bool HasEntryAt(int flatIndex)
        {
            return flatIndex >= 0 && flatIndex < _items.Length && _items[flatIndex].HasValue;
        }

        /// <summary>Saves used slots to items.dat. Caller should also save sprites to sprites.dat.</summary>
        public void SaveToFile(string itemsPath)
        {
            if (string.IsNullOrWhiteSpace(itemsPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(itemsPath));

            List<PackedItemEntry> entries = new List<PackedItemEntry>();
            for (int t = 0; t < TypeCount; t++)
            {
                for (int p = 0; p < BiomePartitionCount; p++)
                {
                    int baseIdx = t * PackedItemTableCore.KeysPerType + p * SlotsPerBiome;
                    for (byte k = 0; k < SlotsPerBiome; k++)
                    {
                        if (!_items[baseIdx + k].HasValue) continue;
                        PackedItemData data = _items[baseIdx + k].Value;
                        entries.Add(new PackedItemEntry((byte)t, (byte)p, k, data.Block0, data.Block1));
                    }
                }
            }
            FileStorage.SaveToFile(itemsPath, entries, PackedItemSerialization.Serialize);
        }

        /// <summary>Loads from items file, populates table, rebuilds free lists. Clears existing entries. Does not load sprites.</summary>
        public void LoadFromFile(string itemsPath)
        {
            if (string.IsNullOrWhiteSpace(itemsPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(itemsPath));

            PackedItemLoadResult result = FileStorage.LoadFromFile(itemsPath, PackedItemSerialization.Deserialize);
            Array.Clear(_items, 0, _items.Length);

            foreach (PackedItemEntry entry in result.Entries)
            {
                int idx = PackedItemTableCore.GetFlatIndex((ItemType)entry.Type, entry.Partition, entry.Key);
                if (idx >= 0 && idx < _items.Length)
                    _items[idx] = entry.ToData();
            }
            RebuildFreeLists();
        }

        /// <summary>Loads items, then sprites, then texts. Paths must match saved (type, partition, key).</summary>
        public void LoadFromFiles(string itemsPath, string spritesPath, string textsPath = null)
        {
            LoadFromFile(itemsPath);
            if (!string.IsNullOrWhiteSpace(spritesPath) && _spriteTable != null)
                _spriteTable.LoadFromFile(spritesPath);
            if (!string.IsNullOrWhiteSpace(textsPath) && _textTable != null)
                _textTable.LoadFromFile(textsPath);
        }
    }
}
