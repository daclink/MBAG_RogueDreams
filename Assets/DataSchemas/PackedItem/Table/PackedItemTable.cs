using System;
using System.Collections.Generic;
using UnityEngine;
using Tables;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Canonical item table. Owns slot layout and free lists. Writes sprites to SpriteTable2D and text to TextTable2D at same (type, partition, key).
    /// Add allocates key, stores item with SpriteKey=TextKey=key, sets spritesheet and text. Remove frees key, clears both.
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

        public PackedItemTable(SpriteTable2D spriteTable, TextTable2D textTable)
        {
            _spriteTable = spriteTable ?? throw new ArgumentNullException(nameof(spriteTable));
            _textTable = textTable ?? throw new ArgumentNullException(nameof(textTable));
            _items = new PackedItemData?[TotalSlots];
            _core = new PackedItemTableCore();
        }

        /// <summary>Sprite table reference. Can be set or changed.</summary>
        public SpriteTable2D SpriteTable
        {
            get => _spriteTable;
            set => _spriteTable = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Text table reference. Required for Add/Update/Remove; must not be null.</summary>
        public TextTable2D TextTable
        {
            get => _textTable;
            set => _textTable = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Gets partition index for biome flags.</summary>
        public static int GetPartitionIndex(BiomeFlags biomeFlags) => PackedItemTableCore.GetPartitionIndex(biomeFlags);

        /// <summary>Gets flat index for (type, partition, key).</summary>
        public static int GetFlatIndex(ItemType type, int partition, byte key) =>
            PackedItemTableCore.GetFlatIndex(type, partition, key);

        /// <summary>Adds an item. Allocates key, stores item with SpriteKey=TextKey=key, sets spritesheet and text. Returns key or -1 if full.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public int Add(ItemType type, BiomeFlags biomeFlags, Sprite[] spritesheet, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text)
        {
            if (spritesheet == null || spritesheet.Length == 0) return -1;
            _ = text ?? throw new ArgumentNullException(nameof(text));

            int key = _core.Add(type, biomeFlags);
            if (key < 0) return -1;

            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            byte keyByte = (byte)key;

            PackedItemData data = BuildPackedItemData(type, biomeFlags, keyByte, aspectFlags, statusFlags, rarityFlags,
                health, power, armor, agility, vigor, fortune, range, rarity);

            WriteItemAt(type, partition, keyByte, data);
            WriteSpriteAndText(type, biomeFlags, keyByte, spritesheet, text);

            return key;
        }

        /// <summary>Adds an item with a single sprite (frame 0 only).</summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public int Add(ItemType type, BiomeFlags biomeFlags, Sprite sprite, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text)
        {
            return Add(type, biomeFlags, sprite != null ? new[] { sprite } : null, aspectFlags, statusFlags, rarityFlags, health, power, armor, agility, vigor, fortune, range, rarity, text);
        }

        /// <summary>Removes item, sprite, and text at (type, biomeFlags, key). Frees the slot.</summary>
        public void Remove(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            TryClearSlot(type, biomeFlags, key);
        }

        /// <summary>Updates item at (type, oldBiomeFlags, key). If newBiomeFlags differs, moves to new partition.
        /// Returns (success, newKeyIfMoved). When moved, newKeyIfMoved has the new key; new partition = GetPartitionIndex(newBiomeFlags).</summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public (bool success, byte? newKeyIfMoved) Update(ItemType type, BiomeFlags oldBiomeFlags, byte key, Sprite[] spritesheet, BiomeFlags newBiomeFlags, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));

            PackedItemData? existing = Get(type, oldBiomeFlags, key);

            if (!existing.HasValue) return (false, null);
            if (spritesheet == null || spritesheet.Length == 0) return (false, null);

            int oldPartition = PackedItemTableCore.GetPartitionIndex(oldBiomeFlags);
            int newPartition = PackedItemTableCore.GetPartitionIndex(newBiomeFlags);

            if (newPartition == oldPartition)
            {
                byte keyByte = key;

                PackedItemData data = BuildPackedItemData(type, newBiomeFlags, keyByte, aspectFlags, statusFlags, rarityFlags,
                    health, power, armor, agility, vigor, fortune, range, rarity);

                WriteItemAt(type, oldPartition, keyByte, data);
                WriteSpriteAndText(type, newBiomeFlags, keyByte, spritesheet, text);

                return (true, null);
            }

            int newKey = Add(type, newBiomeFlags, spritesheet, aspectFlags, statusFlags, rarityFlags, health, power, armor, agility, vigor, fortune, range, rarity, text);

            if (newKey < 0) return (false, null);
            TryClearSlot(type, oldBiomeFlags, key);
            return (true, (byte)newKey);
        }

        /// <summary>Updates item with a single sprite (frame 0 only).</summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public (bool success, byte? newKeyIfMoved) Update(ItemType type, BiomeFlags oldBiomeFlags, byte key, Sprite sprite, BiomeFlags newBiomeFlags, AspectFlags aspectFlags, StatusFlags statusFlags,
            RarityFlags rarityFlags, sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity, string text)
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

        /// <summary>Saves used slots to items.dat (block0, block1 only). Caller should also save sprites to sprites.dat.</summary>
        public void SaveToFile(string itemsPath)
        {
            if (string.IsNullOrWhiteSpace(itemsPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(itemsPath));

            PackedItemStorage.SaveToFile(itemsPath, CollectUsedItems());
        }

        /// <summary>Loads from items file and populates table by deriving slot from each item's Block0. Does not load sprites.</summary>
        public void LoadFromFile(string itemsPath)
        {
            if (string.IsNullOrWhiteSpace(itemsPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(itemsPath));

            PackedItemData[] items = PackedItemStorage.LoadItemsFromFile(itemsPath);
            Array.Clear(_items, 0, _items.Length);
            _core.ResetAllFree();

            foreach (PackedItemData data in items)
                ApplyLoadedEntry(data);
        }

        /// <summary>Loads items, then sprites, then texts. Paths must match saved (type, partition, key).</summary>
        public void LoadFromFiles(string itemsPath, string spritesPath, string textsPath = null)
        {
            LoadFromFile(itemsPath);
            if (!string.IsNullOrWhiteSpace(spritesPath))
                _spriteTable.LoadFromFile(spritesPath);
            if (!string.IsNullOrWhiteSpace(textsPath))
                _textTable.LoadFromFile(textsPath);
        }

        private static PackedItemData BuildPackedItemData(
            ItemType type,
            BiomeFlags biomeFlags,
            byte keyByte,
            AspectFlags aspectFlags,
            StatusFlags statusFlags,
            RarityFlags rarityFlags,
            sbyte health,
            sbyte power,
            sbyte armor,
            sbyte agility,
            sbyte vigor,
            sbyte fortune,
            sbyte range,
            sbyte rarity)
        {
            ulong block0 = PackedItemSchema.PackBlock0(type, aspectFlags, statusFlags, biomeFlags, rarityFlags, keyByte, keyByte);
            ulong block1 = PackedItemSchema.PackBlock1(health, power, armor, agility, vigor, fortune, range, rarity);
            return new PackedItemData(block0, block1);
        }

        private void WriteItemAt(ItemType type, int partition, byte keyByte, PackedItemData data)
        {
            int idx = PackedItemTableCore.GetFlatIndex(type, partition, keyByte);
            _items[idx] = data;
        }

        private void WriteSpriteAndText(ItemType type, BiomeFlags biomeFlags, byte keyByte, Sprite[] spritesheet, string text)
        {
            _spriteTable.SetAt(type, biomeFlags, keyByte, spritesheet);
            _textTable.SetAt(type, biomeFlags, keyByte, text);
        }

        /// <summary>Clears packed item, sprite, and text for the slot and returns the key to the free list. No-op if slot empty.</summary>
        private void TryClearSlot(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int idx = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (idx < 0 || idx >= _items.Length || !_items[idx].HasValue)
                return;

            _items[idx] = null;
            _spriteTable.ClearAt(type, biomeFlags, key);
            _textTable.ClearAt(type, biomeFlags, key);
            _core.Remove(type, biomeFlags, key);
        }

        private void ApplyLoadedEntry(PackedItemData data)
        {
            int partition = PackedItemTableCore.GetPartitionIndex(data.BiomeFlags);
            int idx = PackedItemTableCore.GetFlatIndex(data.ItemType, partition, data.SpriteKey);
            if (idx < 0 || idx >= _items.Length)
                return;

            _items[idx] = data;
            _core.MarkUsed(data.ItemType, data.BiomeFlags, data.SpriteKey);
        }

        private List<PackedItemData> CollectUsedItems()
        {
            var items = new List<PackedItemData>();
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i].HasValue)
                    items.Add(_items[i].Value);
            }
            return items;
        }
    }
}
