using System;
using System.Collections.Generic;
using System.IO;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Saves and loads lists of PackedItemData to and from files.
    /// Uses PackedItemSerialization for the binary format.
    /// </summary>
    public static class PackedItemStorage
    {
        /// <summary>Default filename for items data. Use with Path.Combine(Application.persistentDataPath, PackedItemStorage.DefaultFileName).</summary>
        public const string DefaultFileName = "items.dat";
        private const string TempExtension = ".tmp";
        private const string BackupExtension = ".bak";

        /// <summary>
        /// Serializes the items and writes them to the specified file.
        /// Writes to a .tmp file first; on success, replaces the main file and keeps the previous version as .bak.
        /// If the write fails, the main file is left unchanged.
        /// </summary>
        /// <param name="path">Full path to the file (e.g. Application.persistentDataPath + "/items.dat")</param>
        /// <param name="items">Items to save</param>
        /// <exception cref="ArgumentNullException">Thrown if path or items is null.</exception>
        /// <exception cref="ArgumentException">Thrown if path is empty or whitespace.</exception>
        public static void SaveToFile(string path, IReadOnlyList<PackedItemData> items)
        {
            _ = path is not null                 ? 0 : throw new ArgumentNullException(nameof(path));
            _ = !string.IsNullOrWhiteSpace(path) ? 0 : throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));
            _ = items is not null                ? 0 : throw new ArgumentNullException(nameof(items));

            string tmpPath = path + TempExtension;
            try
            {
                using (FileStream stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None)) 
                {
                    PackedItemSerialization.Serialize(stream, items);
                }

                string bakPath = path + BackupExtension;
                if (File.Exists(path))
                    File.Replace(tmpPath, path, bakPath);
                else
                    File.Move(tmpPath, path);
            }
            finally
            {
                if (File.Exists(tmpPath))
                {
                    try { File.Delete(tmpPath); }
                    catch { /* Best-effort cleanup of orphaned .tmp */ }
                }
            }
        }

        /// <summary>
        /// Reads the file and deserializes the items.
        /// Tries the main file first. If it is missing or deserialization fails (e.g. corrupted or truncated),
        /// attempts to load from the .bak backup file. Throws if neither succeeds.
        /// </summary>
        /// <param name="path">Full path to the file (e.g. Application.persistentDataPath + "/items.dat")</param>
        /// <returns>The list of PackedItemData items</returns>
        /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
        /// <exception cref="ArgumentException">Thrown if path is empty or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if neither the main file nor the .bak backup exists.</exception>
        /// <exception cref="InvalidDataException">Thrown if both files exist but contain invalid data.</exception>
        /// <exception cref="EndOfStreamException">Thrown if a file is truncated or empty.</exception>
        public static List<PackedItemData> LoadFromFile(string path)
        {
            _ = path is not null                 ? 0 : throw new ArgumentNullException(nameof(path));
            _ = !string.IsNullOrWhiteSpace(path) ? 0 : throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));

            string bakPath = path + BackupExtension;

            if (File.Exists(path))
            {
                try { return LoadFromPath(path); }
                catch (IOException) { /* Main file exists but deserialization failed; fall through to try backup */ }
                catch (InvalidDataException) { /* Main file exists but deserialization failed; fall through to try backup */ }
            }

            if (File.Exists(bakPath))
                return LoadFromPath(bakPath);

            throw new FileNotFoundException($"No items file found at '{path}' or backup at '{bakPath}'.", path);
        }

        /// <summary>Opens the file at the given path, deserializes, and returns the items.</summary>
        private static List<PackedItemData> LoadFromPath(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                (List<PackedItemData> items, _) = PackedItemSerialization.Deserialize(stream);  // Discards version 
                return items;
            }
        }
    }
}
