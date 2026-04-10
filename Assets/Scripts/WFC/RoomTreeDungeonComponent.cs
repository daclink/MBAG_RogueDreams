using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using MBAG;

namespace WFC
{
    /// <summary>
    /// MonoBehaviour that runs RoomTreeDungeonGenerator and builds Unity Tilemaps.
    /// Creates one Tilemap per room + one corridor Tilemap. Use with tile assets (grass, dirt, path, water, wall, empty).
    /// DualGrid/RoomManager integration is separate.
    /// </summary>
    public class RoomTreeDungeonComponent : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private int randomSeed;
        [SerializeField] private bool generateOnStart = true;
        [Min(1)]
        [SerializeField] private int gridSize = 4;

        [Header("Tile Assets")]
        [SerializeField] private TileBase emptyTile;
        [SerializeField] private TileBase grassTile;
        [SerializeField] private TileBase dirtTile;
        [SerializeField] private TileBase pathTile;
        [SerializeField] private TileBase waterTile;
        [SerializeField] private TileBase wallTile;

        [Header("Output")]
        [SerializeField] private Transform gridParent;
        [SerializeField] private Grid grid;

        [Header("DualGrid (optional – canonical floor)")]
        [SerializeField] private DualGridTilemap dualGridTilemap;
        [SerializeField] private BiomeTileRegistry biomeTileRegistry;
        [SerializeField] private int currentBiome = 0;
        [SerializeField] private bool hideBaseTilemapAfterDualGrid = true;
        [Tooltip("Tiles outside any room (the 'void' between rooms) are written as Wall in the DualGrid base tilemap.\n" +
                 "Disable to use Empty instead. This affects DualGrid rendering only.")]
        [SerializeField] private bool treatOutsideRoomsAsWall = true;
        [Tooltip("Press to toggle raw WFC base grid visibility for troubleshooting")]
        [SerializeField] private Key gridToggleKey = Key.G;

        private RoomTreeDungeonGenerator _generator;
        private readonly Dictionary<Vector2Int, Tilemap> _roomTilemaps = new Dictionary<Vector2Int, Tilemap>();
        private Tilemap _corridorTilemap;
        private Tilemap _baseTilemap;
        private GameObject _gridRoot;
        private const int BaseLayerSortOrderWhenVisible = 10;

        public RoomTreeDungeonGenerator Generator => _generator;
        public IReadOnlyDictionary<Vector2Int, Tilemap> RoomTilemaps => _roomTilemaps;

        void Start()
        {
            if (generateOnStart) Generate();
        }

        void Update()
        {
            if (_baseTilemap == null) return;
            var r = _baseTilemap.GetComponent<TilemapRenderer>();
            if (r == null) return;
            var k = Keyboard.current;
            if (k != null && k[gridToggleKey].wasPressedThisFrame)
            {
                r.enabled = !r.enabled;
                if (r.enabled)
                    r.sortingOrder = BaseLayerSortOrderWhenVisible;
                Debug.Log($"[RoomTree] Base grid (raw WFC) {(r.enabled ? "visible" : "hidden")} [press {gridToggleKey} to toggle]");
            }
        }

        [ContextMenu("Generate Room Tree Dungeon")]
        public void Generate()
        {
            Debug.Log("[RoomTree] Generate() called");

            int nullTiles = (emptyTile == null ? 1 : 0) + (grassTile == null ? 1 : 0) +
                            (dirtTile == null ? 1 : 0) + (pathTile == null ? 1 : 0) +
                            (waterTile == null ? 1 : 0) + (wallTile == null ? 1 : 0);
            if (nullTiles > 0)
                Debug.LogWarning($"[RoomTree] {nullTiles}/6 tile assets are NULL — " +
                    $"empty={emptyTile != null} grass={grassTile != null} dirt={dirtTile != null} " +
                    $"path={pathTile != null} water={waterTile != null} wall={wallTile != null}");

            _generator = new RoomTreeDungeonGenerator(gridSize, randomSeed);
            _generator.Generate();

            Debug.Log($"[RoomTree] Generated {_generator.Nodes.Count} rooms, " +
                      $"gridSize={_generator.GridSize}, bounds={_generator.DungeonBounds}, root={_generator.Root?.GridPosition}");

            EnsureGrid();
            Debug.Log($"[RoomTree] Grid created: {grid != null}, parent: {gridParent?.name}");

            ClearExisting();
            BuildRoomTilemaps();
            Debug.Log($"[RoomTree] Built {_roomTilemaps.Count} room tilemaps");

            BuildCorridorTilemap();
            Debug.Log($"[RoomTree] Corridor tilemap built. Camera at {Camera.main?.transform.position}, " +
                      $"orthoSize={Camera.main?.orthographicSize}");

            BuildBaseTilemapForDualGrid();
            ApplyDualGridIfConfigured();
        }

