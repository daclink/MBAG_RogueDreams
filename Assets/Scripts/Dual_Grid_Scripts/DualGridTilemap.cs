using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TileType;

///currently just copied over from Jesshammer while I figure this out



public class DualGridTilemap : MonoBehaviour {
    /// <summary>
    /// This is going to remain the same as the original for now
    /// </summary>
    protected static readonly Vector3Int[] NEIGHBOURS = {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0)
    };
    
    /// <summary>
    /// Part of the original, making a slightly different version to account for Biomes
    /// </summary>
    // original
    protected static Dictionary<Tuple<TileType, TileType, TileType, TileType>, Tile> neighbourTupleToTile;
    // idea for biome inclusion
    // private static Dictionary<Tuple<TileType, TileType, TileType, TileType, Biome>, Tile> neighbourTupleToTile;
    
    
    /// <summary>
    /// input tile map, and gonna need the dimensions of it if there isn't fixed dimensions
    /// </summary>
    // Provide references to each tilemap in the inspector
    public Tilemap inputTilemap; //input tile map
    public BoundsInt bounds;
    public int inputHeight;
    public int inputWidth;
    public Vector3Int origin;


    /// <summary>
    /// part of the original, using a slightly different system so I am adjusting it
    /// </summary>
    // public Tilemap displayTilemap; 

    public Tilemap floorTilemap;
    public Tilemap wallTilemap;

    // Add the placeholder and display tilemaps that the logic uses
    // public Tilemap placeholderTilemap;
    // public Tilemap displayTilemap;
    
    
    // Provide the dirt and grass placeholder tiles in the inspector
    [SerializeField] public Tile grassPlaceholderTile;
    [SerializeField] public Tile dirtPlaceholderTile;
    /// <summary>
    /// Extra placeholders
    /// </summary>
    public Tile emptyPlaceholderTile;
    public Tile pathPlaceholderTile;
    public Tile waterPlaceholderTile;
    public Tile wallPlaceholderTile;
    

    /// <summary>
    /// Grab the tiles
    /// </summary>
    public Tile[] tiles;
    
    /// <summary>
    /// need to update the tiles list ;-;
    /// mainly need to see how the tiles are collected and what entry in the tile list will be what
    /// likely going to need one massive tile palette for this
    /// just not fully sure how this will work yet
    /// </summary>
    void Start() {
        inputTilemap.CompressBounds();
        bounds = inputTilemap.cellBounds;
        inputHeight = bounds.size.y;
        inputWidth = bounds.size.x;
        origin = bounds.position;
        
        // This dictionary stores the "rules", each 4-neighbour configuration corresponds to a tile
        // |_1_|_2_|
        // |_3_|_4_|
        // current enum values are 0, 1, 2 , 3 for empty, grass, dirt, path respectively
        // should iterate through that list starting with the bottom right
        // plan accordingly when setting up the tile list which has to have enough tiles
        // create mapping for every 4-tuple of TileType (4^4 = 256 combinations)
        neighbourTupleToTile = new Dictionary<Tuple<TileType, TileType, TileType, TileType>, Tile>();

        // used ai for this one
        int enumCount = Enum.GetValues(typeof(TileType)).Length;
        int required = enumCount * enumCount * enumCount * enumCount;

        if (tiles == null || tiles.Length < required) {
            Debug.LogError($"tiles array must contain at least {required} entries (has {tiles?.Length ?? 0}).");
        } else {
            for (int tl = 0; tl < enumCount; tl++) {
                for (int tr = 0; tr < enumCount; tr++) {
                    for (int bl = 0; bl < enumCount; bl++) {
                        for (int br = 0; br < enumCount; br++) {
                            int idx = ((tl * enumCount + tr) * enumCount + bl) * enumCount + br;
                            var key = Tuple.Create((TileType)tl, (TileType)tr, (TileType)bl, (TileType)br);
                            neighbourTupleToTile[key] = tiles[idx];
                        }
                    }
                }
            }
        }
        
        RefreshFloorTilemap();
    }
    
    /// <summary>
    /// This seems to be used in the example for updating a specific cell tile, might be useful later
    /// for now im just going to comment it out as it isn't needed for the initial render
    /// </summary>
    // public void SetCell(Vector3Int coords, Tile tile) {
    //     placeholderTilemap.SetTile(coords, tile);
    //     SetFloorTile(coords);
    // }
    
    // gets placeholder tile type at specific coordinates
    private TileType GetInputTileTypeAt(Vector3Int coords) {
        if (inputTilemap.GetTile(coords) == grassPlaceholderTile)
            return Grass;
        else if (inputTilemap.GetTile(coords) == dirtPlaceholderTile)
            return Dirt;
        else if (inputTilemap.GetTile(coords) == emptyPlaceholderTile)
            return Empty;
        else if (inputTilemap.GetTile(coords) == pathPlaceholderTile)
            return Path;
        else if (inputTilemap.GetTile(coords) == waterPlaceholderTile)
            return Water;
        else if (inputTilemap.GetTile(coords) == wallPlaceholderTile)
            return Wall;
        else
            return Empty; // default to empty if no tile is found
    }
    
    // uses given coordinates of the floor tile to create a tuple of the associated neighbor tile types
    protected Tile CalculateFloorTile(Vector3Int coords) {
        // 4 neighbours
        TileType topRight = GetInputTileTypeAt(coords + NEIGHBOURS[3]);
        TileType topLeft = GetInputTileTypeAt(coords + NEIGHBOURS[2]);
        TileType botRight = GetInputTileTypeAt(coords + NEIGHBOURS[1]);
        TileType botLeft = GetInputTileTypeAt(coords + NEIGHBOURS[0]);

        var neighbourTuple = Tuple.Create(topLeft, topRight, botLeft, botRight);

        return neighbourTupleToTile[neighbourTuple];
    }
    
    /// <summary>
    /// This one takes a position on the placeholder/input tilemap and updates the floor map based on
    /// the listed neighbors - going to need multiple of these to implement the "layer" idea
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    protected void SetFloorTile(Vector3Int pos) {
        for (int i = 0; i < NEIGHBOURS.Length; i++) {
            Vector3Int newPos = pos + NEIGHBOURS[i];
            floorTilemap.SetTile(newPos, CalculateFloorTile(newPos));
        }
    }

    // The tiles on the floor tilemap will recalculate themselves based on the input tilemap
    /// <summary>
    /// My understanding is that this scans through the existing tiles within a specific area
    /// and calls the update funciton separately
    ///
    ///
    /// This has been updated to scan through a number of coordinates equivalent to the "input" tilemap
    /// </summary>
    /// <returns></returns>
    public void RefreshFloorTilemap() {
        for (int x = bounds.xMin-1; x < bounds.xMax; x++) {
            for (int y = bounds.yMin-1; y < bounds.yMax; y++) {
                SetFloorTile(new Vector3Int(x, y, 0));
            }
        }
    }
}

public enum TileType {
    Empty,
    Dirt,
    Grass,
    Path,
    Water,
    Wall
}