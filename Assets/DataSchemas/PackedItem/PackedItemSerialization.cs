using System;
using System.Collections.Generic;
using System.IO;
using Serialization;

namespace DataSchemas.PackedItem
{
    /// <summary>One item entry in sparse serialization. Type and partition are byte indices; Key is 0-24.</summary>
    public readonly struct PackedItemEntry
    {
        public byte Type { get; }
        public byte Partition { get; }
        public byte Key { get; }
        public ulong Block0 { get; }
        public ulong Block1 { get; }

        public PackedItemEntry(byte type, byte partition, byte key, ulong block0, ulong block1)
        {
            Type = type;
            Partition = partition;
            Key = key;
            Block0 = block0;
            Block1 = block1;
        }

        public PackedItemData ToData() => new PackedItemData(Block0, Block1);
    }

    /// <summary>Result of loading packed items from file. Sparse entries keyed by (type, partition, key).</summary>
    public sealed class PackedItemLoadResult
    {
        public List<PackedItemEntry> Entries { get; }
        public byte Version { get; }

        public PackedItemLoadResult(List<PackedItemEntry> entries, byte version)
        {
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            Version = version;
        }

        /// <summary>Legacy: flat list of items (order preserved).</summary>
        public List<PackedItemData> Items
        {
            get
            {
                var list = new List<PackedItemData>(Entries.Count);
                foreach (PackedItemEntry e in Entries)
                    list.Add(e.ToData());
                return list;
            }
        }
    }

    /// <summary>
    /// Serializes packed items in sparse format: [version][count][per entry: type, partition, key, block0, block1].
    /// Only used slots. Format V2. Little-endian.
    /// </summary>
    public static class PackedItemSerialization
    {
        private const int MaxEntryCount = PackedItemTableCore.TotalSlots;

        private const byte DataKindPackedItems = 0;
        private const byte ProtocolV1 = 1;
        private const byte CompatibilityStrict = 0;
        private const byte FormatV1Flat = 0;
        private const byte FormatV2Sparse = 1;
        private const byte MinSupportedFormat = 0;
        private const byte MaxSupportedFormat = 1;

        private static byte CurrentVersion => Versioning.Pack(DataKindPackedItems, ProtocolV1, CompatibilityStrict, FormatV2Sparse);

        private static void ValidateVersion(byte version)
        {
            (byte dataKind, byte protocol, byte compatibility, byte format) = Versioning.Unpack(version);

            static void Require(bool ok, string message)
            {
                if (!ok) throw new InvalidDataException(message);
            }

            Require(dataKind == DataKindPackedItems, $"Unsupported data kind: {dataKind}. Expected {DataKindPackedItems} (PackedItems).");
            Require(protocol == ProtocolV1, $"Unsupported protocol: {protocol}. Expected {ProtocolV1}.");
            Require(compatibility == CompatibilityStrict, $"Unsupported compatibility mode: {compatibility}. Expected {CompatibilityStrict}.");
            Require(format >= MinSupportedFormat && format <= MaxSupportedFormat, $"Unsupported format version: {format}. Supported range: {MinSupportedFormat}-{MaxSupportedFormat}.");
        }

        /// <summary>Serializes sparse entries to the stream.</summary>
        public static void Serialize(Stream stream, IReadOnlyList<PackedItemEntry> entries)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));
            _ = entries.Count <= MaxEntryCount ? 0 : throw new ArgumentOutOfRangeException(nameof(entries), entries.Count, $"Entry count must not exceed {MaxEntryCount}.");

            stream.WriteByte(CurrentVersion);
            StandardizeLittleEndian.WriteInt32(stream, entries.Count);

            foreach (PackedItemEntry e in entries)
            {
                stream.WriteByte(e.Type);
                stream.WriteByte(e.Partition);
                stream.WriteByte(e.Key);
                StandardizeLittleEndian.WriteUInt64(stream, e.Block0);
                StandardizeLittleEndian.WriteUInt64(stream, e.Block1);
            }
        }

        /// <summary>Deserializes from the stream. Supports format V1 (flat) and V2 (sparse).</summary>
        public static PackedItemLoadResult Deserialize(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> buffer = stackalloc byte[1];
            _ = stream.Read(buffer) == 1 ? 0 : throw new EndOfStreamException("Expected version byte.");
            byte version = buffer[0];
            ValidateVersion(version);

            (_, _, _, byte format) = Versioning.Unpack(version);

            if (format == FormatV1Flat)
                return DeserializeFlat(stream, version);
            return DeserializeSparse(stream, version);
        }

        private static PackedItemLoadResult DeserializeFlat(Stream stream, byte version)
        {
            int count = StandardizeLittleEndian.ReadInt32(stream);
            _ = count >= 0 && count <= MaxEntryCount ? 0 : throw new InvalidDataException($"Invalid item count: {count}.");

            List<PackedItemEntry> list = new List<PackedItemEntry>(count);
            for (int i = 0; i < count; i++)
            {
                ulong block0 = StandardizeLittleEndian.ReadUInt64(stream);
                ulong block1 = StandardizeLittleEndian.ReadUInt64(stream);
                var data = new PackedItemData(block0, block1);
                int p = PackedItemTableCore.GetPartitionIndex(data.BiomeFlags);
                list.Add(new PackedItemEntry((byte)data.ItemType, (byte)p, data.SpriteKey, block0, block1));
            }
            return new PackedItemLoadResult(list, version);
        }

        private static PackedItemLoadResult DeserializeSparse(Stream stream, byte version)
        {
            int count = StandardizeLittleEndian.ReadInt32(stream);
            _ = count >= 0 && count <= MaxEntryCount ? 0 : throw new InvalidDataException($"Invalid entry count: {count}.");

            List<PackedItemEntry> list = new List<PackedItemEntry>(count);
            for (int i = 0; i < count; i++)
            {
                int t = stream.ReadByte();
                int p = stream.ReadByte();
                int k = stream.ReadByte();
                if (t < 0 || p < 0 || k < 0)
                    throw new EndOfStreamException($"Truncated entry at index {i}.");

                ulong block0 = StandardizeLittleEndian.ReadUInt64(stream);
                ulong block1 = StandardizeLittleEndian.ReadUInt64(stream);
                list.Add(new PackedItemEntry((byte)t, (byte)p, (byte)k, block0, block1));
            }
            return new PackedItemLoadResult(list, version);
        }
    }
}
