using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using MBAG;

namespace WFC
{
    /// <summary>
    /// MonoBehaviour that runs <see cref="RoomTreeDungeonGenerator"/> and streams the Unity tilemaps
    /// for the current room plus direct neighbors. Generated room data stays resident; rendered
    /// DualGrid input, collision, and debug overlay tile data are rebuilt as the player changes rooms.
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
        [Tooltip("If enabled, only the current room and its direct neighbors are written into Unity Tilemaps. " +
                 "Generation data stays resident; rendered/collision tile data streams as the player changes rooms.")]
        [SerializeField] private bool streamRoomObjects = true;
        [Min(0)]
        [Tooltip("How many neighbor-hops to keep loaded around the current room. 1 = current + direct neighbors, 2 = include neighbors-of-neighbors, etc.")]
        [SerializeField] private int streamedNeighborHops = 1;
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
        private Tilemap _baseTilemap;
        private Tilemap _wallCollisionTilemap;
        private Tile _debugOverlayTile;
        private GameObject _gridRoot;
        private readonly HashSet<Vector2Int> _loadedRoomKeys = new HashSet<Vector2Int>();
        private Vector2Int _lastStreamedRoomKey = new Vector2Int(int.MinValue, int.MinValue);
        private bool _hasStreamedRoom;
        private const int BaseLayerSortOrderWhenVisible = 10;

        public RoomTreeDungeonGenerator Generator => _generator;

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
            if (k != null && k[gridToggleKey].wasPressedThisFrame)
            {
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

            UpdateStreamedRoomsFromPlayer();
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
            EnsureBaseTilemapForDualGrid();
            ApplyDualGridIfConfigured();
            RebuildStreamedRoomObjects(GetInitialStreamCenterRoom());

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

            _loadedRoomKeys.Clear();
            _lastStreamedRoomKey = new Vector2Int(int.MinValue, int.MinValue);
            _hasStreamedRoom = false;
        }

        private void EnsureBaseTilemapForDualGrid()
        {
            if (_baseTilemap == null)
            {
                var go = new GameObject("RoomTreeBaseTilemap");
                go.transform.SetParent(gridParent, false);
                _baseTilemap = go.AddComponent<Tilemap>();
                var tr = go.AddComponent<TilemapRenderer>();
                tr.sortingLayerName = "Default";
                tr.sortingOrder = -2;
            }
        }

        /// <summary>
        /// Rebuilds the DualGrid input tilemap using only the loaded room set. Null space around the
        /// loaded rooms is intentionally left empty; DualGrid interprets null as wall, giving us a
        /// natural boundary around unloaded rooms without storing the full dungeon in Unity tilemaps.
        /// </summary>
        private void RebuildBaseTilemapForLoadedRooms(ISet<Vector2Int> loadedRooms)
        {
            if (_generator == null || _baseTilemap == null || loadedRooms == null) return;
            _baseTilemap.ClearAllTiles();

            foreach (Vector2Int roomKey in loadedRooms)
            {
                RoomTreeNode node = GetRoom(roomKey);
                if (node?.TileData == null) continue;

                var block = new TileBase[RoomTreeLayout.RoomSize * RoomTreeLayout.RoomSize];
                for (int y = 0; y < RoomTreeLayout.RoomSize; y++)
                    for (int x = 0; x < RoomTreeLayout.RoomSize; x++)
                        block[x + y * RoomTreeLayout.RoomSize] = GetTileAsset(node.TileData[x, y]);

                _baseTilemap.SetTilesBlock(
                    new BoundsInt(node.WorldPosition.x, node.WorldPosition.y, 0,
                        RoomTreeLayout.RoomSize, RoomTreeLayout.RoomSize, 1),
                    block);
            }

            WriteLoadedCorridors(_baseTilemap, loadedRooms, pathTile);
            _baseTilemap.RefreshAllTiles();
        }

