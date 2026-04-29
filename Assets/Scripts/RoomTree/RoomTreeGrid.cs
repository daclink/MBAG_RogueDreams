using System.Collections.Generic;
using UnityEngine;

namespace WFC
{
    /// <summary>
    /// Shared room-tree grid queries: map a world <see cref="Grid"/> cell to the
    /// <see cref="RoomTreeNode"/> whose 10×10 <see cref="RoomTreeLayout.RoomSize"/> bounds contain it.
    /// </summary>
    public static class RoomTreeGrid
    {
        /// <param name="nodes">All rooms in the current dungeon (e.g. <see cref="RoomTreeDungeonGenerator.Nodes"/>).</param>
        /// <param name="worldCell">Cell from <c>grid.WorldToCell</c> (world-space tile coordinates).</param>
        /// <returns>The room that owns that cell, or null if the cell is not inside any room.</returns>
        public static RoomTreeNode FindRoomContainingCell(
            IReadOnlyDictionary<Vector2Int, RoomTreeNode> nodes,
            Vector3Int worldCell)
        {
            if (nodes == null) return null;
            int roomSize = RoomTreeLayout.RoomSize;
            foreach (RoomTreeNode node in nodes.Values)
            {
                if (node == null) continue;
                Vector2Int wp = node.WorldPosition;
                if (worldCell.x >= wp.x && worldCell.x < wp.x + roomSize &&
                    worldCell.y >= wp.y && worldCell.y < wp.y + roomSize)
                    return node;
            }

            return null;
        }

        public static RoomTreeNode FindRoomContainingCell(RoomTreeDungeonGenerator generator, Vector3Int worldCell)
        {
            if (generator == null) return null;
            return FindRoomContainingCell(generator.Nodes, worldCell);
        }
    }
}
