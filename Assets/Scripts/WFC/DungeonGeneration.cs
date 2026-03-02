using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace WFC
{
    
    // ---------------------  ENUMS  -----------------------
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
        
        [Header("Auto-Setup Configuration")]
        [SerializeField] private bool autoSetupOnStart = true;
        [Tooltip("If true, will automatically create missing hierarchy on Start")]
        
        [Header("Tilemap References")]
        private Tilemap baseLayerTilemap;
        private Grid gridComponent;

        [Header("WFC Tile Assets")] 
        [SerializeField] private TileBase emptyTile;
        [SerializeField] private TileBase grassTile;
        [SerializeField] private TileBase dirtTile;
        [SerializeField] private TileBase pathTile;
        [SerializeField] private TileBase waterTile;
        [SerializeField] private TileBase wallTile;

        [Header("Minimap References")]

        private MinimapRenderer minimapRenderer;
        private RawImage minimapImage;
        private RectTransform minimapPanel;
        
        [Header("Minimap Configuration")]
        [SerializeField] private int pixelsPerRoom = 16;
        [SerializeField] private int roomSpacing = 4;
        [SerializeField] private int roomBorderSize = 2;
        [SerializeField] private int minimapPaddingRooms = 1;
        [SerializeField] private bool showMinimapConnections = true;
        [SerializeField] private int minimapConnectionWidth = 2;
        
        [Header("Minimap Colors")]
        [SerializeField] private Color minimapPanelBackgroundColor = new Color(0, 0, 0, 0f);
        [SerializeField] private Color minimapConnectionColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color minimapEmptyColor = new Color(0.1f, 0.1f, 0.1f, 0f);
        [SerializeField] private Color minimapNormalRoomColor = Color.white;
        [SerializeField] private Color minimapStartRoomColor = Color.green;
        [SerializeField] private Color minimapEndRoomColor = Color.red;
        [SerializeField] private Color minimapItemRoomColor = Color.yellow;
        [SerializeField] private Color minimapBorderColor = Color.black;

        [Header("Generation Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private int randomSeed = 0;
        [SerializeField] private int mapWidth = 10;
        [SerializeField] private int mapHeight = 10;
        [SerializeField] private int minRooms = 8;
        [SerializeField] private int maxRooms = 16;
        
        [Header("WFC Settings")]
        [SerializeField] private int pathWidth = 2;

        private RoomLayoutGenerator roomLayoutGen;
        private WFCTilemap wfcGen;
        private TileType[,] finalTilemap;
        private int[,] roomLayout;

        
        /**
        * --------------------  MONOBEHAVIOR METHODS  ----------------------
        */
        void Start()
        {
            // Auto-setup hierarchy if enabled and components are missing
            if (autoSetupOnStart)
            {
                CheckAndSetupHierarchy();
            }
            
            if (generateOnStart)
            {
                GenerateCompleteDungeon();
            }
        }
        
        /**
         * --------------------  MAIN DRIVER METHOD  ----------------------
         */
        
        /**
         * Generates the dungeon in multiple stages
         */
        public void GenerateCompleteDungeon()
        {
            
            // Check if hierarchy is set up
            if (baseLayerTilemap == null || gridComponent == null)
            {
                // Debug.LogError("Hierarchy not set up! Attempting auto-setup...");
                CheckAndSetupHierarchy();
                
                // Check again after setup attempt
                if (baseLayerTilemap == null || gridComponent == null)
                {
                    // Debug.LogError("Auto-setup failed! Cannot generate dungeon.");
                    return;
                }
            }
            
            if (randomSeed != 0)
            {
                UnityEngine.Random.InitState(randomSeed);
            }
            
            // Stage 1: Generate room layout
            roomLayoutGen = new RoomLayoutGenerator();
            roomLayout = roomLayoutGen.GenerateRoomGrid(mapWidth, mapHeight, minRooms, maxRooms);

            var roomPositions = roomLayoutGen.GetRoomPositions();
            
            // Could be useful for later on when needing to pass coordinates to the player and camera for spawn points
            // var startRoom = roomLayoutGen.GetStartRoomPosition();
            // var endRoom = roomLayoutGen.GetEndRoomPosition();
            // var itemRoom = roomLayoutGen.GetItemRoomPosition();
            
            if (minimapRenderer != null)
            {
                // Initialize if not already initialized
                if (!minimapRenderer.IsInitialized())
                {
                    InitializeMinimapRenderer();
                }
                // Render the minimap
                minimapRenderer.RenderMinimap(roomLayout);
            }    
            else
            {
                Debug.LogWarning("MinimapRenderer is null - minimap will not be rendered");
            }

            // Stage 2: Generate tilemap using WFC
            wfcGen = new WFCTilemap(roomLayout, roomPositions, roomLayoutGen, pathWidth);
            finalTilemap = wfcGen.Generate();

            Vector2Int tilemapSize = wfcGen.GetTilemapSize();

            // Stage 3: Build Unity Tilemap
            BuildUnityTilemap(finalTilemap);
        }
        
        /**
         * ------------------ TILEMAP CREATION METHODS ---------------------
         *
         * These create the tilemap given information from the growth algorithm
         */

        /**
         * Builds the tilemap by setting tiles
         */
        private void BuildUnityTilemap(TileType[,] tileData)
        {
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
            // Can make method call here to move camera over the start room
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
        
        
        /**
         * --------------------  HIERARCHY SETUP METHODS  -----------------------
         *
         * These are used to set up the hierarchy including the canvas elements for the minimap and
         * the grid/tilemap elements
        */
        
        /**
        * Checks if hierarchy is set up, and creates it if missing
        * Safe to call multiple times - only creates what's missing
        */
        public void CheckAndSetupHierarchy()
        {
            bool needsSetup = IsSetupNeeded();

            if (needsSetup)
            {
                AutoSetupHierarchy();
            }
        }

        /**
         * Checks for missing components
         */
        private bool IsSetupNeeded()
        {
            return baseLayerTilemap == null || 
                   gridComponent == null || 
                   minimapRenderer == null || 
                   minimapImage == null || 
                   minimapPanel == null;
        }

        /**
         *  Public method to manually trigger hierarchy setup
         *  Can be called from Inspector or from other scripts
         */
        [ContextMenu("Auto-Setup Hierarchy")]
        public void AutoSetupHierarchy()
        {
            // Setup Grid and Tilemap if missing
            if (baseLayerTilemap == null || gridComponent == null)
            {
                SetupGridAndTilemap();
            }

            // Setup Minimap UI if missing
            if (minimapRenderer == null || minimapImage == null || minimapPanel == null)
            {
                SetupMinimapUI();
            }
            
            
#if UNITY_EDITOR
            // Mark object as dirty so Unity saves the changes
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        /**
         * Force a recreate of the hierarchy scene objects. Not used but could be useful later on when dealing
         * with multiple maps in one run
         */
        [ContextMenu("Force Recreate Hierarchy")]
        public void ForceRecreateHierarchy()
        {
            // Clean up existing
            DungeonSetup.CleanupExistingHierarchy(transform);

            // Clear references
            baseLayerTilemap = null;
            gridComponent = null;
            minimapRenderer = null;
            minimapImage = null;
            minimapPanel = null;

            // Recreate
            AutoSetupHierarchy();
        }
        
        /**
         * Setup the grid and tilemap components
         */
        private void SetupGridAndTilemap()
        {
            // Check if Grid already exists as child
            Transform existingGrid = transform.Find("MapGrid");
            if (existingGrid != null && existingGrid.GetComponent<Grid>() != null)
            {
                gridComponent = existingGrid.GetComponent<Grid>();
                
                // Check for tilemap
                Transform existingTilemap = existingGrid.Find("BaseLayerTilemap");
                if (existingTilemap != null && existingTilemap.GetComponent<Tilemap>() != null)
                {
                    baseLayerTilemap = existingTilemap.GetComponent<Tilemap>();
                    return;
                }
            }

            // Create new Grid and Tilemap
            var setup = DungeonSetup.SetupDungeonHierarchy(transform);
            gridComponent = setup.grid;
            baseLayerTilemap = setup.baseTilemap;

            Debug.Log("Grid and Tilemap created");
        }

        /**
         * Setup minimap UI components
         */
        private void SetupMinimapUI()
        {
            // Check if Canvas exists
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform existingPanel = canvas.transform.Find("MinimapPanel");
                if (existingPanel != null)
                {
                    minimapPanel = existingPanel.GetComponent<RectTransform>();
                    
                    Transform existingImage = existingPanel.Find("MinimapImage");
                    if (existingImage != null)
                    {
                        minimapImage = existingImage.GetComponent<RawImage>();
                    }
                }
            }

            // Check if MinimapRenderer exists
            if (minimapRenderer == null)
            {
                minimapRenderer = FindObjectOfType<MinimapRenderer>();
            }

            // Create missing components
            if (minimapPanel == null || minimapImage == null || minimapRenderer == null)
            {
                var setup = DungeonSetup.SetupDungeonHierarchy(transform);
                
                if (minimapPanel == null) minimapPanel = setup.minimapPanel;
                if (minimapImage == null) minimapImage = setup.minimapImage;
                
                if (minimapRenderer == null && setup.minimapRendererObject != null)
                {
                    minimapRenderer = setup.minimapRendererObject.GetComponent<MinimapRenderer>();
                }
                
                Image panelImage = minimapPanel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = minimapPanelBackgroundColor;
                }

                InitializeMinimapRenderer();
            }

            Debug.Log("Minimap UI components configured");
        }
        
        /**
         *  Initialize the MinimapRenderer with values from this script's Inspector
         *  Calls the initialize method from minimapRenderer
         */
        private void InitializeMinimapRenderer()
        {
            if (minimapRenderer == null || minimapImage == null || minimapPanel == null)
            {
                Debug.LogWarning("Cannot initialize MinimapRenderer - missing components");
                return;
            }

            minimapRenderer.Initialize(
                image: minimapImage,
                container: minimapPanel,
                pixelsPerRoom: pixelsPerRoom,
                roomSpacing: roomSpacing,
                roomBorderSize: roomBorderSize,
                paddingRooms: minimapPaddingRooms,
                showConnections: showMinimapConnections,
                connectionWidth: minimapConnectionWidth,
                connectionColor: minimapConnectionColor,
                emptyColor: minimapEmptyColor,
                normalRoomColor: minimapNormalRoomColor,
                startRoomColor: minimapStartRoomColor,
                endRoomColor: minimapEndRoomColor,
                itemRoomColor: minimapItemRoomColor,
                borderColor: minimapBorderColor
            );
        }

        
        /**
         * --------------------------  GETTERS  -------------------------------
         * 
         */
        
        public int[,] GetRoomLayoutForMinimap() => roomLayout;
        public TileType[,] GetGeneratedTilemap() => finalTilemap;
        public Tilemap GetBaseLayerTilemap() => baseLayerTilemap;
    }
}
    