        /// <summary>
        /// Builds a dedicated, non-rendered tilemap that holds <see cref="wallTile"/> at every cell we want
        /// to physically block (wall / water / empty / outside-rooms). All other cells are left null,
        /// so the resulting <see cref="CompositeCollider2D"/> polygon has holes exactly where the player
        /// can walk. Decouples collision from tile-asset <c>colliderType</c> and from DualGrid rendering.
        /// </summary>
        private void BuildWallCollisionTilemap(ISet<Vector2Int> loadedRooms)
        {
            if (_generator == null || wallTile == null || loadedRooms == null || loadedRooms.Count == 0) return;

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

            BoundsInt b = GetLoadedWorldBounds(loadedRooms, 1);
            HashSet<Vector2Int> loadedCorridorCells = BuildLoadedCorridorCells(loadedRooms);
            int wallCount = 0;

            _wallCollisionTilemap.ClearAllTiles();
            for (int y = b.yMin; y < b.yMax; y++)
            {
                for (int x = b.xMin; x < b.xMax; x++)
                {
                    if (loadedCorridorCells.Contains(new Vector2Int(x, y)))
                        continue;

                    if (TryGetLoadedRoomTileType(x, y, loadedRooms, out TileType t))
                    {
                        if (!RoomWalkMaskBuilder.IsWalkableTile(t))
                        {
                            _wallCollisionTilemap.SetTile(new Vector3Int(x, y, 0), debugTile);
                            wallCount++;
                        }
                        continue;
                    }

                    // Anything in the streamed bounds that is neither a loaded walkable room cell nor a loaded
                    // corridor is collision void. This blocks doors/corridors leading into unloaded rooms.
                    _wallCollisionTilemap.SetTile(new Vector3Int(x, y, 0), debugTile);
                    wallCount++;
                }
            }

            _wallCollisionTilemap.RefreshAllTiles();
            WfcTilemapCollisionUtility.EnsureTilemapCollider2D(_wallCollisionTilemap);
            Debug.Log($"[RoomTree] Wall-collision tilemap built: {wallCount}/{b.size.x * b.size.y} blocking cells " +
                      $"({(100f * wallCount) / (b.size.x * b.size.y):0.0}%) over streamed bounds={b}. " +
                      $"Overlay renderer enabled={_wallCollisionTilemap.GetComponent<TilemapRenderer>().enabled}, " +
                      $"sortingOrder={_wallCollisionTilemap.GetComponent<TilemapRenderer>().sortingOrder}.");
        }

        private void UpdateStreamedRoomsFromPlayer()
        {
            if (!streamRoomObjects || _generator?.Nodes == null || grid == null)
                return;

            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null)
                return;

            RoomTreeNode room = RoomTreeGrid.FindRoomContainingCell(_generator, grid.WorldToCell(playerGo.transform.position));
            if (room == null)
                return;

            if (_hasStreamedRoom && room.GridPosition == _lastStreamedRoomKey)
                return;

