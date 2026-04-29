using System;
using System.Collections.Generic;
using System.IO;
using Serialization;

namespace DataSchemas.PackedItem
{
    /// <summary>Result of loading packed items from file. Items only; slot (type, partition, key) is derived from each item's Block0.</summary>
    public sealed class PackedItemLoadResult
    {
        public PackedItemData[] Items { get; }
        public byte Version { get; }

        public PackedItemLoadResult(PackedItemData[] items, byte version)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Version = version;
        }
    }

    /// <summary>
    /// Serializes packed items as a list of (block0, block1) only. Slot (type, partition, key) is derived from block0 when loading.
    /// Format: [version][count][per item: block0, block1]. Little-endian.
    /// </summary>
    public static class PackedItemSerialization
    {
        private const int MaxItemCount = PackedItemTableCore.TotalSlots;

        private const byte DataKindPackedItems = 0;
        private const byte ProtocolV1 = 1;
        private const byte CompatibilityStrict = 0;

        private enum PackedItemFormat : byte
        {
            BlocksOnly = 0,
        }

        private static readonly PackedItemFormat CurrentFormat = PackedItemFormat.BlocksOnly;

        private static byte CurrentVersion =>
            Versioning.Pack(DataKindPackedItems, ProtocolV1, CompatibilityStrict, (byte)CurrentFormat);

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
            Require(format == (byte)PackedItemFormat.BlocksOnly, $"Unsupported format version: {format}. Expected BlocksOnly.");
        }

        /// <summary>Serializes items to the stream (block0, block1 only).</summary>
        public static void Serialize(Stream stream, IReadOnlyList<PackedItemData> items)
        {
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            _ = items ?? throw new ArgumentNullException(nameof(items));
            _ = items.Count <= MaxItemCount ? 0 : throw new ArgumentOutOfRangeException(nameof(items), items.Count, $"Item count must not exceed {MaxItemCount}.");

            stream.WriteByte(CurrentVersion);
            StandardizeLittleEndian.WriteInt32(stream, items.Count);

            foreach (PackedItemData item in items)
            {
                StandardizeLittleEndian.WriteUInt64(stream, item.Block0);
                StandardizeLittleEndian.WriteUInt64(stream, item.Block1);
            }
        }

        /// <summary>Deserializes from the stream. Format: [version][count][per item: block0, block1].</summary>
        public static PackedItemLoadResult Deserialize(Stream stream)
        {
            _ = stream ?? throw new ArgumentNullException(nameof(stream));

            Span<byte> buffer = stackalloc byte[1];
            _ = stream.Read(buffer) == 1 ? 0 : throw new EndOfStreamException("Expected version byte.");
            byte version = buffer[0];
            ValidateVersion(version);

            int count = StandardizeLittleEndian.ReadInt32(stream);
            _ = count >= 0 && count <= MaxItemCount ? 0 : throw new InvalidDataException($"Invalid item count: {count}.");

            PackedItemData[] items = new PackedItemData[count];
            for (int i = 0; i < count; i++)
            {
                ulong block0 = StandardizeLittleEndian.ReadUInt64(stream);
                ulong block1 = StandardizeLittleEndian.ReadUInt64(stream);
                items[i] = new PackedItemData(block0, block1);
            }
            return new PackedItemLoadResult(items, version);
        }
    }
}