        private void EnsureGrid()
        {
            if (grid != null && gridParent == null) gridParent = grid.transform;
            if (gridParent == null)
            {
                _gridRoot = new GameObject("RoomTreeGrid");
                _gridRoot.transform.SetParent(transform, false);
                grid = _gridRoot.AddComponent<Grid>();
                grid.cellSize = new Vector3(1, 1, 0);
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
                gridParent = _gridRoot.transform;
            }
        }

        private void ClearExisting()
        {
            foreach (var tm in _roomTilemaps.Values)
                if (tm != null && tm.gameObject != null) DestroyImmediate(tm.gameObject);
            _roomTilemaps.Clear();
            if (_corridorTilemap != null && _corridorTilemap.gameObject != null)
            {
                DestroyImmediate(_corridorTilemap.gameObject);
                _corridorTilemap = null;
            }

            if (_baseTilemap != null && _baseTilemap.gameObject != null)
            {
                DestroyImmediate(_baseTilemap.gameObject);
                _baseTilemap = null;
            }
        }

        private void BuildRoomTilemaps()
        {
            foreach (var kvp in _generator.Nodes)
            {
                var node = kvp.Value;
                var go = new GameObject($"Room_{node.GridPosition.x}_{node.GridPosition.y}");
                go.transform.SetParent(gridParent, false);

                var tm = go.AddComponent<Tilemap>();
                var tr = go.AddComponent<TilemapRenderer>();
                tr.sortingOrder = 0;
                tr.sortingLayerName = "Default";

                var block = new TileBase[100];
                for (int y = 0; y < 10; y++)
                    for (int x = 0; x < 10; x++)
                        block[x + y * 10] = GetTileAsset(node.TileData[x, y]);
                var bounds = new BoundsInt(node.WorldPosition.x, node.WorldPosition.y, 0, 10, 10, 1);
                tm.SetTilesBlock(bounds, block);

                _roomTilemaps[node.GridPosition] = tm;
            }
        }

        private void BuildCorridorTilemap()
        {
            var go = new GameObject("CorridorTilemap");
            go.transform.SetParent(gridParent, false);
            go.transform.localPosition = Vector3.zero;

            _corridorTilemap = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -1;
            tr.sortingLayerName = "Default";

            var b = _generator.DungeonBounds;
            int w = b.size.x;
            int h = b.size.y;
            var block = new TileBase[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var t = _generator.CorridorTilemap[x, y];
                    block[x + y * w] = t == TileType.Path ? pathTile : null;
                }
            _corridorTilemap.SetTilesBlock(new BoundsInt(b.xMin, b.yMin, 0, w, h, 1), block);
        }

