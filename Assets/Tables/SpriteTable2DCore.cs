using System;
using System.Collections.Generic;
using DataSchemas.PackedItem;

namespace Tables
{
    /// <summary>
    /// Platform-agnostic core for 2D partitioned lookup table structure.
    /// Manages indexing, free lists, and slot allocation. No engine types.
    /// </summary>
    public sealed class SpriteTable2DCore
    {
        public const int TypeCount = 256;
        public const int BiomePartitionCount = 10;
        public const int SlotsPerBiome = 25;
        public const int KeysPerType = BiomePartitionCount * SlotsPerBiome;
        public const int TotalSlots = TypeCount * KeysPerType;

        private readonly Stack<byte>[,] _freeLists;

        public SpriteTable2DCore()
        {
            _freeLists = new Stack<byte>[TypeCount, BiomePartitionCount];
            for (int t = 0; t < TypeCount; t++)
                for (int p = 0; p < BiomePartitionCount; p++)
                    _freeLists[t, p] = new Stack<byte>();
        }

        /// <summary>Maps BiomeFlags to partition index 0-9. Single-bit → that bit index; 2 bits → 8; 3+ bits → 9.</summary>
        public static int GetPartitionIndex(BiomeFlags biomeFlags)
        {
            byte b = (byte)biomeFlags;
            if (b == 0) return 0;

            int count = 0;
            int lowest = -1;
            for (int i = 0; i < 8; i++)
            {
                if ((b & (1 << i)) != 0)
                {
                    count++;
                    if (lowest < 0) lowest = i;
                }
            }
            if (count == 1) return lowest;
            if (count == 2) return 8;
            return 9;
        }

        /// <summary>Computes flat array index from (type, partition, key).</summary>
        public static int GetFlatIndex(ItemType type, int partition, byte key)
        {
            return ((byte)type) * KeysPerType + partition * SlotsPerBiome + key;
        }

        /// <summary>Rebuilds free lists from slot emptiness. <paramref name="isSlotEmpty"/> is called with flat index.</summary>
        public void RebuildFreeLists(Func<int, bool> isSlotEmpty)
        {
            if (isSlotEmpty is null)
                throw new ArgumentNullException(nameof(isSlotEmpty));

            for (int t = 0; t < TypeCount; t++)
            {
                for (int p = 0; p < BiomePartitionCount; p++)
                {
                    _freeLists[t, p].Clear();
                    int baseIdx = t * KeysPerType + p * SlotsPerBiome;
                    for (int k = 0; k < SlotsPerBiome; k++)
                    {
                        if (isSlotEmpty(baseIdx + k))
                            _freeLists[t, p].Push((byte)k);
                    }
                }
            }
        }

        /// <summary>
        /// Allocates a slot for (type, biomeFlags). Reuses freed slot or scans via <paramref name="isSlotEmpty"/>.
        /// Returns key 0-24, or -1 if partition full.
        /// Caller must store value at GetFlatIndex(type, GetPartitionIndex(biomeFlags), key).
        /// </summary>
        public int Add(ItemType type, BiomeFlags biomeFlags, Func<int, bool> isSlotEmpty)
        {
            if (isSlotEmpty is null)
                throw new ArgumentNullException(nameof(isSlotEmpty));

            int typeIndex = (byte)type;
            int partition = GetPartitionIndex(biomeFlags);

            if (_freeLists[typeIndex, partition].Count > 0)
            {
                return _freeLists[typeIndex, partition].Pop();
            }

            int baseIdx = typeIndex * KeysPerType + partition * SlotsPerBiome;
            for (int k = 0; k < SlotsPerBiome; k++)
            {
                int idx = baseIdx + k;
                if (isSlotEmpty(idx))
                    return k;
            }

            return -1;
        }

        /// <summary>Marks the slot as freed for reuse. Caller must clear storage at that index.</summary>
        public void Remove(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            int partition = GetPartitionIndex(biomeFlags);
            _freeLists[(byte)type, partition].Push(key);
        }
    }
}
