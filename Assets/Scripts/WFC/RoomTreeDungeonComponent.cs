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
        [Tooltip("Press to toggle the red wall-collision overlay (shows exactly where physics walls are) and raw WFC grid.")]
        [SerializeField] private Key gridToggleKey = Key.G;
        [Tooltip("Color applied to the wall-collision overlay so physical walls are visually distinct from DualGrid rendering.")]
        [SerializeField] private Color wallCollisionDebugColor = new Color(1f, 0f, 0f, 0.55f);
        [Tooltip("Show the red wall-collision debug overlay at start (press G to toggle).")]
        [SerializeField] private bool showWallCollisionDebugOverlay = true;
        [Tooltip("Local-space offset applied to the wall-collision tilemap. DualGrid canonical tiles render shifted by -0.5/-0.5 " +
                 "from the input grid, so the collider tilemap needs the same shift to line up with what you see on screen.")]
        [SerializeField] private Vector2 wallCollisionOffset = new Vector2(-0.5f, -0.5f);

        private RoomTreeDungeonGenerator _generator;
        private int _layoutVersion;
        private readonly Dictionary<Vector2Int, Tilemap> _roomTilemaps = new Dictionary<Vector2Int, Tilemap>();
        private Tilemap _corridorTilemap;
        private Tilemap _baseTilemap;
        private Tilemap _wallCollisionTilemap;
        private Tile _debugOverlayTile;
        private GameObject _gridRoot;
        private const int BaseLayerSortOrderWhenVisible = 10;

        public RoomTreeDungeonGenerator Generator => _generator;
        public IReadOnlyDictionary<Vector2Int, Tilemap> RoomTilemaps => _roomTilemaps;

        /// <summary>Bumps each <see cref="Generate"/>; use with <see cref="RoomWalkMaskCache"/> keys.</summary>
        public int LayoutVersion => _layoutVersion;

        /// <summary>Grid used for room tilemaps (same reference as serialized <c>grid</c> after <see cref="EnsureGrid"/>).</summary>
        public Grid DungeonGrid => grid;

        /// <summary>Invoked after generation, tilemaps, base layer, and optional DualGrid are complete.</summary>
        public static event System.Action<RoomTreeDungeonComponent> OnRoomTreeGenerated;

        void Start()
        {
            if (generateOnStart) Generate();
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (!k[gridToggleKey].wasPressedThisFrame) return;

            // Toggle the red wall-collision debug overlay. This is the visualization that lets you
            // confirm walls are physically there (rendered on top of the DualGrid in wallCollisionDebugColor).
            if (_wallCollisionTilemap != null)
            {
                var r = _wallCollisionTilemap.GetComponent<TilemapRenderer>();
                if (r != null)
                {
                    r.enabled = !r.enabled;
                    Debug.Log($"[RoomTree] Wall-collision overlay {(r.enabled ? "visible" : "hidden")} [press {gridToggleKey} to toggle]");
                }
            }

            // Also toggle raw WFC base grid so you can inspect the underlying tile types directly.
            if (_baseTilemap != null)
            {
                var r = _baseTilemap.GetComponent<TilemapRenderer>();
                if (r != null)
                {
                    r.enabled = !r.enabled;
                    if (r.enabled) r.sortingOrder = BaseLayerSortOrderWhenVisible;
                }
            }
        }

        [ContextMenu("Generate Room Tree Dungeon")]
        public void Generate()
        {
            _layoutVersion++;
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
            BuildWallCollisionTilemap();
            ApplyDualGridIfConfigured();

            OnRoomTreeGenerated?.Invoke(this);
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

            if (_wallCollisionTilemap != null && _wallCollisionTilemap.gameObject != null)
            {
                DestroyImmediate(_wallCollisionTilemap.gameObject);
                _wallCollisionTilemap = null;
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
        /// Builds a dedicated, non-rendered tilemap that holds <see cref="wallTile"/> at every cell we want
        /// to physically block (wall / water / empty / outside-rooms). All other cells are left null,
        /// so the resulting <see cref="CompositeCollider2D"/> polygon has holes exactly where the player
        /// can walk. Decouples collision from tile-asset <c>colliderType</c> and from DualGrid rendering.
        /// </summary>
        private void BuildWallCollisionTilemap()
        {
            if (_generator == null || wallTile == null) return;

            var b = _generator.DungeonBounds;
            int w = b.size.x;
            int h = b.size.y;

            if (_wallCollisionTilemap == null)
            {
                var go = new GameObject("RoomTreeWallCollisionTilemap");
                go.transform.SetParent(gridParent, false);
                _wallCollisionTilemap = go.AddComponent<Tilemap>();
                // Debug overlay: render on top of the DualGrid in a distinct tint so the player
                // can clearly see where physics walls are. Toggle at runtime via the G key.
                var tr = go.AddComponent<TilemapRenderer>();
                tr.sortingLayerName = "Default";
                // Very high sort order so we land on top of DualGrid floor/wall tilemaps.
                tr.sortingOrder = 9999;
                tr.enabled = showWallCollisionDebugOverlay;
            }

            // Shift the whole wall-collision tilemap so its render AND its collider line up with the DualGrid
            // canonical tiles (which render offset -0.5/-0.5 from the input grid).
            _wallCollisionTilemap.transform.localPosition = new Vector3(wallCollisionOffset.x, wallCollisionOffset.y, 0f);
            _wallCollisionTilemap.color = wallCollisionDebugColor;

            // Build a plain white-sprite Tile used ONLY for the debug overlay. Keeping it white
            // means the multiplicative Tilemap.color tint looks vivid regardless of what the
            // source wallTile art looks like. Its collider shape is the full cell (Sprite).
            var debugTile = GetOrCreateDebugOverlayTile();

            TileBase[] block = new TileBase[w * h];
            int wallCount = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    TileType t = TileType.Empty;
                    bool inRoom = false;

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

                    bool isCorridor = _generator.CorridorTilemap[x, y] == TileType.Path;
                    if (isCorridor)
                    {
                        block[x + y * w] = null;
                        continue;
                    }

                    if (!inRoom)
                    {
                        block[x + y * w] = debugTile;
                        wallCount++;
                        continue;
                    }

                    if (!RoomWalkMaskBuilder.IsWalkableTile(t))
                    {
                        block[x + y * w] = debugTile;
                        wallCount++;
                    }
                    else
                    {
                        block[x + y * w] = null;
                    }
                }
            }

            _wallCollisionTilemap.ClearAllTiles();
            _wallCollisionTilemap.SetTilesBlock(new BoundsInt(b.xMin, b.yMin, 0, w, h, 1), block);
            WfcTilemapCollisionUtility.EnsureTilemapCollider2D(_wallCollisionTilemap);
            Debug.Log($"[RoomTree] Wall-collision tilemap built: {wallCount}/{w * h} blocking cells " +
                      $"({(100f * wallCount) / (w * h):0.0}%) over bounds={b}. " +
                      $"Overlay renderer enabled={_wallCollisionTilemap.GetComponent<TilemapRenderer>().enabled}, " +
                      $"sortingOrder={_wallCollisionTilemap.GetComponent<TilemapRenderer>().sortingOrder}.");
        }

        /// <summary>
        /// Lazily builds a 1x1 white Tile used only for the debug overlay. Using a plain white sprite
        /// means the overlay shows as a solid <see cref="wallCollisionDebugColor"/> on screen
        /// (Tilemap.color is multiplicative and doesn't tint toward bright colors against dark art).
        /// </summary>
        private Tile GetOrCreateDebugOverlayTile()
        {
            if (_debugOverlayTile != null && _debugOverlayTile.sprite != null) return _debugOverlayTile;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "RoomTree_DebugOverlayTex",
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            sprite.name = "RoomTree_DebugOverlaySprite";

            _debugOverlayTile = ScriptableObject.CreateInstance<Tile>();
            _debugOverlayTile.hideFlags = HideFlags.HideAndDontSave;
            _debugOverlayTile.name = "RoomTree_DebugOverlayTile";
            _debugOverlayTile.sprite = sprite;
            _debugOverlayTile.color = Color.white;
            // Grid (not Sprite) so TilemapCollider2D gets a full cell-sized box per tile.
            // A procedurally-created Sprite.Create'd texture has no physics shape, so ColliderType.Sprite
            // would leave shapeCount=0 even when the tilemap is populated (root cause of the "0 shapes" bug).
            _debugOverlayTile.colliderType = Tile.ColliderType.Grid;
            return _debugOverlayTile;
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
