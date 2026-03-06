using UnityEngine;

/// <summary>
/// Registry of BiomeTileSets. One per biome. Index = biome ID (0-15, 4 used initially).
/// </summary>
[CreateAssetMenu(menuName = "Tables/Biome Tile Registry", fileName = "BiomeTileRegistry")]
public class BiomeTileRegistry : ScriptableObject
{
    [SerializeField] private BiomeTileSet[] biomeSets = new BiomeTileSet[4];

    /// <summary>Returns the BiomeTileSet for the given biome index, or null if out of range.</summary>
    public BiomeTileSet GetBiomeSet(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomeSets.Length) return null;
        return biomeSets[biomeIndex];
    }

    /// <summary>Number of biome slots (4 now, expandable to 16).</summary>
    public int BiomeCount => biomeSets?.Length ?? 0;
}
