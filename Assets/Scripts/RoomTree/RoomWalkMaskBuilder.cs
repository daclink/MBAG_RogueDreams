using MBAG;
using UnityEngine;

namespace WFC
{
    /// <summary>
    /// Builds a 64-bit walk mask for the 8×8 interior of a 10×10 room <see cref="RoomTreeLayout.RoomSize"/>.
    /// Local tile indices (1..8, 1..8) map to bit index <c>(ly - 1) * 8 + (lx - 1)</c> (BbGrid order).
    /// </summary>
    public static class RoomWalkMaskBuilder
    {
        /// <summary>Grass, dirt, and carved path are walkable; wall, water, empty are not.</summary>
        public static bool IsWalkableTile(TileType t)
        {
            return t == TileType.Grass || t == TileType.Dirt || t == TileType.Path;
        }

        /// <param name="tileData10">Room tile data [0..9, 0..9].</param>
        public static ulong BuildInteriorWalkMask8x8(TileType[,] tileData10)
        {
            if (tileData10 == null)
                throw new System.ArgumentNullException(nameof(tileData10));
            int w = tileData10.GetLength(0);
            int h = tileData10.GetLength(1);
            if (w < RoomTreeLayout.RoomSize || h < RoomTreeLayout.RoomSize)
                throw new System.ArgumentException("Expected at least 10×10 tile data.", nameof(tileData10));

            ulong mask = 0UL;
            for (int bbY = 0; bbY < 8; bbY++)
            {
                for (int bbX = 0; bbX < 8; bbX++)
                {
                    int lx = bbX + 1;
                    int ly = bbY + 1;
                    if (IsWalkableTile(tileData10[lx, ly]))
                        mask |= 1UL << (bbY * 8 + bbX);
                }
            }

            return mask;
        }

        /// <summary>World cell → interior bit index 0..63, or -1 if outside this room's 8×8 interior.</summary>
        public static bool TryWorldCellToInteriorBitIndex(Vector3Int roomOriginCell, Vector3Int worldCell, out int bitIndex)
        {
            int lx = worldCell.x - roomOriginCell.x;
            int ly = worldCell.y - roomOriginCell.y;
            if (lx < 1 || lx > 8 || ly < 1 || ly > 8)
            {
                bitIndex = -1;
                return false;
            }

            int bbX = lx - 1;
            int bbY = ly - 1;
            bitIndex = bbY * 8 + bbX;
            return true;
        }

        /// <summary>Interior bit index → world cell (center layer z=0).</summary>
        public static Vector3Int InteriorBitIndexToWorldCell(Vector3Int roomOriginCell, int bitIndex)
        {
            int bbX = bitIndex % 8;
            int bbY = bitIndex / 8;
            return new Vector3Int(roomOriginCell.x + bbX + 1, roomOriginCell.y + bbY + 1, 0);
        }

        public static Vector3Int RoomOriginCell(Vector2Int worldPosition)
        {
            return new Vector3Int(worldPosition.x, worldPosition.y, 0);
        }
    }
}
