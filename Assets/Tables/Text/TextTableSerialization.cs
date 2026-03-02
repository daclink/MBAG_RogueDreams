using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Serialization;

namespace Tables
{
    /// <summary>Result of loading text table from file.</summary>
    public sealed class TextTableLoadResult
    {
        public List<TextTableEntry> Entries { get; }

        public TextTableLoadResult(List<TextTableEntry> entries)
        {
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }
    }

    /// <summary>One text entry in the serialized table. Type and partition are byte indices; Key is 0–31.</summary>
    public readonly struct TextTableEntry
    {
        public byte Type { get; }
        public byte Partition { get; }
        public byte Key { get; }
        public string Text { get; }

        public TextTableEntry(byte type, byte partition, byte key, string text)
        {
            Type = type;
            Partition = partition;
            Key = key;
            Text = text ?? string.Empty;
        }
    }

    /// <summary>
    /// Serializes and deserializes text table data. Format:
    /// [1 byte version][4 bytes count][per entry: 1 byte type, 1 byte partition, 1 byte key, 4 bytes utf8 length, utf8 bytes].
    /// Sparse: only used slots. UTF-8 encoded strings.
    /// </summary>
    public static class TextTableSerialization
    {
        private const int MaxEntryCount = 100_000;
        private const int MaxTextByteLength = 4096;
        private const byte DataKindTextTable = 2;
        private const byte ProtocolV1 = 1;
        private const byte CompatibilityStrict = 0;
        private const byte FormatV1 = 0;

        private static byte CurrentVersion => Versioning.Pack(DataKindTextTable, ProtocolV1, CompatibilityStrict, FormatV1);
        private static readonly Encoding Utf8 = Encoding.UTF8;

        private static void ValidateVersion(byte version)
        {
            (byte dataKind, byte protocol, byte compatibility, byte format) = Versioning.Unpack(version);

            static void Require(bool ok, string message)
            {
                if (!ok) throw new InvalidDataException(message);
            }

            Require(dataKind == DataKindTextTable, $"Unsupported data kind: {dataKind}. Expected {DataKindTextTable} (TextTable).");
            Require(protocol == ProtocolV1, $"Unsupported protocol: {protocol}. Expected {ProtocolV1}.");
            Require(compatibility == CompatibilityStrict, $"Unsupported compatibility mode: {compatibility}.");
            Require(format == FormatV1, $"Unsupported format: {format}. Expected {FormatV1}.");
        }

        private static void ReadExactly(Stream stream, byte[] buffer, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int n = stream.Read(buffer, totalRead, count - totalRead);
                if (n == 0)
                    throw new EndOfStreamException($"Expected {count} bytes, got {totalRead}.");
                totalRead += n;
            }
        }

        /// <summary>Serializes entries to the stream.</summary>
        public static void Serialize(Stream stream, IReadOnlyList<TextTableEntry> entries)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            if (entries is null) throw new ArgumentNullException(nameof(entries));
            if (entries.Count > MaxEntryCount)
                throw new ArgumentOutOfRangeException(nameof(entries), entries.Count, $"Entry count must not exceed {MaxEntryCount}.");

            stream.WriteByte(CurrentVersion);
            StandardizeLittleEndian.WriteInt32(stream, entries.Count);

            foreach (TextTableEntry e in entries)
            {
                string s = e.Text ?? string.Empty;
                byte[] bytes = Utf8.GetBytes(s);
                if (bytes.Length > MaxTextByteLength)
                    throw new InvalidOperationException($"Text exceeds max length of {MaxTextByteLength} bytes.");

                stream.WriteByte(e.Type);
                stream.WriteByte(e.Partition);
                stream.WriteByte(e.Key);
                StandardizeLittleEndian.WriteInt32(stream, bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>Deserializes from the stream.</summary>
        public static TextTableLoadResult Deserialize(Stream stream)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            Span<byte> buf = stackalloc byte[1];
            if (stream.Read(buf) != 1)
                throw new EndOfStreamException("Expected version byte.");
            byte version = buf[0];
            ValidateVersion(version);

            int count = StandardizeLittleEndian.ReadInt32(stream);
            if (count < 0 || count > MaxEntryCount)
                throw new InvalidDataException($"Invalid entry count: {count}. Must be 0–{MaxEntryCount}.");

            List<TextTableEntry> list = new List<TextTableEntry>(count);

            for (int i = 0; i < count; i++)
            {
                int t = stream.ReadByte();
                int p = stream.ReadByte();
                int k = stream.ReadByte();
                if (t < 0 || p < 0 || k < 0)
                    throw new EndOfStreamException($"Truncated entry at index {i}.");

                int len = StandardizeLittleEndian.ReadInt32(stream);
                if (len < 0 || len > MaxTextByteLength)
                    throw new InvalidDataException($"Invalid text length {len} at entry {i}.");

                string text = string.Empty;
                if (len > 0)
                {
                    byte[] bytes = new byte[len];
                    ReadExactly(stream, bytes, len);
                    text = Utf8.GetString(bytes);
                }

                list.Add(new TextTableEntry((byte)t, (byte)p, (byte)k, text));
            }

            return new TextTableLoadResult(list);
        }
    }
}
