using System.Collections.Generic;
using UnityEngine;
using MBAG;

namespace WFC
{
    /// <summary>
    /// Single-room WFC. 10×10 total (8×8 walkable + walls). Uses WFCCore for collapse/propagate.
    /// </summary>
    public class RoomWFCTilemap
    {
        private const int RoomSize = RoomTreeLayout.RoomSize;
        private const int WallThickness = 1;
        private const int InteriorSize = 8;
        private const int PathWidth = RoomTreeLayout.PathWidth;

        private readonly int _northDoorStart;
        private readonly int _eastDoorStart;
        private readonly int _southDoorStart;
        private readonly int _westDoorStart;
        private readonly bool _allowWater;

        private HashSet<TileType>[,] _tilePossibilities;
        private TileType[,] _collapsedTilemap;
        private bool[,] _isPrePlaced;
        private List<Vector2Int> _uncollapsedCells;
        private Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>> _adjacencyRules;

        public RoomWFCTilemap(bool hasNorth, bool hasEast, bool hasSouth, bool hasWest, bool allowWater = true)
        {
            // Backward-compatible ctor: uses centered door positions.
            int centered = (RoomSize - PathWidth) / 2;
            _northDoorStart = hasNorth ? centered : -1;
            _eastDoorStart = hasEast ? centered : -1;
            _southDoorStart = hasSouth ? centered : -1;
            _westDoorStart = hasWest ? centered : -1;
            _allowWater = allowWater;
        }

        /// <summary>
        /// Door start offsets are the start index along that wall (see RoomTreeNode.DoorStarts).
        /// Pass -1 for "no door on that side".
        /// </summary>
        public RoomWFCTilemap(int northDoorStart, int eastDoorStart, int southDoorStart, int westDoorStart, bool allowWater = true)
        {
            _northDoorStart = northDoorStart;
            _eastDoorStart = eastDoorStart;
            _southDoorStart = southDoorStart;
            _westDoorStart = westDoorStart;
            _allowWater = allowWater;
        }

        public TileType[,] Generate()
        {
            _adjacencyRules = WFCCore.GetAdjacencyRules();
            InitializeSuperposition();
            PrePlaceStaticTiles();
            PropagateAllPrePlacedConstraints();
            BuildUncollapsedCellsList();

            int maxIterations = RoomSize * RoomSize * 3;
            int iterations = 0;

            while (!IsFullyCollapsed() && iterations < maxIterations)
            {
                var cell = WFCCore.FindLowestEntropyCell(_tilePossibilities, _isPrePlaced, _uncollapsedCells);
                if (cell == null) break;

                WFCCore.CollapseCell(cell.Value, _tilePossibilities, _collapsedTilemap, RoomSize, RoomSize, _adjacencyRules);
                WFCCore.Propagate(cell.Value, _tilePossibilities, _isPrePlaced, _adjacencyRules, IsInBounds);
                _uncollapsedCells.Remove(cell.Value);
                iterations++;
            }

            WFCCore.FinalizeTilemap(_tilePossibilities, _collapsedTilemap, _isPrePlaced, RoomSize, RoomSize);
            return _collapsedTilemap;
        }

        private void InitializeSuperposition()
        {
            _tilePossibilities = new HashSet<TileType>[RoomSize, RoomSize];
            _collapsedTilemap = new TileType[RoomSize, RoomSize];
            _isPrePlaced = new bool[RoomSize, RoomSize];

            // IMPORTANT: we do NOT include TileType.Path in the random superposition.
            // Paths are carved deterministically from doors so they form a continuous network.
            var interiorTypes = new HashSet<TileType> { TileType.Grass, TileType.Dirt };
            if (_allowWater) interiorTypes.Add(TileType.Water);

            for (int x = 0; x < RoomSize; x++)
            {
                for (int y = 0; y < RoomSize; y++)
                {
                    _tilePossibilities[x, y] = new HashSet<TileType>(interiorTypes);
                    _collapsedTilemap[x, y] = TileType.Empty;
                    _isPrePlaced[x, y] = false;
                }
            }
        }

