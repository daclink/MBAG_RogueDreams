using System.Collections.Generic;
using UnityEngine;
using MBAG;

namespace WFC
{
    public enum RoomTreeRoomType
    {
        Normal,
        Start,
        End,
        Item
    }

    /// <summary>
    /// Node in the room tree. Holds grid position, depth, room type, tile data, and neighbor links.
    /// </summary>
    public class RoomTreeNode
    {
        public Vector2Int GridPosition { get; set; }
        public int Depth { get; set; }
        public RoomTreeRoomType RoomType { get; set; }
        public TileType[,] TileData { get; set; }
        public int BiomeIndex { get; set; }

        /// <summary>Neighbors in the spanning tree (max 4). Index: 0=North, 1=East, 2=South, 3=West.</summary>
        public RoomTreeNode[] Neighbors { get; } = new RoomTreeNode[4];

        /// <summary>
        /// Door start offsets for each side (0=N,1=E,2=S,3=W). Value is the start index along the wall:
        /// - For N/S: x start (local to room)
        /// - For E/W: y start (local to room)
        /// - -1 means no door on that side
        /// </summary>
        public int[] DoorStarts { get; } = { -1, -1, -1, -1 };

        public List<RoomTreeNode> NeighborsList
        {
            get
            {
                var list = new List<RoomTreeNode>(4);
                for (int i = 0; i < 4; i++)
                    if (Neighbors[i] != null) list.Add(Neighbors[i]);
                return list;
            }
        }

        public bool HasNorth => Neighbors[0] != null;
        public bool HasEast => Neighbors[1] != null;
        public bool HasSouth => Neighbors[2] != null;
        public bool HasWest => Neighbors[3] != null;

        /// <summary>World position (origin of room). Spacing = RoomTreeLayout.Spacing.</summary>
        public Vector2Int WorldPosition => GridPosition * RoomTreeLayout.Spacing;

        public RoomTreeNode(Vector2Int gridPos)
        {
            GridPosition = gridPos;
        }
    }
}
