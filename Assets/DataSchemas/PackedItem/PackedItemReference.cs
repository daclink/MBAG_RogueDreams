using System;
using UnityEngine;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Lightweight reference to an item in the packed item tables.
    /// Use (type, biomeFlags, key) to look up sprite, name, and stats.
    /// </summary>
    [Serializable]
    public struct PackedItemReference : IEquatable<PackedItemReference>
    {
        public ItemType Type;
        public BiomeFlags BiomeFlags;
        [Range(0, 31)]
        public byte Key;

        public PackedItemReference(ItemType type, BiomeFlags biomeFlags, byte key)
        {
            Type = type;
            BiomeFlags = biomeFlags;
            Key = key;
        }

        public bool Equals(PackedItemReference other) =>
            Type == other.Type && BiomeFlags == other.BiomeFlags && Key == other.Key;

        public override bool Equals(object obj) => obj is PackedItemReference other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Type, BiomeFlags, Key);

        public static bool operator ==(PackedItemReference a, PackedItemReference b) => a.Equals(b);
        public static bool operator !=(PackedItemReference a, PackedItemReference b) => !a.Equals(b);
    }
}
