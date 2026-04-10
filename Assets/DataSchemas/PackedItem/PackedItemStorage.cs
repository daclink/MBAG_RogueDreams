using System;
using System.Collections.Generic;
using Serialization;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Saves and loads lists of PackedItemData to and from files. Uses FileStorage with delegates.
    /// </summary>
    public static class PackedItemStorage
    {
        /// <summary>Default filename for items. Use with Path.Combine(Application.persistentDataPath, DefaultFileName).</summary>
        public const string DefaultFileName = "items.dat";

        /// <summary>
        /// Serializes entries to file. Uses atomic write with .tmp and .bak.
        /// </summary>
        public static void SaveToFile(string path, IReadOnlyList<PackedItemEntry> entries)
        {
            FileStorage.SaveToFile(path, entries, PackedItemSerialization.Serialize);
        }

        /// <summary>
        /// Converts flat list to entries (type, partition from BiomeFlags, key from SpriteKey) and saves.
        /// </summary>
        public static void SaveToFile(string path, IReadOnlyList<PackedItemData> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            var entries = new List<PackedItemEntry>(items.Count);
            foreach (PackedItemData d in items)
            {
                int p = PackedItemTableCore.GetPartitionIndex(d.BiomeFlags);
                entries.Add(new PackedItemEntry((byte)d.ItemType, (byte)p, d.SpriteKey, d.Block0, d.Block1));
            }
            SaveToFile(path, entries);
        }

        /// <summary>
        /// Loads items from file. Tries main file first, falls back to .bak. Returns Items from the load result.
        /// </summary>
        public static List<PackedItemData> LoadFromFile(string path)
        {
            PackedItemLoadResult result = FileStorage.LoadFromFile(path, PackedItemSerialization.Deserialize);
            return result.Items;
        }
    }
}
