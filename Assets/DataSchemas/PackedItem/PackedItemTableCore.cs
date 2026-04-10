using System;
using System.Collections.Generic;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Platform-agnostic core for packed item table. Defines slot layout (256×32×32).
    /// SpriteTable2D uses this layout. Owns free lists for canonical allocation.
    /// Partition = biome byte value (0–255). Keys 0–31 per partition.
    /// </summary>
    public sealed class PackedItemTableCore
    {
        public const int TypeCount = 256;
        public const int BiomePartitionCount = 32;
        public const int SlotsPerBiome = 32;
        public const int KeysPerType = BiomePartitionCount * SlotsPerBiome;
        public const int TotalSlots = TypeCount * KeysPerType;

        private readonly Stack<byte>[,] _freeLists;

        public PackedItemTableCore()
        {
            _freeLists = new Stack<byte>[TypeCount, BiomePartitionCount];
            for (int t = 0; t < TypeCount; t++)
                for (int p = 0; p < BiomePartitionCount; p++)
                    _freeLists[t, p] = new Stack<byte>();
        }

        /// <summary>Partition = biome byte value clamped to 0–31. Extend BiomeFlags and BiomePartitionCount as needed.</summary>
        public static int GetPartitionIndex(BiomeFlags biomeFlags)
        {
            int p = (byte)biomeFlags;
            return p < BiomePartitionCount ? p : BiomePartitionCount - 1;
        }

        /// <summary>Computes flat array index from (type, partition, key).</summary>
        public static int GetFlatIndex(ItemType type, int partition, byte key)
        {
            return ((byte)type) * KeysPerType + partition * SlotsPerBiome + key;
        }

        /// <summary>Rebuilds free lists from slot emptiness.</summary>
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

        /// <summary>Allocates a slot for (type, biomeFlags). Returns key 0–31, or -1 if full.</summary>
        public int Add(ItemType type, BiomeFlags biomeFlags, Func<int, bool> isSlotEmpty)
        {
            if (isSlotEmpty is null)
                throw new ArgumentNullException(nameof(isSlotEmpty));

            int typeIndex = (byte)type;
            int partition = GetPartitionIndex(biomeFlags);

            if (_freeLists[typeIndex, partition].Count > 0)
                return _freeLists[typeIndex, partition].Pop();

            int baseIdx = typeIndex * KeysPerType + partition * SlotsPerBiome;
            for (int k = 0; k < SlotsPerBiome; k++)
            {
                if (isSlotEmpty(baseIdx + k))
                    return k;
            }
            return -1;
        }

        /// <summary>Marks the slot as freed. Caller must clear storage.</summary>
        public void Remove(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            int partition = GetPartitionIndex(biomeFlags);
            _freeLists[(byte)type, partition].Push(key);
        }
    }
}
