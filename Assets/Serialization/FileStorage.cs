using System;
using System.IO;

namespace Serialization
{
    /// <summary>
    /// Universal file storage with atomic writes and backup fallback.
    /// Writes to .tmp first, then replaces main file (keeping .bak). Load tries main, falls back to .bak.
    /// </summary>
    public static class FileStorage
    {
        private const string TempExtension = ".tmp";
        private const string BackupExtension = ".bak";

        /// <summary>
        /// Saves data by calling the provided serialize delegate. Uses .tmp for atomicity.
        /// </summary>
        public static void SaveToFile<T>(string path, T data, Action<Stream, T> serialize)
        {
            _ = path is not null ? 0 : throw new ArgumentNullException(nameof(path));
            _ = !string.IsNullOrWhiteSpace(path) ? 0 : throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));
            _ = serialize is not null ? 0 : throw new ArgumentNullException(nameof(serialize));

            string tmpPath = path + TempExtension;
            try
            {
                using (FileStream stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serialize(stream, data);
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
        /// Loads data by calling the provided deserialize delegate. Tries main file first, then .bak fallback.
        /// </summary>
        public static T LoadFromFile<T>(string path, Func<Stream, T> deserialize)
        {
            _ = path is not null ? 0 : throw new ArgumentNullException(nameof(path));
            _ = !string.IsNullOrWhiteSpace(path) ? 0 : throw new ArgumentException("Path cannot be empty or whitespace.", nameof(path));
            _ = deserialize is not null ? 0 : throw new ArgumentNullException(nameof(deserialize));

            string bakPath = path + BackupExtension;

            if (File.Exists(path))
            {
                try { return LoadFromPath(path, deserialize); }
                catch (IOException) { /* Fall through to backup */ }
                catch (InvalidDataException) { /* Fall through to backup */ }
            }

            if (File.Exists(bakPath))
                return LoadFromPath(bakPath, deserialize);

            throw new FileNotFoundException($"No file found at '{path}' or backup at '{bakPath}'.", path);
        }

        private static T LoadFromPath<T>(string path, Func<Stream, T> deserialize)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return deserialize(stream);
            }
        }
    }
}