        private void PrePlaceStaticTiles()
        {
            // 1. Perimeter walls
            for (int x = 0; x < RoomSize; x++)
            {
                PlaceTile(x, 0, TileType.Wall);
                PlaceTile(x, RoomSize - 1, TileType.Wall);
            }
            for (int y = 0; y < RoomSize; y++)
            {
                PlaceTile(0, y, TileType.Wall);
                PlaceTile(RoomSize - 1, y, TileType.Wall);
            }

            // 2. Doors (path through walls).
            // Allow any start along the chosen wall, but keep corners intact by clamping.
            PlaceDoorNorth(_northDoorStart);
            PlaceDoorSouth(_southDoorStart);
            PlaceDoorEast(_eastDoorStart);
            PlaceDoorWest(_westDoorStart);

            // 3. Guaranteed 1-tile-wide path that connects door openings inside the room.
            CarvePersistentPathThroughRoom();
            CarveOrganicInteriorAroundPath();
        }

        /// <summary>
        /// Makes the room shape more organic by turning cells that are far from the path into walls.
        /// Keeps everything inside the fixed 10×10 footprint so corridors and doors still line up.
        /// </summary>
        private void CarveOrganicInteriorAroundPath()
        {
            const int maxDistanceFromPath = 2; // tweak to make rooms tighter/looser

            // 1. BFS from all path cells to compute Manhattan distance to nearest path.
            int[,] dist = new int[RoomSize, RoomSize];
            for (int x = 0; x < RoomSize; x++)
                for (int y = 0; y < RoomSize; y++)
                    dist[x, y] = int.MaxValue;

            var queue = new Queue<Vector2Int>();
            for (int x = 0; x < RoomSize; x++)
            {
                for (int y = 0; y < RoomSize; y++)
                {
                    if (_collapsedTilemap[x, y] == TileType.Path)
                    {
                        dist[x, y] = 0;
                        queue.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            var offsets = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                int d = dist[p.x, p.y];
                if (d >= maxDistanceFromPath) continue;
                foreach (var off in offsets)
                {
                    int nx = p.x + off.x;
                    int ny = p.y + off.y;
                    if (nx <= 0 || nx >= RoomSize - 1 || ny <= 0 || ny >= RoomSize - 1) continue; // keep perimeter walls
                    if (dist[nx, ny] <= d + 1) continue;
                    dist[nx, ny] = d + 1;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }

            // 2. Any interior cell farther than the threshold from the path becomes wall.
            for (int x = 1; x < RoomSize - 1; x++)
            {
                for (int y = 1; y < RoomSize - 1; y++)
                {
                    if (_collapsedTilemap[x, y] == TileType.Path) continue;
                    if (_isPrePlaced[x, y]) continue; // already a wall/door
                    if (dist[x, y] == int.MaxValue || dist[x, y] > maxDistanceFromPath)
                        PlaceTile(x, y, TileType.Wall);
                }
            }
        }

        private void CarvePersistentPathThroughRoom()
        {
            var doorEntries = new List<Vector2Int>(4);

            // Convert door "start along wall" into a single entry point inside the room.
            // With PathWidth=1, doorStart is the exact coordinate.
            if (_northDoorStart >= 0)
            {
                int x = ClampDoorStart(_northDoorStart);
                doorEntries.Add(new Vector2Int(x, RoomSize - 2)); // just inside top wall
            }
            if (_southDoorStart >= 0)
            {
                int x = ClampDoorStart(_southDoorStart);
                doorEntries.Add(new Vector2Int(x, 1)); // just inside bottom wall
            }
            if (_eastDoorStart >= 0)
            {
                int y = ClampDoorStart(_eastDoorStart);
                doorEntries.Add(new Vector2Int(RoomSize - 2, y)); // just inside right wall
            }
            if (_westDoorStart >= 0)
            {
                int y = ClampDoorStart(_westDoorStart);
                doorEntries.Add(new Vector2Int(1, y)); // just inside left wall
            }

            if (doorEntries.Count == 0) return;

            // Hub point inside the room. This guarantees connectivity even for 3-way rooms.
            var hub = new Vector2Int(RoomSize / 2, RoomSize / 2);
            hub = ClampToInterior(hub);

            // Carve door → hub for each entry (L-shaped Manhattan path).
            for (int i = 0; i < doorEntries.Count; i++)
            {
                var p = ClampToInterior(doorEntries[i]);
                CarveManhattanPath(p, hub);
            }
        }

        private static Vector2Int ClampToInterior(Vector2Int p)
        {
            return new Vector2Int(
                Mathf.Clamp(p.x, 1, RoomSize - 2),
                Mathf.Clamp(p.y, 1, RoomSize - 2));
        }

        private void CarveManhattanPath(Vector2Int a, Vector2Int b)
        {
            // Horizontal then vertical.
            int xStep = a.x <= b.x ? 1 : -1;
            for (int x = a.x; x != b.x; x += xStep)
                PlaceTile(x, a.y, TileType.Path);
            PlaceTile(b.x, a.y, TileType.Path);

            int yStep = a.y <= b.y ? 1 : -1;
            for (int y = a.y; y != b.y; y += yStep)
                PlaceTile(b.x, y, TileType.Path);
            PlaceTile(b.x, b.y, TileType.Path);
        }

        private void PropagateAllPrePlacedConstraints()
        {
            // Ensure walls/doors/carved paths constrain the remaining superposition.
            // (Without this, pre-placed tiles would not influence neighbors because propagation
            // is only triggered by collapsed non-preplaced cells.)
            for (int x = 0; x < RoomSize; x++)
            {
                for (int y = 0; y < RoomSize; y++)
                {
                    if (!_isPrePlaced[x, y]) continue;
                    WFCCore.Propagate(
                        new Vector2Int(x, y),
                        _tilePossibilities,
                        _isPrePlaced,
                        _adjacencyRules,
                        IsInBounds);
                }
            }
        }

        private static int ClampDoorStart(int start)
        {
            if (start < 0) return -1;
            int min = RoomTreeLayout.DoorStartMin;
            int max = RoomTreeLayout.DoorStartMax;
            if (max < min) return Mathf.Clamp(start, 0, RoomSize - PathWidth);
            return Mathf.Clamp(start, min, max);
        }

        private void PlaceDoorNorth(int start)
        {
            start = ClampDoorStart(start);
            if (start < 0) return;
            for (int i = 0; i < PathWidth; i++)
                PlaceTile(start + i, RoomSize - 1, TileType.Path);
        }

        private void PlaceDoorSouth(int start)
        {
            start = ClampDoorStart(start);
            if (start < 0) return;
            for (int i = 0; i < PathWidth; i++)
                PlaceTile(start + i, 0, TileType.Path);
        }

        private void PlaceDoorEast(int start)
        {
            start = ClampDoorStart(start);
            if (start < 0) return;
            for (int i = 0; i < PathWidth; i++)
                PlaceTile(RoomSize - 1, start + i, TileType.Path);
        }

        private void PlaceDoorWest(int start)
        {
            start = ClampDoorStart(start);
            if (start < 0) return;
            for (int i = 0; i < PathWidth; i++)
                PlaceTile(0, start + i, TileType.Path);
        }

        private void BuildUncollapsedCellsList()
        {
            _uncollapsedCells = new List<Vector2Int>();
            for (int x = 0; x < RoomSize; x++)
                for (int y = 0; y < RoomSize; y++)
                    if (!_isPrePlaced[x, y])
                        _uncollapsedCells.Add(new Vector2Int(x, y));
        }

        private bool IsFullyCollapsed()
        {
            for (int x = 0; x < RoomSize; x++)
                for (int y = 0; y < RoomSize; y++)
                    if (!_isPrePlaced[x, y] && _tilePossibilities[x, y].Count > 1)
                        return false;
            return true;
        }

        private void PlaceTile(int x, int y, TileType type)
        {
            _tilePossibilities[x, y].Clear();
            _tilePossibilities[x, y].Add(type);
            _collapsedTilemap[x, y] = type;
            _isPrePlaced[x, y] = true;
        }

        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < RoomSize && pos.y >= 0 && pos.y < RoomSize;
        }

        public static int GetRoomSize() => RoomSize;
    }
}
