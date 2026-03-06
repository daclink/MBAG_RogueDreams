#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor verification of DualGrid canonical key count (55 with rotation + reflection).
/// Menu: Tools > Dual Grid > Verify Canonical Count
/// </summary>
public static class VerifyCanonicalCount
{
    [MenuItem("Tools/Dual Grid/Verify Canonical Count")]
    public static void Run()
    {
        var runtimeKeys = DualGridCanonicalKeys.GetCanonicalKeyOrder();
        int runtimeCount = runtimeKeys.Count;

        var localKeys = ComputeCanonicalKeysLocally();
        int localCount = localKeys.Count;

        bool match = runtimeCount == localCount;
        for (int i = 0; i < System.Math.Min(runtimeCount, localCount) && match; i++)
            match = runtimeKeys[i] == localKeys[i];

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DualGrid Canonical Key Verification (55 tiles, D4) ===");
        sb.AppendLine();
        sb.AppendLine($"Runtime count: {runtimeCount}");
        sb.AppendLine($"Local count:   {localCount}");
        sb.AppendLine($"Match: {(match ? "YES" : "NO")}");
        sb.AppendLine();
        sb.AppendLine("First 10 canonical keys:");
        for (int i = 0; i < System.Math.Min(10, runtimeCount); i++)
        {
            int k = runtimeKeys[i];
            int bl = k & 3, br = (k >> 2) & 3, tl = (k >> 4) & 3, tr = (k >> 6) & 3;
            sb.AppendLine($"  [{i}] key={k} corners=({bl},{br},{tl},{tr})");
        }

        string msg = sb.ToString();
        Debug.Log(msg);
        EditorUtility.DisplayDialog("Canonical Verification", msg, "OK");
    }

    private static int Rot90(int k)
    {
        int bl = k & 3, br = (k >> 2) & 3, tl = (k >> 4) & 3, tr = (k >> 6) & 3;
        return br | (tr << 2) | (bl << 4) | (tl << 6);
    }

    private static int Reflect(int k)
    {
        int bl = k & 3, br = (k >> 2) & 3, tl = (k >> 4) & 3, tr = (k >> 6) & 3;
        return br | (bl << 2) | (tr << 4) | (tl << 6);
    }

    private static int DistinctCornerTypes(int k)
    {
        var s = new HashSet<int> { k & 3, (k >> 2) & 3, (k >> 4) & 3, (k >> 6) & 3 };
        return s.Count;
    }

    private static bool IsCanonicalLocal(int k)
    {
        int min = k;
        int x = k;
        for (int i = 0; i < 4; i++) { if (x < min) min = x; x = Rot90(x); }
        x = Reflect(k);
        for (int i = 0; i < 4; i++) { if (x < min) min = x; x = Rot90(x); }
        return k == min;
    }

    private static List<int> ComputeCanonicalKeysLocally()
    {
        var list = new List<int>();
        for (int k = 0; k < 256; k++)
            if (IsCanonicalLocal(k))
                list.Add(k);
        list.Sort((a, b) =>
        {
            int da = DistinctCornerTypes(a), db = DistinctCornerTypes(b);
            if (da != db) return da.CompareTo(db);
            return a.CompareTo(b);
        });
        return list;
    }
}
#endif
