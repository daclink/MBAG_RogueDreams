using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 55 canonical tiles per biome (rotation + reflection). Matches DualGridCanonicalKeys / Generate55CanonicalTiles.
/// </summary>
[CreateAssetMenu(menuName = "Tables/Biome Tile Set", fileName = "BiomeTileSet")]
public class BiomeTileSet : ScriptableObject
{
    public const int TileCount = 55;

    [SerializeField] private Tile[] tiles = new Tile[TileCount];

    /// <summary>Returns the tile for the given canonical index (0-54).</summary>
    public Tile GetTile(int canonicalIndex)
    {
        if (canonicalIndex < 0 || canonicalIndex >= TileCount) return null;
        return tiles[canonicalIndex];
    }

    /// <summary>Sets the tile at canonical index. Used by slicing tool.</summary>
    public void SetTile(int canonicalIndex, Tile tile)
    {
        if (canonicalIndex >= 0 && canonicalIndex < TileCount)
            tiles[canonicalIndex] = tile;
    }

    /// <summary>Returns the tiles array for editor/inspector access.</summary>
    public Tile[] Tiles => tiles;
}
