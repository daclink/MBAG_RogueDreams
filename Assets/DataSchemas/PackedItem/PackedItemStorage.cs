using System;
using System.Collections.Generic;
using Serialization;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Saves and loads packed items to and from files. Format is a list of (block0, block1) only; slot is derived from block0 when loading.
    /// </summary>
    public static class PackedItemStorage
    {
        /// <summary>Default filename for items. Use with Path.Combine(Application.persistentDataPath, DefaultFileName).</summary>
        public const string DefaultFileName = "items.dat";

        /// <summary>
        /// Saves items to file (block0, block1 only). Uses atomic write with .tmp and .bak.
        /// </summary>
        public static void SaveToFile(string path, IReadOnlyList<PackedItemData> items)
        {
            _ = items ?? throw new ArgumentNullException(nameof(items));
            FileStorage.SaveToFile(path, items, PackedItemSerialization.Serialize);
        }

        /// <summary>
        /// Loads from file and returns the flat list of items. Tries main file first, falls back to .bak.
        /// </summary>
        public static PackedItemData[] LoadItemsFromFile(string path)
        {
            PackedItemLoadResult result = LoadResultFromFile(path);
            return result.Items;
        }

        /// <summary>
        /// Loads from file and returns the full result (Items + Version).
        /// </summary>
        public static PackedItemLoadResult LoadResultFromFile(string path)
        {
            return FileStorage.LoadFromFile(path, PackedItemSerialization.Deserialize);
        }
    }
}
