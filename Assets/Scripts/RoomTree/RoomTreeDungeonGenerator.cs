using System.Collections.Generic;
using UnityEngine;
using MBAG;

namespace WFC
{
    /// <summary>
    /// Orchestrates room-tree dungeon generation: spanning tree, room WFC, special room assignment, corridors.
    /// </summary>
    public class RoomTreeDungeonGenerator
    {
        private readonly int _gridSize;

        public Dictionary<Vector2Int, RoomTreeNode> Nodes { get; private set; }
        public RoomTreeNode Root { get; private set; }
        public TileType[,] CorridorTilemap { get; private set; }
        public BoundsInt DungeonBounds { get; private set; }
        public int GridSize => _gridSize;

        public RoomTreeDungeonGenerator(int gridSize = 4, int randomSeed = 0)
        {
            _gridSize = Mathf.Max(1, gridSize);
            if (randomSeed != 0) Random.InitState(randomSeed);
        }

        public void Generate()
        {
            Nodes = RoomTreeSpanningTreeBuilder.Build(_gridSize, 0);
            Root = FindRoot();
            AssignSpecialRooms();
            AssignDoorStarts();
            GenerateRoomTilemaps();
            GenerateCorridors();
        }

        private RoomTreeNode FindRoot()
        {
            foreach (var n in Nodes.Values)
                if (n.Depth == 0) return n;
            return null;
        }

        private void AssignSpecialRooms()
        {
            if (Root == null) return;

            Root.RoomType = RoomTreeRoomType.Start;

            int maxDepth = 0;
            RoomTreeNode maxDepthNode = null;
            foreach (var n in Nodes.Values)
            {
                if (n.Depth > maxDepth)
                {
                    maxDepth = n.Depth;
                    maxDepthNode = n;
                }
            }
            if (maxDepthNode != null)
                maxDepthNode.RoomType = RoomTreeRoomType.End;

            AssignItemRooms();
        }

        private void AssignItemRooms()
        {
            var depthBuckets = new Dictionary<int, List<RoomTreeNode>>();
            foreach (var n in Nodes.Values)
            {
                if (n.RoomType != RoomTreeRoomType.Normal) continue;
                if (!depthBuckets.TryGetValue(n.Depth, out var list))
                {
                    list = new List<RoomTreeNode>();
                    depthBuckets[n.Depth] = list;
                }
                list.Add(n);
            }

            int itemDepth = 2;
            int itemsPlaced = 0;
            while (itemDepth <= 8 && itemsPlaced < 5)
            {
                if (depthBuckets.TryGetValue(itemDepth, out var candidates) && candidates.Count > 0)
                {
                    candidates.Shuffle();
                    float chance = 0.7f;
                    foreach (var n in candidates)
                    {
                        if (n.RoomType == RoomTreeRoomType.Item) continue;
                        if (Random.value < chance)
                        {
                            n.RoomType = RoomTreeRoomType.Item;
                            itemsPlaced++;
                            chance *= 0.4f;
                            break;
                        }
                        chance *= 0.6f;
                    }
                }
                itemDepth *= 2;
            }
        }

        private void GenerateRoomTilemaps()
        {
            foreach (var node in Nodes.Values)
            {
                bool allowWater = node.RoomType != RoomTreeRoomType.Start &&
                                 node.RoomType != RoomTreeRoomType.End &&
                                 node.RoomType != RoomTreeRoomType.Item;

                var wfc = new RoomWFCTilemap(
                    northDoorStart: node.DoorStarts[0],
                    eastDoorStart: node.DoorStarts[1],
                    southDoorStart: node.DoorStarts[2],
                    westDoorStart: node.DoorStarts[3],
                    allowWater: allowWater);
                node.TileData = wfc.Generate();
            }
        }

        /// <summary>
        /// For each tree edge, choose a random door start along that wall and assign it to both rooms
        /// (A's side and B's opposite side). This guarantees corridors connect cleanly even though
        /// door positions are "anywhere on the wall".
        /// </summary>
        private void AssignDoorStarts()
        {
            if (Nodes == null) return;

            var seen = new HashSet<(Vector2Int, Vector2Int)>();

            foreach (var kvp in Nodes)
            {
                var room = kvp.Value;
                for (int dir = 0; dir < 4; dir++)
                {
                    var neighbor = room.Neighbors[dir];
                    if (neighbor == null) continue;

                    var edge = (room.GridPosition, neighbor.GridPosition);
                    var edgeRev = (neighbor.GridPosition, room.GridPosition);
                    if (seen.Contains(edge) || seen.Contains(edgeRev)) continue;
                    seen.Add(edge);

                    int start = RoomTreeLayout.RandomDoorStart();
                    room.DoorStarts[dir] = start;
                    neighbor.DoorStarts[GetOppositeDir(dir)] = start;
                }
            }
        }

        private static int GetOppositeDir(int dir)
        {
            // 0=N,1=E,2=S,3=W
            return (dir + 2) & 3;
        }

        private void GenerateCorridors()
        {
            DungeonBounds = RoomTreeCorridorGenerator.GetDungeonBounds(_gridSize);
            int w = DungeonBounds.size.x;
            int h = DungeonBounds.size.y;

            CorridorTilemap = new TileType[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    CorridorTilemap[x, y] = TileType.Empty;

            RoomTreeCorridorGenerator.FillCorridors(
                CorridorTilemap,
                new Vector2Int(DungeonBounds.xMin, DungeonBounds.yMin),
                Nodes,
                _gridSize);
        }

        public RoomTreeNode GetNodeAt(Vector2Int gridPos) => Nodes?.TryGetValue(gridPos, out var n) == true ? n : null;
    }

    internal static class ListExtensions
    {
        public static void Shuffle<T>(this List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
