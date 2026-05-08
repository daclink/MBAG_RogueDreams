using System;
using System.Collections.Generic;
using UnityEngine;
using DataSchemas.PackedItem;

namespace Tables
{
    /// <summary>
    /// Storage for spritesheets at (type, partition, key). Layout from PackedItemTableCore.
    /// Each slot holds Sprite[] (frames). Frame 0 = icon. PackedItemTable is canonical; use SetAt/ClearAt.
    /// </summary>
    [CreateAssetMenu(menuName = "Tables/Sprite Table 2D", fileName = "SpriteTable2D")]
    public class SpriteTable2D : ScriptableObject
    {
        public const int TypeCount = PackedItemTableCore.TypeCount;
        public const int BiomePartitionCount = PackedItemTableCore.BiomePartitionCount;
        public const int SlotsPerBiome = PackedItemTableCore.SlotsPerBiome;
        public const int KeysPerType = PackedItemTableCore.KeysPerType;
        public const int TotalSlots = PackedItemTableCore.TotalSlots;

        public static int GetPartitionIndex(BiomeFlags biomeFlags) => PackedItemTableCore.GetPartitionIndex(biomeFlags);

        [System.NonSerialized] private Sprite[][] _spritesheets;

        // Git-friendly path: keep the spritesheet source texture (project asset) and rebuild runtime Sprite[] frames on load.
        // This avoids baking raw pixels into sprites.dat.
        [SerializeField] private Texture2D[] _sourceTextures;
        [Min(1)]
        [SerializeField] private int _frameWidth = 64;
        [Min(1)]
        [SerializeField] private int _frameHeight = 64;

        /// <summary>Number of non-null slots. Used by custom inspector.</summary>
        public int GetUsedSlotCount()
        {
            if (_spritesheets == null) return 0;
            int n = 0;
            for (int i = 0; i < _spritesheets.Length; i++)
                if (_spritesheets[i] != null && _spritesheets[i].Length > 0) n++;
            return n;
        }

        void OnEnable()
        {
            EnsureCapacity();
            EnsureSourceArrayCapacity();
            // Don't rebuild if we have no imported source textures; legacy loads populate runtime sprites from sprites.dat instead.
            if (HasAnySourceTexture())
                RebuildRuntimeSpritesFromSourceTextures();
        }

        void EnsureCapacity()
        {
            if (_spritesheets == null || _spritesheets.Length < TotalSlots)
            {
                var prev = _spritesheets;
                _spritesheets = new Sprite[TotalSlots][];
                if (prev != null)
                    Array.Copy(prev, _spritesheets, Math.Min(prev.Length, TotalSlots));
            }
        }

        void EnsureSourceArrayCapacity()
        {
            if (_sourceTextures == null || _sourceTextures.Length < TotalSlots)
            {
                var prev = _sourceTextures;
                _sourceTextures = new Texture2D[TotalSlots];
                if (prev != null)
                    Array.Copy(prev, _sourceTextures, Math.Min(prev.Length, TotalSlots));
            }
        }

        bool HasAnySourceTexture()
        {
            if (_sourceTextures == null) return false;
            for (int i = 0; i < _sourceTextures.Length; i++)
                if (_sourceTextures[i] != null) return true;
            return false;
        }

        /// <summary>Returns frame 0 (icon) at [type][partition from biomeFlags][key].</summary>
        public Sprite Get(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            return GetFrame(type, biomeFlags, key, 0);
        }

        /// <summary>Returns the sprite at the given frame index. Frame 0 = icon.</summary>
        public Sprite GetFrame(ItemType type, BiomeFlags biomeFlags, byte key, int frameIndex)
        {
            EnsureCapacity();
            if (_spritesheets == null) return null;
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int i = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (i < 0 || i >= _spritesheets.Length) return null;
            Sprite[] frames = _spritesheets[i];
            if (frames == null || frames.Length == 0) return null;
            if (frameIndex < 0 || frameIndex >= frames.Length) return frames[0];
            return frames[frameIndex] ?? frames[0];
        }

