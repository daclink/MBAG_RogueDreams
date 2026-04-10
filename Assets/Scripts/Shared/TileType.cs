namespace MBAG
{
    /// <summary>
    /// Shared tile type used by WFC dungeon generation and DualGrid tilemap.
    /// Values aligned for compatibility between both systems.
    /// </summary>
    public enum TileType
    {
        Empty = -1,
        Grass = 0,
        Dirt = 1,
        Path = 2,
        Water = 3,
        Wall = 4
    }
}
