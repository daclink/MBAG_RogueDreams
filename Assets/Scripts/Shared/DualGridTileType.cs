namespace MBAG
{
    /// <summary>
    /// Four tile types for DualGrid mask-based lookup.
    /// Maps from WFC TileType: Ground←Dirt/Path/Empty, Wall←Wall, Water←Water, Grass←Grass.
    /// Priority for primary selection: Wall > Water > Grass > Ground.
    /// </summary>
    public enum DualGridTileType
    {
        Ground = 0,
        Wall = 1,
        Water = 2,
        Grass = 3
    }

    public static class DualGridTileTypeMapping
    {
        /// <summary>Maps WFC TileType to DualGrid 4-type. Empty → Ground.</summary>
        public static DualGridTileType FromTileType(TileType wfc)
        {
            return wfc switch
            {
                TileType.Empty => DualGridTileType.Ground,
                TileType.Dirt => DualGridTileType.Ground,
                TileType.Path => DualGridTileType.Ground,
                TileType.Wall => DualGridTileType.Wall,
                TileType.Water => DualGridTileType.Water,
                TileType.Grass => DualGridTileType.Grass,
                _ => DualGridTileType.Ground
            };
        }

        /// <summary>Priority for primary: higher = wins ties. Wall > Water > Grass > Ground.</summary>
        public static int Priority(DualGridTileType t)
        {
            return t switch
            {
                DualGridTileType.Wall => 3,
                DualGridTileType.Water => 2,
                DualGridTileType.Grass => 1,
                DualGridTileType.Ground => 0,
                _ => 0
            };
        }
    }
}