        /// <summary>Returns all frames at the slot, or null if empty.</summary>
        public Sprite[] GetSpritesheet(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            EnsureCapacity();
            if (_spritesheets == null) return null;
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int i = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (i < 0 || i >= _spritesheets.Length) return null;
            return _spritesheets[i];
        }

        /// <summary>Convenience: returns frame 0 for the given item data.</summary>
        public Sprite GetSprite(PackedItemData data)
        {
            return Get(data.ItemType, data.BiomeFlags, data.SpriteKey);
        }

        /// <summary>Sets spritesheet at (type, partition, key). PackedItemTable allocates slots.</summary>
        public void SetAt(ItemType type, BiomeFlags biomeFlags, byte key, Sprite[] frames)
        {
            EnsureCapacity();
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int i = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (i >= 0 && i < _spritesheets.Length)
            {
                _spritesheets[i] = frames != null && frames.Length > 0 ? frames : null;
                TryAssignSourceFromFrames(i, frames);
            }
        }

        /// <summary>Clears spritesheet at (type, partition, key).</summary>
        public void ClearAt(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            EnsureCapacity();
            if (_spritesheets == null) return;
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int i = PackedItemTableCore.GetFlatIndex(type, partition, key);
            if (i >= 0 && i < _spritesheets.Length)
            {
                _spritesheets[i] = null;
                EnsureSourceArrayCapacity();
                if (i < _sourceTextures.Length)
                    _sourceTextures[i] = null;
            }
        }

        /// <summary>True if the slot has a non-empty spritesheet.</summary>
        public bool HasEntry(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            return Get(type, biomeFlags, key) != null;
        }

        /// <summary>Returns the first key in the given partition whose frame 0 matches the sprite, or -1.</summary>
        public int GetKeyOf(ItemType type, BiomeFlags biomeFlags, Sprite sprite)
        {
            if (sprite == null) return -1;
            EnsureCapacity();
            if (_spritesheets == null) return -1;
            int partition = PackedItemTableCore.GetPartitionIndex(biomeFlags);
            int baseIdx = ((byte)type) * KeysPerType + partition * SlotsPerBiome;
            for (int k = 0; k < SlotsPerBiome; k++)
            {
                Sprite[] frames = _spritesheets[baseIdx + k];
                if (frames != null && frames.Length > 0 && frames[0] == sprite)
                    return k;
            }
            return -1;
        }

        /// <summary>Saves used slots to file. Uses SpriteTableStorage with atomic write.</summary>
        public void SaveFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            EnsureCapacity();
            if (_spritesheets == null) return;

            List<SpriteTableEntry> entries = new List<SpriteTableEntry>();
            int w = SpriteTableSerialization.SpriteWidth;
            int h = SpriteTableSerialization.SpriteHeight;
            for (int t = 0; t < TypeCount; t++)
            {
                for (int p = 0; p < BiomePartitionCount; p++)
                {
                    int baseIdx = t * KeysPerType + p * SlotsPerBiome;
                    for (byte k = 0; k < SlotsPerBiome; k++)
                    {
                        Sprite[] frames = _spritesheets[baseIdx + k];
                        if (frames == null || frames.Length == 0) continue;
                        int frameCount = Math.Min(frames.Length, SpriteTableSerialization.MaxFramesPerItem);
                        Sprite[] toSave = frames;
                        if (frameCount < frames.Length)
                        {
                            toSave = new Sprite[frameCount];
                            Array.Copy(frames, toSave, frameCount);
                        }
                        byte[] pixels = SpritePixelConversion.SpriteToPixels(toSave, w, h);
                        entries.Add(new SpriteTableEntry((byte)t, (byte)p, k, frameCount, pixels));
                    }
                }
            }
            SpriteTableStorage.SaveToFile(path, entries);
        }

