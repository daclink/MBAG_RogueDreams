using System;
using System.Collections.Generic;
using System.IO;
using Serialization;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Serializes and deserializes lists of PackedItemData in a binary format:
    /// [1 byte version][4 bytes count][16 bytes per item (Block0, Block1)].
    /// Little-endian. Cross-platform via StandardizeLittleEndian.
    /// </summary>
    public static class PackedItemSerialization
    {
        private const int MaxItemCount = 1_000_000;

        private const byte DataKindPackedItems = 0;
        private const byte ProtocolV1 = 1;
        private const byte CompatibilityStrict = 0;
        private const byte FormatV1 = 0;
        private const byte MinSupportedFormat = 0;
        private const byte MaxSupportedFormat = 1;

        private static byte CurrentVersion => Versioning.Pack(DataKindPackedItems, ProtocolV1, CompatibilityStrict, FormatV1);

        /// <summary>
        /// Unpack the version bytes. Throw if it is invalid or does not meet the expected standard.
        /// </summary>
        /// <param name="version">Written version to be tested against current standard.</param>
        /// <exception cref="InvalidDataException">Thrown if version values are outside the expected range.</exception>
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

        /// <summary>
        /// Validate stream and items as non-null and item count as within limits.
        /// Then write version and number of expected objects
        /// Finally, for every item in the list, write Block0 and then Block1 to the stream.
        /// </summary>
        /// <param name="stream">Destination data stream</param>
        /// <param name="items">List PackedItemData structures.</param>
        public static void Serialize(Stream stream, IReadOnlyList<PackedItemData> items)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            _ = items.Count <= MaxItemCount ? 0 : throw new ArgumentOutOfRangeException(nameof(items), items.Count, $"Item count must not exceed {MaxItemCount}.");

            stream.WriteByte(CurrentVersion);
            StandardizeLittleEndian.WriteInt32(stream, items.Count);

            foreach (PackedItemData item in items)
            {
                StandardizeLittleEndian.WriteUInt64(stream, item.Block0);
                StandardizeLittleEndian.WriteUInt64(stream, item.Block1);
            }
        }

        /// <summary>
        /// Validate stream, then allocate a 1-byte span to read and then validate the version.
        /// Get the count of items to read before creating a list of size 'count'.
        /// Then read data as blocks, then use them to create new PackedItemData structures.
        /// Finally, add the new PackedItemData structures to a list.
        /// </summary>
        /// <param name="stream">Source data stream</param>
        /// <returns>List of PackedItemData items and the version byte</returns>
        public static (List<PackedItemData> items, byte version) Deserialize(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> buffer = stackalloc byte[1];
            _ = stream.Read(buffer) == 1 ? 0 : throw new EndOfStreamException("Expected version byte.");
            byte version = buffer[0];
            ValidateVersion(version);

            int count = StandardizeLittleEndian.ReadInt32(stream);
            _ = count >= 0 && count <= MaxItemCount ? 0 : throw new InvalidDataException($"Invalid item count: {count}. Must be 0-{MaxItemCount}.");

            List<PackedItemData> list = new List<PackedItemData>(count);
            for (int i = 0; i < count; i++)
            {
                ulong block0 = StandardizeLittleEndian.ReadUInt64(stream);
                ulong block1 = StandardizeLittleEndian.ReadUInt64(stream);
                list.Add(new PackedItemData(block0, block1));
            }

            return (list, version);
        }
    }
}
