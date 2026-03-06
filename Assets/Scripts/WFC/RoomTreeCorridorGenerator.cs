using System.Collections.Generic;
using UnityEngine;
using MBAG;

namespace WFC
{
    /// <summary>
    /// Fills a tilemap with corridor paths between rooms. Corridors stop at room walls.
    /// Single global corridor tilemap. PathWidth=2, spacing=RoomTreeLayout.Spacing.
    /// </summary>
    public static class RoomTreeCorridorGenerator
    {
        /// <summary>
        /// Fill corridors in tileData. tileData should be sized to cover the full dungeon.
        /// origin is the world-space offset (min x,y of the grid).
        /// </summary>
        public static void FillCorridors(
            TileType[,] tileData,
            Vector2Int origin,
            Dictionary<Vector2Int, RoomTreeNode> nodes,
            int gridSize)
        {
            var seen = new HashSet<(Vector2Int, Vector2Int)>();

            foreach (var kvp in nodes)
            {
                RoomTreeNode room = kvp.Value;
                Vector2Int worldA = room.WorldPosition;

                for (int i = 0; i < 4; i++)
                {
                    var neighbor = room.Neighbors[i];
                    if (neighbor == null) continue;

                    Vector2Int worldB = neighbor.WorldPosition;
                    var edge = (room.GridPosition, neighbor.GridPosition);
                    var edgeRev = (neighbor.GridPosition, room.GridPosition);
                    if (seen.Contains(edge) || seen.Contains(edgeRev)) continue;
                    seen.Add(edge);

                    int doorStartA = room.DoorStarts != null ? room.DoorStarts[i] : -1;
                    if (doorStartA < 0) doorStartA = (RoomTreeLayout.RoomSize - RoomTreeLayout.PathWidth) / 2;
                    FillCorridorBetween(tileData, origin, worldA, worldB, i, doorStartA);
                }
            }
        }

        private static void FillCorridorBetween(
            TileType[,] tileData,
            Vector2Int origin,
            Vector2Int worldA,
            Vector2Int worldB,
            int directionFromA,
            int doorStart)
        {
            int dataOriginX = -origin.x;
            int dataOriginY = -origin.y;

            if (directionFromA == 0) // North: A's top -> B's bottom
            {
                int xStart = worldA.x + doorStart;
                int yStart = worldA.y + RoomTreeLayout.RoomSize;
                int yEnd = worldB.y;
                for (int x = 0; x < RoomTreeLayout.PathWidth; x++)
                    for (int y = yStart; y < yEnd; y++)
                        SetTile(tileData, dataOriginX + xStart + x, dataOriginY + y, TileType.Path);
            }
            else if (directionFromA == 1) // East: A's right -> B's left
            {
                int xStart = worldA.x + RoomTreeLayout.RoomSize;
                int xEnd = worldB.x;
                int yStart = worldA.y + doorStart;
                for (int x = xStart; x < xEnd; x++)
                    for (int y = 0; y < RoomTreeLayout.PathWidth; y++)
                        SetTile(tileData, dataOriginX + x, dataOriginY + yStart + y, TileType.Path);
            }
            else if (directionFromA == 2) // South: A's bottom -> B's top
            {
                int xStart = worldA.x + doorStart;
                int yStart = worldB.y + RoomTreeLayout.RoomSize;
                int yEnd = worldA.y;
                for (int x = 0; x < RoomTreeLayout.PathWidth; x++)
                    for (int y = yStart; y < yEnd; y++)
                        SetTile(tileData, dataOriginX + xStart + x, dataOriginY + y, TileType.Path);
            }
            else // West: A's left -> B's right
            {
                int xStart = worldB.x + RoomTreeLayout.RoomSize;
                int xEnd = worldA.x;
                int yStart = worldA.y + doorStart;
                for (int x = xStart; x < xEnd; x++)
                    for (int y = 0; y < RoomTreeLayout.PathWidth; y++)
                        SetTile(tileData, dataOriginX + x, dataOriginY + yStart + y, TileType.Path);
            }
        }

        private static void SetTile(TileType[,] data, int x, int y, TileType type)
        {
            int w = data.GetLength(0);
            int h = data.GetLength(1);
            if (x >= 0 && x < w && y >= 0 && y < h)
                data[x, y] = type;
        }

        /// <summary>Compute full dungeon bounds. Grid 0,0 to (gridSize-1, gridSize-1), rooms 10×10, spacing RoomTreeLayout.Spacing.</summary>
        public static BoundsInt GetDungeonBounds(int gridSize)
        {
            int totalW = (gridSize - 1) * RoomTreeLayout.Spacing + RoomTreeLayout.RoomSize;
            int totalH = (gridSize - 1) * RoomTreeLayout.Spacing + RoomTreeLayout.RoomSize;
            return new BoundsInt(0, 0, 0, totalW, totalH, 1);
        }
    }
}
