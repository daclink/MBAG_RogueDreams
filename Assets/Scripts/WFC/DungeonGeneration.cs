using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace WFC
{
    
    // All tyle types
    public enum TileType
    {
        Empty = -1,
        Grass = 0,
        Dirt = 1,
        Path = 2,
        Water = 3,
        Wall = 4
    }
    
    //All biome types
    public enum BiomeType
    {
        Biome1 = 0,
        Biome2 = 1,
        Biome3 = 2,
        Biome4 = 3
    }

    // All room types
    public enum RoomType
    {
        Empty = -1,
        Normal = 0,
        Start = 1,
        End = 2,
        Item = 3
    }

    // Directions for adjacency checking
    public enum Direction
    {
        North,
        South,
        East,
        West
    }

    public class DungeonGeneration : MonoBehaviour
    {
        [Header("Tilemap References")]
        [SerializeField] private Tilemap baseLayerTilemap;
        [SerializeField] private Grid gridComponent;

        [Header("Tile Assets")] 
        [SerializeField] private TileBase emptyTile;
        [SerializeField] private TileBase grassTile;
        [SerializeField] private TileBase dirtTile;
        [SerializeField] private TileBase pathTile;
        [SerializeField] private TileBase waterTile;
        [SerializeField] private TileBase wallTile;

        [Header("Minimap")]
        [SerializeField] private MinimapRenderer minimapRenderer;

        [Header("Generation Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private int randomSeed = 0;
        [SerializeField] private int mapWidth;
        [SerializeField] private int mapHeight;
        [SerializeField] private int minRooms;
        [SerializeField] private int maxRooms;
        
        [Header("WFC Settings")]
        [SerializeField] private int pathWidth = 2;
        [Range(1, 8)]
        [Tooltip("Width of paths through rooms (in tiles)")]
        private int inspectorPathWidth = 2;

        private RoomLayoutGenerator roomLayoutGen;
        private WFCTilemap wfcGen;
        private TileType[,] finalTilemap;
        private int[,] roomLayout;

        void Start()
        {
            if (generateOnStart)
            {
                GenerateCompleteDungeon();
            }
        }

        // Sync inspector value to internal pathWidth
        void OnValidate()
        {
            pathWidth = inspectorPathWidth;
        }

        [ContextMenu("Generate New Dungeon")]
        public void GenerateCompleteDungeon()
        {
            if (randomSeed != 0)
            {
                UnityEngine.Random.InitState(randomSeed);
            }

            Debug.Log("=== Starting Dungeon Generation ===");

            // Stage 1: Generate room layout
            Debug.Log("Stage 1: Generating room layout...");
            roomLayoutGen = new RoomLayoutGenerator();
            roomLayout = roomLayoutGen.GenerateRoomGrid(mapWidth, mapHeight, minRooms, maxRooms);

            var roomPositions = roomLayoutGen.GetRoomPositions();
            var startRoom = roomLayoutGen.GetStartRoomPosition();
            var endRoom = roomLayoutGen.GetEndRoomPosition();
            var itemRoom = roomLayoutGen.GetItemRoomPosition();

            Debug.Log($"Room layout complete: {roomPositions.Count} rooms");
            
            // Render minimap AFTER room layout generation
            if (minimapRenderer != null)
            {
                minimapRenderer.RenderMinimap(roomLayout);
                Debug.Log("Minimap rendered!");
            }

            // Stage 2: Generate tilemap using WFC
            Debug.Log("Stage 2: Generating tilemap with WFC...");
            wfcGen = new WFCTilemap(roomLayout, roomPositions, roomLayoutGen, pathWidth);
            // wfcTilemapGen.SetPathWidth(pathWidth); // Set path width
            finalTilemap = wfcGen.Generate();

            Vector2Int tilemapSize = wfcGen.GetTilemapSize();
            Debug.Log($"Tilemap complete: {tilemapSize.x}x{tilemapSize.y} tiles");

            // Stage 3: Build Unity Tilemap
            Debug.Log("Stage 3: Building Unity Tilemap...");
            BuildUnityTilemap(finalTilemap);

            Debug.Log("=== Dungeon Generation Complete ===");
        }

        private void BuildUnityTilemap(TileType[,] tileData)
        {
            if (baseLayerTilemap == null)
            {
                Debug.LogError("Base Layer Tilemap not assigned!");
                return;
            }

            baseLayerTilemap.ClearAllTiles();

            int width = tileData.GetLength(0);
            int height = tileData.GetLength(1);

            int tilesPlaced = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileType tileType = tileData[x, y];
                    Vector3Int tilePosition = new Vector3Int(x, y, 0);

                    TileBase tileAsset = GetTileAsset(tileType);

                    if (tileAsset != null)
                    {
                        baseLayerTilemap.SetTile(tilePosition, tileAsset);
                        tilesPlaced++;
                    }
                }
            }

            Debug.Log($"Unity Tilemap built: {tilesPlaced} tiles placed");
            CenterCameraOnTilemap(width, height);
        }

        private TileBase GetTileAsset(TileType type)
        {
            switch (type)
            {
                case TileType.Grass: return grassTile;
                case TileType.Dirt: return dirtTile;
                case TileType.Path: return pathTile;
                case TileType.Water: return waterTile;
                case TileType.Wall: return wallTile;
                case TileType.Empty: return emptyTile;
                default: return null;
            }
        }

        private void CenterCameraOnTilemap(int width, int height)
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
            }
        }

        public int[,] GetRoomLayoutForMinimap() => roomLayout;
        public TileType[,] GetGeneratedTilemap() => finalTilemap;
        public Tilemap GetBaseLayerTilemap() => baseLayerTilemap;
    }
}
    