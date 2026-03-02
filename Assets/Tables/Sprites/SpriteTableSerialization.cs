using System;
using System.Collections.Generic;
using System.IO;
using Serialization;

namespace Tables
{
    /// <summary>Result of loading sprite table from file.</summary>
    public sealed class SpriteTableLoadResult
    {
        public int Width { get; }
        public int Height { get; }
        public List<SpriteTableEntry> Entries { get; }

        public SpriteTableLoadResult(int width, int height, List<SpriteTableEntry> entries)
        {
            Width = width;
            Height = height;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }
    }

    /// <summary>
    /// One spritesheet entry in the serialized table. Type and partition are byte indices; Key is 0-31; FrameCount is 1-16 (icon + animation); Pixels is RGBA, row-major, all frames concatenated.
    /// </summary>
    public readonly struct SpriteTableEntry
    {
        public byte Type { get; }
        public byte Partition { get; }
        public byte Key { get; }
        /// <summary>Number of frames (1 = single sprite).</summary>
        public int FrameCount { get; }
        public byte[] Pixels { get; }

        public SpriteTableEntry(byte type, byte partition, byte key, int frameCount, byte[] pixels)
        {
            Type = type;
            Partition = partition;
            Key = key;
            FrameCount = frameCount <= 0 ? 1 : frameCount;
            Pixels = pixels ?? Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Serializes and deserializes sprite table data. Format V2 (spritesheets):
    /// [1 byte version][4 bytes width][4 bytes height][4 bytes count][per entry: 1 byte type, 1 byte partition, 1 byte key, 1 byte frameCount, frameCount*BytesPerFrame pixels].
    /// Sparse: only used slots. 64×64 RGBA = 16,384 bytes per frame.
    /// </summary>
    public static class SpriteTableSerialization
    {
        public const int SpriteWidth = 64;
        public const int SpriteHeight = 64;
        public const int BytesPerFrame = SpriteWidth * SpriteHeight * 4;

        public const int MaxFramesPerItem = 16;

        private const int MaxEntryCount = 100_000;
        private const int MaxFrameCount = MaxFramesPerItem;
        private const byte DataKindSpriteTable = 1;
        private const byte ProtocolV1 = 1;
        private const byte CompatibilityStrict = 0;
        private const byte FormatV1 = 0;
        private const byte FormatV2Spritesheet = 1;

        private static byte CurrentVersion => Versioning.Pack(DataKindSpriteTable, ProtocolV1, CompatibilityStrict, FormatV2Spritesheet);

        private static void ValidateVersion(byte version)
        {
            (byte dataKind, byte protocol, byte compatibility, byte format) = Versioning.Unpack(version);

            static void Require(bool ok, string message)
            {
                if (!ok) throw new InvalidDataException(message);
            }

            Require(dataKind == DataKindSpriteTable, $"Unsupported data kind: {dataKind}. Expected {DataKindSpriteTable} (SpriteTable).");
            Require(protocol == ProtocolV1, $"Unsupported protocol: {protocol}. Expected {ProtocolV1}.");
            Require(compatibility == CompatibilityStrict, $"Unsupported compatibility mode: {compatibility}.");
            Require(format == FormatV1 || format == FormatV2Spritesheet, $"Unsupported format: {format}. Expected {FormatV1} or {FormatV2Spritesheet}.");
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

        /// <summary>Serializes entries to the stream (Format V2 spritesheet).</summary>
        public static void Serialize(Stream stream, IReadOnlyList<SpriteTableEntry> entries)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));
            if (entries.Count > MaxEntryCount)
                throw new ArgumentOutOfRangeException(nameof(entries), entries.Count, $"Entry count must not exceed {MaxEntryCount}.");

            stream.WriteByte(CurrentVersion);
            StandardizeLittleEndian.WriteInt32(stream, SpriteWidth);
            StandardizeLittleEndian.WriteInt32(stream, SpriteHeight);
            StandardizeLittleEndian.WriteInt32(stream, entries.Count);

            foreach (SpriteTableEntry e in entries)
            {
                int frameCount = e.FrameCount <= 0 ? 1 : Math.Min(e.FrameCount, MaxFrameCount);
                int bytesExpected = frameCount * BytesPerFrame;
                if (e.Pixels is null || e.Pixels.Length != bytesExpected)
                    throw new InvalidOperationException($"Entry must have exactly {bytesExpected} pixel bytes ({frameCount} frames).");

                stream.WriteByte(e.Type);
                stream.WriteByte(e.Partition);
                stream.WriteByte(e.Key);
                stream.WriteByte((byte)frameCount);
                stream.Write(e.Pixels, 0, bytesExpected);
            }
        }

        /// <summary>Deserializes from the stream. Supports Format V1 (single sprite) and V2 (spritesheet).</summary>
        public static SpriteTableLoadResult Deserialize(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> buf = stackalloc byte[1];
            if (stream.Read(buf) != 1)
                throw new EndOfStreamException("Expected version byte.");
            byte version = buf[0];
            ValidateVersion(version);

            (_, _, _, byte format) = Versioning.Unpack(version);
            int width = StandardizeLittleEndian.ReadInt32(stream);
            int height = StandardizeLittleEndian.ReadInt32(stream);
            int count = StandardizeLittleEndian.ReadInt32(stream);

            if (width != SpriteWidth || height != SpriteHeight)
                throw new InvalidDataException($"Expected {SpriteWidth}x{SpriteHeight}, got {width}x{height}.");
            if (count < 0 || count > MaxEntryCount)
                throw new InvalidDataException($"Invalid entry count: {count}. Must be 0-{MaxEntryCount}.");

            List<SpriteTableEntry> list = new List<SpriteTableEntry>(count);

            for (int i = 0; i < count; i++)
            {
                int t = stream.ReadByte();
                int p = stream.ReadByte();
                int k = stream.ReadByte();
                if (t < 0 || p < 0 || k < 0)
                    throw new EndOfStreamException($"Truncated entry at index {i}.");

                int frameCount = format == FormatV2Spritesheet ? stream.ReadByte() : 1;
                if (frameCount <= 0) frameCount = 1;
                if (frameCount > MaxFrameCount)
                    throw new InvalidDataException($"Invalid frame count {frameCount} at entry {i}.");

                int bytesToRead = frameCount * BytesPerFrame;
                byte[] pixels = new byte[bytesToRead];
                ReadExactly(stream, pixels, bytesToRead);

                list.Add(new SpriteTableEntry((byte)t, (byte)p, (byte)k, frameCount, pixels));
            }

            return new SpriteTableLoadResult(width, height, list);
        }
    }
}
