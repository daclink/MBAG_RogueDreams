using System;

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

        // Bitmask free list per (type, partition). Bit k=1 means key k is free (0..31).
        private readonly uint[,] _freeMask;

        public PackedItemTableCore()
        {
            _freeMask = new uint[TypeCount, BiomePartitionCount];
            ResetAllFree();
        }

        /// <summary>Sets all keys as free for all (type, partition) buckets.</summary>
        public void ResetAllFree()
        {
            for (int t = 0; t < TypeCount; t++)
            {
                for (int p = 0; p < BiomePartitionCount; p++)
                {
                    _freeMask[t, p] = 0xFFFF_FFFFu; // all 32 keys free
                }
            }
        }

        /// <summary>Partition = biome byte value in range 0–31. Throws if out of range.</summary>
        public static int GetPartitionIndex(BiomeFlags biomeFlags)
        {
            int p = (byte)biomeFlags;
            if (p >= BiomePartitionCount)
                throw new ArgumentOutOfRangeException(nameof(biomeFlags), biomeFlags, $"BiomeFlags value {p} is out of range. Expected 0-{BiomePartitionCount - 1}.");
            return p;
        }

        /// <summary>Computes flat array index from (type, partition, key).</summary>
        public static int GetFlatIndex(ItemType type, int partition, byte key)
        {
            return (((byte)type) * KeysPerType) + (partition * SlotsPerBiome) + key;
        }

        /// <summary>Allocates a slot for (type, biomeFlags). Returns key 0–31, or -1 if full.</summary>
        public int Add(ItemType type, BiomeFlags biomeFlags)
        {
            int typeIndex = (byte)type;
            int partition = GetPartitionIndex(biomeFlags);

            uint mask = _freeMask[typeIndex, partition];
            if (mask != 0u)
            {
                // Unity / .NET Standard 2.0: BitOperations.TrailingZeroCount is often unavailable or internal.
                int k = IndexOfLowestSetBit(mask); // 0..31
                _freeMask[typeIndex, partition] = mask & ~(1u << k);
                return k;
            }

            return -1;
        }

        /// <summary>Lowest key k in 0..31 with (mask & (1 &lt;&lt; k)) != 0. Caller must ensure mask != 0.</summary>
        private static int IndexOfLowestSetBit(uint mask)
        {
            int k = 0;
            while ((mask & (1u << k)) == 0)
                k++;
            return k;
        }

        /// <summary>Marks the slot as freed. Caller must clear storage.</summary>
        public void Remove(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            int partition = GetPartitionIndex(biomeFlags);
            _freeMask[(byte)type, partition] |= (1u << key);
        }

        /// <summary>
        /// Marks the slot as in use (not free). Used when loading items into slots without calling <see cref="Add"/>.
        /// Bit cleared = key allocated; see <see cref="Remove"/> which sets the bit again.
        /// </summary>
        public void MarkUsed(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            int partition = GetPartitionIndex(biomeFlags);
            _freeMask[(byte)type, partition] &= ~(1u << key);
        }

        // /// <summary>
        // /// Legacy helper: rebuilds free masks from an isSlotEmpty predicate by scanning all slots.
        // /// Kept commented-out for reference; current design maintains masks incrementally.
        // /// </summary>
        // public void RebuildFreeLists(Func<int, bool> isSlotEmpty)
        // {
        //     _ = isSlotEmpty ?? throw new ArgumentNullException(nameof(isSlotEmpty));

        //     // For every type
        //     for (int t = 0; t < TypeCount; t++)

        //     {   // For every partition in the type
        //         for (int p = 0; p < BiomePartitionCount; p++)

        //         {   // Calculate start index
        //             int baseIdx = t * KeysPerType + p * SlotsPerBiome;
        //             uint mask = 0u;

        //             // Check if each slot in the partition is empty
        //             for (int k = 0; k < SlotsPerBiome; k++)
        //             {
        //                 // Build a bitmask for our slots
        //                 if (isSlotEmpty(baseIdx + k))
        //                     mask |= (1u << k);
        //             }
        //             // Set resulting mask
        //             _freeMask[t, p] = mask;
        //         }
        //     }
        // }
    }
}