            RebuildStreamedRoomObjects(room);
        }

        private RoomTreeNode GetInitialStreamCenterRoom()
        {
            if (_generator?.Nodes == null)
                return null;

            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null && grid != null)
            {
                RoomTreeNode playerRoom = RoomTreeGrid.FindRoomContainingCell(_generator, grid.WorldToCell(playerGo.transform.position));
                if (playerRoom != null)
                    return playerRoom;
            }

            return _generator.Root;
        }

        private void RebuildStreamedRoomObjects(RoomTreeNode centerRoom)
        {
            if (_generator?.Nodes == null)
                return;

            HashSet<Vector2Int> loaded = BuildLoadedRoomSet(centerRoom);
            if (SetEquals(_loadedRoomKeys, loaded))
            {
                _lastStreamedRoomKey = centerRoom != null
                    ? centerRoom.GridPosition
                    : new Vector2Int(int.MinValue, int.MinValue);
                _hasStreamedRoom = centerRoom != null;
                return;
            }

            _loadedRoomKeys.Clear();
            foreach (Vector2Int key in loaded)
                _loadedRoomKeys.Add(key);

            RebuildBaseTilemapForLoadedRooms(_loadedRoomKeys);
            BuildWallCollisionTilemap(_loadedRoomKeys);
            RefreshDualGridFromBase();

            _lastStreamedRoomKey = centerRoom != null
                ? centerRoom.GridPosition
                : new Vector2Int(int.MinValue, int.MinValue);
            _hasStreamedRoom = centerRoom != null;
            Debug.Log($"[RoomTree] Streamed {_loadedRoomKeys.Count} room(s) around {_lastStreamedRoomKey}.");
        }

        public IReadOnlyCollection<Vector2Int> LoadedRoomKeys => _loadedRoomKeys;

        private HashSet<Vector2Int> BuildLoadedRoomSet(RoomTreeNode centerRoom)
        {
            var loaded = new HashSet<Vector2Int>();
            if (_generator?.Nodes == null)
                return loaded;

            if (!streamRoomObjects)
            {
                foreach (Vector2Int key in _generator.Nodes.Keys)
                    loaded.Add(key);
                return loaded;
            }

            if (centerRoom == null)
                centerRoom = _generator.Root;
            if (centerRoom == null)
                return loaded;

            int maxHops = Mathf.Max(0, streamedNeighborHops);
            var q = new Queue<(RoomTreeNode room, int depth)>();
            q.Enqueue((centerRoom, 0));

            while (q.Count > 0)
            {
                (RoomTreeNode room, int depth) = q.Dequeue();
                if (room == null) continue;
                if (!loaded.Add(room.GridPosition)) continue;
                if (depth >= maxHops) continue;

                for (int i = 0; i < room.Neighbors.Length; i++)
                {
                    RoomTreeNode neighbor = room.Neighbors[i];
                    if (neighbor != null)
                        q.Enqueue((neighbor, depth + 1));
                }
            }

            return loaded;
        }

        private void RefreshDualGridFromBase()
        {
            if (dualGridTilemap == null || _baseTilemap == null)
                return;

            dualGridTilemap.inputTilemap = _baseTilemap;
            _baseTilemap.CompressBounds();
            dualGridTilemap.bounds = _baseTilemap.cellBounds;
            dualGridTilemap.inputHeight = dualGridTilemap.bounds.size.y;
            dualGridTilemap.inputWidth = dualGridTilemap.bounds.size.x;
            dualGridTilemap.origin = dualGridTilemap.bounds.position;
            dualGridTilemap.floorTilemap?.ClearAllTiles();
            dualGridTilemap.wallTilemap?.ClearAllTiles();
            dualGridTilemap.RefreshFloorTilemap();
        }

        private void WriteLoadedCorridors(Tilemap tilemap, ISet<Vector2Int> loadedRooms, TileBase tile)
        {
            if (tilemap == null || tile == null)
                return;

            HashSet<Vector2Int> cells = BuildLoadedCorridorCells(loadedRooms);
            foreach (Vector2Int cell in cells)
                tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
        }

        private HashSet<Vector2Int> BuildLoadedCorridorCells(ISet<Vector2Int> loadedRooms)
        {
            var cells = new HashSet<Vector2Int>();
            if (_generator?.Nodes == null || loadedRooms == null)
                return cells;

            var seen = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (Vector2Int key in loadedRooms)
            {
                RoomTreeNode room = GetRoom(key);
                if (room == null) continue;

                for (int dir = 0; dir < room.Neighbors.Length; dir++)
                {
                    RoomTreeNode neighbor = room.Neighbors[dir];
                    if (neighbor == null || !loadedRooms.Contains(neighbor.GridPosition))
                        continue;

                    var edge = (room.GridPosition, neighbor.GridPosition);
                    var edgeRev = (neighbor.GridPosition, room.GridPosition);
                    if (seen.Contains(edge) || seen.Contains(edgeRev))
                        continue;
                    seen.Add(edge);

                    int doorStart = room.DoorStarts != null ? room.DoorStarts[dir] : -1;
                    if (doorStart < 0)
                        doorStart = (RoomTreeLayout.RoomSize - RoomTreeLayout.PathWidth) / 2;
                    AddCorridorCellsBetween(cells, room.WorldPosition, neighbor.WorldPosition, dir, doorStart);
                }
            }

            return cells;
        }

        private static void AddCorridorCellsBetween(
            HashSet<Vector2Int> cells,
            Vector2Int worldA,
            Vector2Int worldB,
            int directionFromA,
            int doorStart)
        {
            if (directionFromA == 0)
            {
                int xStart = worldA.x + doorStart;
                int yStart = worldA.y + RoomTreeLayout.RoomSize;
                int yEnd = worldB.y;
                for (int x = 0; x < RoomTreeLayout.PathWidth; x++)
                    for (int y = yStart; y < yEnd; y++)
                        cells.Add(new Vector2Int(xStart + x, y));
            }
            else if (directionFromA == 1)
            {
                int xStart = worldA.x + RoomTreeLayout.RoomSize;
                int xEnd = worldB.x;
                int yStart = worldA.y + doorStart;
                for (int x = xStart; x < xEnd; x++)
                    for (int y = 0; y < RoomTreeLayout.PathWidth; y++)
                        cells.Add(new Vector2Int(x, yStart + y));
            }
            else if (directionFromA == 2)
            {
                int xStart = worldA.x + doorStart;
                int yStart = worldB.y + RoomTreeLayout.RoomSize;
                int yEnd = worldA.y;
                for (int x = 0; x < RoomTreeLayout.PathWidth; x++)
                    for (int y = yStart; y < yEnd; y++)
                        cells.Add(new Vector2Int(xStart + x, y));
            }
            else
            {
                int xStart = worldB.x + RoomTreeLayout.RoomSize;
                int xEnd = worldA.x;
                int yStart = worldA.y + doorStart;
                for (int x = xStart; x < xEnd; x++)
                    for (int y = 0; y < RoomTreeLayout.PathWidth; y++)
                        cells.Add(new Vector2Int(x, yStart + y));
            }
        }

        private BoundsInt GetLoadedWorldBounds(ISet<Vector2Int> loadedRooms, int margin)
        {
            if (loadedRooms == null || loadedRooms.Count == 0)
                return _generator != null ? _generator.DungeonBounds : new BoundsInt(0, 0, 0, 1, 1, 1);

            int xMin = int.MaxValue;
            int yMin = int.MaxValue;
            int xMax = int.MinValue;
            int yMax = int.MinValue;

            foreach (Vector2Int key in loadedRooms)
            {
                RoomTreeNode room = GetRoom(key);
                if (room == null) continue;

                xMin = Mathf.Min(xMin, room.WorldPosition.x);
                yMin = Mathf.Min(yMin, room.WorldPosition.y);
                xMax = Mathf.Max(xMax, room.WorldPosition.x + RoomTreeLayout.RoomSize);
                yMax = Mathf.Max(yMax, room.WorldPosition.y + RoomTreeLayout.RoomSize);
            }

            foreach (Vector2Int cell in BuildLoadedCorridorCells(loadedRooms))
            {
                xMin = Mathf.Min(xMin, cell.x);
                yMin = Mathf.Min(yMin, cell.y);
                xMax = Mathf.Max(xMax, cell.x + 1);
                yMax = Mathf.Max(yMax, cell.y + 1);
            }

            if (xMin == int.MaxValue)
                return _generator != null ? _generator.DungeonBounds : new BoundsInt(0, 0, 0, 1, 1, 1);

            xMin -= margin;
            yMin -= margin;
            xMax += margin;
            yMax += margin;
            return new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
        }

        private bool TryGetLoadedRoomTileType(int x, int y, ISet<Vector2Int> loadedRooms, out TileType tileType)
        {
            foreach (Vector2Int key in loadedRooms)
            {
                RoomTreeNode room = GetRoom(key);
                if (room?.TileData == null) continue;

                Vector2Int wp = room.WorldPosition;
                if (x < wp.x || x >= wp.x + RoomTreeLayout.RoomSize ||
                    y < wp.y || y >= wp.y + RoomTreeLayout.RoomSize)
                    continue;

                tileType = room.TileData[x - wp.x, y - wp.y];
                return true;
            }

            tileType = TileType.Empty;
            return false;
        }

        private RoomTreeNode GetRoom(Vector2Int key)
        {
            return _generator?.Nodes != null && _generator.Nodes.TryGetValue(key, out RoomTreeNode room)
                ? room
                : null;
        }

        private static bool SetEquals(HashSet<Vector2Int> a, HashSet<Vector2Int> b)
        {
            if (a == null || b == null)
                return a == b;
            return a.SetEquals(b);
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
