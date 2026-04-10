using System.Collections.Generic;
using Serialization;

namespace Tables
{
    /// <summary>
    /// Saves and loads sprite table data to and from files. Uses FileStorage with delegates.
    /// </summary>
    public static class SpriteTableStorage
    {
        /// <summary>Default filename for sprites. Use with Path.Combine(Application.persistentDataPath, DefaultFileName).</summary>
        public const string DefaultFileName = "sprites.dat";

        /// <summary>
        /// Serializes entries and writes to file. Uses atomic write with .tmp and .bak.
        /// </summary>
        public static void SaveToFile(string path, IReadOnlyList<SpriteTableEntry> entries)
        {
            FileStorage.SaveToFile(path, entries, SpriteTableSerialization.Serialize);
        }

        /// <summary>
        /// Loads from file. Tries main file first, falls back to .bak.
        /// </summary>
        public static SpriteTableLoadResult LoadFromFile(string path)
        {
            return FileStorage.LoadFromFile(path, SpriteTableSerialization.Deserialize);
        }
    }
}
