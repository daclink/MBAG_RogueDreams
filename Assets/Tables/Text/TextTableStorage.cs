using System.Collections.Generic;
using Serialization;

namespace Tables
{
    /// <summary>
    /// Saves and loads text table data to and from files. Uses FileStorage with delegates.
    /// </summary>
    public static class TextTableStorage
    {
        /// <summary>Default filename for texts. Use with Path.Combine(Application.persistentDataPath, DefaultFileName).</summary>
        public const string DefaultFileName = "texts.dat";

        /// <summary>
        /// Serializes entries and writes to file. Uses atomic write with .tmp and .bak.
        /// </summary>
        public static void SaveToFile(string path, IReadOnlyList<TextTableEntry> entries)
        {
            FileStorage.SaveToFile(path, entries, TextTableSerialization.Serialize);
        }

        /// <summary>
        /// Loads from file. Tries main file first, falls back to .bak.
        /// </summary>
        public static TextTableLoadResult LoadFromFile(string path)
        {
            return FileStorage.LoadFromFile(path, TextTableSerialization.Deserialize);
        }
    }
}