        /// <summary>Loads from file, populates table. Clears existing entries first.</summary>
        public void LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            SpriteTableLoadResult result = SpriteTableStorage.LoadFromFile(path);
            EnsureCapacity();
            for (int i = 0; i < _spritesheets.Length; i++)
                _spritesheets[i] = null;

            int w = SpriteTableSerialization.SpriteWidth;
            int h = SpriteTableSerialization.SpriteHeight;
            foreach (SpriteTableEntry entry in result.Entries)
            {
                Sprite[] frames = SpritePixelConversion.PixelsToSprites(entry.Pixels, w, h, entry.FrameCount);
                int idx = PackedItemTableCore.GetFlatIndex((ItemType)entry.Type, entry.Partition, entry.Key);
                if (idx >= 0 && idx < _spritesheets.Length)
                    _spritesheets[idx] = frames;
            }
        }

        /// <summary>
        /// Re-slices <see cref="_sourceTextures"/> into the runtime <see cref="_spritesheets"/> cache.
        /// Safe to call at runtime; requires readable textures and dimensions divisible by the frame size.
        /// </summary>
        public void RebuildRuntimeSpritesFromSourceTextures()
        {
            EnsureCapacity();
            EnsureSourceArrayCapacity();

            for (int i = 0; i < TotalSlots; i++)
            {
                Texture2D tex = i < _sourceTextures.Length ? _sourceTextures[i] : null;
                if (tex == null)
                {
                    _spritesheets[i] = null;
                    continue;
                }

                _spritesheets[i] = SliceTextureToSprites(tex, _frameWidth, _frameHeight);
            }
        }

        /// <summary>Copies the Texture reference from the first frame (if any) into the serialized source table.</summary>
        public void SyncSourceTexturesFromRuntimeSprites()
        {
            EnsureCapacity();
            EnsureSourceArrayCapacity();
            for (int i = 0; i < TotalSlots; i++)
                TryAssignSourceFromFrames(i, _spritesheets[i]);
        }

        void TryAssignSourceFromFrames(int slotIndex, Sprite[] frames)
        {
            if (slotIndex < 0 || slotIndex >= TotalSlots) return;
            EnsureSourceArrayCapacity();
            if (frames == null || frames.Length == 0 || frames[0] == null)
            {
                _sourceTextures[slotIndex] = null;
                return;
            }

            _sourceTextures[slotIndex] = frames[0].texture;
        }

        static Sprite[] SliceTextureToSprites(Texture2D texture, int frameWidth, int frameHeight)
        {
            if (texture == null) return null;
            if (frameWidth <= 0 || frameHeight <= 0) return null;
            if (texture.width % frameWidth != 0 || texture.height % frameHeight != 0)
            {
                Debug.LogWarning(
                    $"SpriteTable2D: texture '{texture.name}' size {texture.width}×{texture.height} is not divisible by frame {frameWidth}×{frameHeight}.");
                return null;
            }

            if (!texture.isReadable)
            {
                Debug.LogWarning(
                    $"SpriteTable2D: texture '{texture.name}' is not readable. Enable Read/Write in the Texture importer.");
                return null;
            }

            int cols = texture.width / frameWidth;
            int rows = texture.height / frameHeight;
            int totalFrames = cols * rows;
            int frameCount = Math.Min(totalFrames, SpriteTableSerialization.MaxFramesPerItem);
            if (totalFrames > frameCount)
            {
                Debug.LogWarning(
                    $"SpriteTable2D: texture '{texture.name}' has {totalFrames} frames; using first {frameCount} (capped).");
            }

            var sprites = new Sprite[frameCount];
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            int idx = 0;
            for (int row = 0; row < rows && idx < frameCount; row++)
            {
                for (int col = 0; col < cols && idx < frameCount; col++)
                {
                    var rect = new Rect(col * frameWidth, texture.height - (row + 1) * frameHeight, frameWidth, frameHeight);
                    sprites[idx++] = Sprite.Create(texture, rect, pivot);
                }
            }

            return sprites;
        }
    }
}
