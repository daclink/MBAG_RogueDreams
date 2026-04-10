using System.Collections.Generic;
using MBAG;

/// <summary>
/// Maps 8-bit corner keys to canonical index and transform (rotation + reflection).
/// Uses D4 symmetry: 4 rotations + reflection. 55 canonical tiles.
/// Order matches Aseprite Generate55CanonicalTiles.lua (by distinct types, then key).
/// </summary>
public static class DualGridCanonicalKeys
{
    public const int CanonicalCount = 55;

    private static readonly int[] CanonicalKeys;
    private static readonly Dictionary<int, (int index, int rotation, bool reflected)> KeyToCanonical;

    static DualGridCanonicalKeys()
    {
        var list = new List<int>();
        for (int key = 0; key < 256; key++)
        {
            if (IsCanonical(key))
                list.Add(key);
        }

        list.Sort((a, b) =>
        {
            int da = DistinctCornerTypes(a);
            int db = DistinctCornerTypes(b);
            if (da != db) return da.CompareTo(db);
            return a.CompareTo(b);
        });

        CanonicalKeys = list.ToArray();

        if (CanonicalKeys.Length != CanonicalCount)
        {
            UnityEngine.Debug.LogError($"DualGridCanonicalKeys: Expected {CanonicalCount} canonical keys, got {CanonicalKeys.Length}. " +
                "Check IsCanonical/sort vs Aseprite Generate55CanonicalTiles.lua.");
        }

        KeyToCanonical = new Dictionary<int, (int, int, bool)>();
        for (int key = 0; key < 256; key++)
        {
            var (idx, rot, refl) = GetCanonicalIndexRotationAndReflection(key);
            KeyToCanonical[key] = (idx, rot, refl);
        }
    }

    /// <summary>Rotate 90° CW: new_bl=br, new_br=tr, new_tr=tl, new_tl=bl.</summary>
    public static int Rotate90(int key)
    {
        int bl = key & 3;
        int br = (key >> 2) & 3;
        int tl = (key >> 4) & 3;
        int tr = (key >> 6) & 3;
        return br | (tr << 2) | (bl << 4) | (tl << 6);
    }

    /// <summary>Reflect horizontally (flip L-R): bl↔br, tl↔tr.</summary>
    public static int Reflect(int key)
    {
        int bl = key & 3;
        int br = (key >> 2) & 3;
        int tl = (key >> 4) & 3;
        int tr = (key >> 6) & 3;
        return br | (bl << 2) | (tr << 4) | (tl << 6);
    }

    /// <summary>Canonical = key is min of its 8-fold orbit (4 rotations + reflected rotations).</summary>
    public static bool IsCanonical(int key)
    {
        int min = key;
        int k = key;
        for (int i = 0; i < 4; i++)
        {
            if (k < min) min = k;
            k = Rotate90(k);
        }
        k = Reflect(key);
        for (int i = 0; i < 4; i++)
        {
            if (k < min) min = k;
            k = Rotate90(k);
        }
        return key == min;
    }

    private static int DistinctCornerTypes(int key)
    {
        int bl = key & 3;
        int br = (key >> 2) & 3;
        int tl = (key >> 4) & 3;
        int tr = (key >> 6) & 3;
        var seen = new HashSet<int> { bl, br, tl, tr };
        return seen.Count;
    }

    /// <summary>Build 8-bit key from four corner types (Ground=0, Wall=1, Water=2, Grass=3).</summary>
    public static int BuildKey(DualGridTileType bl, DualGridTileType br, DualGridTileType tl, DualGridTileType tr)
    {
        return (int)bl | ((int)br << 2) | ((int)tl << 4) | ((int)tr << 6);
    }

    /// <summary>Returns (canonicalIndex 0-54, rotation 0/1/2/3, reflected).</summary>
    public static (int canonicalIndex, int rotation, bool reflected) GetCanonicalIndexRotationAndReflection(int key)
    {
        if (KeyToCanonical.TryGetValue(key, out var v))
            return v;

        int min = key;
        int k = key;
        for (int i = 0; i < 4; i++)
        {
            if (k < min) min = k;
            k = Rotate90(k);
        }
        k = Reflect(key);
        for (int i = 0; i < 4; i++)
        {
            if (k < min) min = k;
            k = Rotate90(k);
        }

        int idx = System.Array.IndexOf(CanonicalKeys, min);
        if (idx < 0) return (0, 0, false);

        k = key;
        for (int r = 0; r < 4; r++)
        {
            if (k == min) return (idx, r, false);
            k = Rotate90(k);
        }
        k = Reflect(key);
        for (int r = 0; r < 4; r++)
        {
            if (k == min) return (idx, r, true);
            k = Rotate90(k);
        }

        return (idx, 0, false);
    }

    /// <summary>Returns (canonicalIndex, rotation). Use GetCanonicalIndexRotationAndReflection for reflection.</summary>
    public static (int canonicalIndex, int rotation) GetCanonicalIndexAndRotation(int key)
    {
        var (idx, rot, _) = GetCanonicalIndexRotationAndReflection(key);
        return (idx, rot);
    }

    /// <summary>Canonical index 0-54 for the given key.</summary>
    public static int GetCanonicalIndex(int key)
    {
        return GetCanonicalIndexRotationAndReflection(key).canonicalIndex;
    }

    /// <summary>Rotation 0/1/2/3 (0°/90°/180°/270° CW) to display key from canonical tile.</summary>
    public static int GetRotation(int key)
    {
        return GetCanonicalIndexRotationAndReflection(key).rotation;
    }

    /// <summary>Ordered canonical keys (matches Aseprite output).</summary>
    public static IReadOnlyList<int> GetCanonicalKeyOrder() => CanonicalKeys;
}