        /// <summary>
        /// Builds a single base Tilemap covering the entire dungeon bounds using WFC tiles.
        /// This is used as the DualGrid input tilemap so canonical hazard tiles can be rendered.
        /// </summary>
        private void BuildBaseTilemapForDualGrid()
        {
            if (_generator == null) return;

            var b = _generator.DungeonBounds;
            int w = b.size.x;
            int h = b.size.y;

            if (_baseTilemap == null)
            {
                var go = new GameObject("RoomTreeBaseTilemap");
                go.transform.SetParent(gridParent, false);
                _baseTilemap = go.AddComponent<Tilemap>();
                var tr = go.AddComponent<TilemapRenderer>();
                tr.sortingLayerName = "Default";
                tr.sortingOrder = -2;
            }

            TileBase[] block = new TileBase[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    TileType t = TileType.Empty;
                    bool inRoom = false;

                    // Sample room data (10x10 per room)
                    foreach (var kvp in _generator.Nodes)
                    {
                        var node = kvp.Value;
                        Vector2Int wp = node.WorldPosition;
                        if (x >= wp.x && x < wp.x + 10 && y >= wp.y && y < wp.y + 10)
                        {
                            int lx = x - wp.x;
                            int ly = y - wp.y;
                            t = node.TileData[lx, ly];
                            inRoom = true;
                            break;
                        }
                    }

                    if (_generator.CorridorTilemap[x, y] == TileType.Path)
                        t = TileType.Path;
                    else if (!inRoom && treatOutsideRoomsAsWall)
                        t = TileType.Wall;

                    block[x + y * w] = GetTileAsset(t);
                }
            }

            _baseTilemap.ClearAllTiles();
            _baseTilemap.SetTilesBlock(new BoundsInt(b.xMin, b.yMin, 0, w, h, 1), block);
            Debug.Log("[RoomTree] Base tilemap for DualGrid built.");
        }

        /// <summary>
        /// Wires DualGridTilemap to the Room Tree base tilemap using the shared BiomeTileRegistry.
        /// Mirrors DungeonGeneration.ApplyDualGridIfConfigured.
        /// </summary>
        private void ApplyDualGridIfConfigured()
        {
            if (dualGridTilemap == null || biomeTileRegistry == null || _baseTilemap == null) return;

            BiomeTileSet tileSet = biomeTileRegistry.GetBiomeSet(currentBiome);
            if (tileSet == null)
            {
                Debug.LogError($"RoomTreeDungeonComponent: No BiomeTileSet for biome {currentBiome}.");
                return;
            }

            // Match placeholders by reference so DualGrid can infer TileType values.
            dualGridTilemap.SetPlaceholderTiles(
                emptyTile as Tile,
                grassTile as Tile,
                dirtTile as Tile,
                pathTile as Tile,
                waterTile as Tile,
                wallTile as Tile);

            dualGridTilemap.inputTilemap = _baseTilemap;
            dualGridTilemap.TileSet = tileSet;

            Transform gridTransform = gridParent != null ? gridParent : _baseTilemap.transform.parent;
            if (dualGridTilemap.floorTilemap == null)
            {
                var go = new GameObject("RoomTreeFloorTilemap");
                go.transform.SetParent(gridTransform, false);
                dualGridTilemap.floorTilemap = go.AddComponent<Tilemap>();
                var tr = go.AddComponent<TilemapRenderer>();
                tr.sortingLayerName = "Default";
                tr.sortingOrder = 0;
            }

            if (dualGridTilemap.wallTilemap == null)
            {
                var go = new GameObject("RoomTreeWallTilemap");
                go.transform.SetParent(gridTransform, false);
                dualGridTilemap.wallTilemap = go.AddComponent<Tilemap>();
                var tr = go.AddComponent<TilemapRenderer>();
                tr.sortingLayerName = "Default";
                tr.sortingOrder = 1;
            }

            if (hideBaseTilemapAfterDualGrid && _baseTilemap != null)
            {
                var r = _baseTilemap.GetComponent<TilemapRenderer>();
                if (r != null) r.enabled = false;
            }

            Debug.Log("[RoomTree] DualGrid wired: BiomeTileSet + base tilemap ready.");
        }

        private TileBase GetTileAsset(TileType type)
        {
            return type switch
            {
                TileType.Grass => grassTile,
                TileType.Dirt => dirtTile,
                TileType.Path => pathTile,
                TileType.Water => waterTile,
                TileType.Wall => wallTile,
                TileType.Empty => emptyTile,
                _ => null
            };
        }
    }
}
