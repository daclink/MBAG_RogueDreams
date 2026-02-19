using UnityEngine;
using DataSchemas.PackedItem;

namespace Tables
{
    /// <summary>
    /// Unity asset wrapper for 2D partitioned sprite lookup. Core logic lives in SpriteTable2DCore.
    /// [type][partition, slot]. 10 biome partitions, 25 slots each. GetSprite(data) for common lookup.
    /// </summary>
    [CreateAssetMenu(menuName = "Tables/Sprite Table 2D", fileName = "SpriteTable2D")]
    public class SpriteTable2D : ScriptableObject
    {
        public const int TypeCount = SpriteTable2DCore.TypeCount;
        public const int BiomePartitionCount = SpriteTable2DCore.BiomePartitionCount;
        public const int SlotsPerBiome = SpriteTable2DCore.SlotsPerBiome;
        public const int KeysPerType = SpriteTable2DCore.KeysPerType;
        public const int TotalSlots = SpriteTable2DCore.TotalSlots;

        /// <summary>Maps BiomeFlags to partition index 0-9. Delegates to core.</summary>
        public static int GetPartitionIndex(BiomeFlags biomeFlags) => SpriteTable2DCore.GetPartitionIndex(biomeFlags);

        [SerializeField] private Sprite[] _sprites = new Sprite[SpriteTable2DCore.TotalSlots];

        private SpriteTable2DCore _core;

        private SpriteTable2DCore Core => _core ??= new SpriteTable2DCore();

        private void OnEnable()
        {
            RebuildFreeLists();
        }

        /// <summary>Rebuilds free lists from null slots. Call after loading or batch updates.</summary>
        public void RebuildFreeLists()
        {
            if (_sprites == null) return;
            Core.RebuildFreeLists(i => i >= 0 && i < _sprites.Length && _sprites[i] == null);
        }

        /// <summary>Returns the sprite at [type][partition from biomeFlags][key].</summary>
        public Sprite Get(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            if (_sprites == null) return null;
            int partition = SpriteTable2DCore.GetPartitionIndex(biomeFlags);
            int i = SpriteTable2DCore.GetFlatIndex(type, partition, key);
            if (i < 0 || i >= _sprites.Length) return null;
            return _sprites[i];
        }

        /// <summary>Convenience: returns the sprite for the given item data.</summary>
        public Sprite GetSprite(PackedItemData data)
        {
            return Get(data.ItemType, data.BiomeFlags, data.SpriteKey);
        }

        /// <summary>Adds a sprite for (type, biomeFlags partition). Returns key 0-24, or -1 if partition full.</summary>
        public int Add(ItemType type, BiomeFlags biomeFlags, Sprite sprite)
        {
            if (sprite == null) return -1;
            if (_sprites == null) _sprites = new Sprite[SpriteTable2DCore.TotalSlots];

            int key = Core.Add(type, biomeFlags, i => _sprites[i] == null);
            if (key < 0) return -1;

            int partition = SpriteTable2DCore.GetPartitionIndex(biomeFlags);
            int idx = SpriteTable2DCore.GetFlatIndex(type, partition, (byte)key);
            _sprites[idx] = sprite;
            return key;
        }

        /// <summary>Removes the entry. Slot can be reused by Add.</summary>
        public void Remove(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            if (_sprites == null) return;
            int partition = SpriteTable2DCore.GetPartitionIndex(biomeFlags);
            int i = SpriteTable2DCore.GetFlatIndex(type, partition, key);
            if (i >= 0 && i < _sprites.Length)
            {
                _sprites[i] = null;
                Core.Remove(type, biomeFlags, key);
            }
        }

        /// <summary>True if the slot has a non-null entry.</summary>
        public bool HasEntry(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            return Get(type, biomeFlags, key) != null;
        }

        /// <summary>Returns the first key in the given partition that matches the sprite, or -1.</summary>
        public int GetKeyOf(ItemType type, BiomeFlags biomeFlags, Sprite sprite)
        {
            if (sprite == null || _sprites == null) return -1;
            int partition = SpriteTable2DCore.GetPartitionIndex(biomeFlags);
            int baseIdx = ((byte)type) * SpriteTable2DCore.KeysPerType + partition * SpriteTable2DCore.SlotsPerBiome;
            for (int k = 0; k < SpriteTable2DCore.SlotsPerBiome; k++)
            {
                if (_sprites[baseIdx + k] == sprite)
                    return k;
            }
            return -1;
        }
    }
}
