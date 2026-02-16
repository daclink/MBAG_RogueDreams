using UnityEngine;

namespace WFC
{
    // All tyle types
    public enum TileType
    {
        Empty = -1,
        Grass = 0,
        Dirt = 1,
        Path = 2
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

    /**
     * This is a simple class/data structure used for rooms
     */
    public class Room
    {
        public RoomType roomType;
        public Vector2Int gridPosition;
        public int width;
        public int height;

        public Room(RoomType roomType, Vector2Int gridPosition, int width, int height)
        {
            this.roomType = roomType;
            this.gridPosition = gridPosition;
            this.width = width;
            this.height = height;
        }

        public Vector2Int GetRoomCenter()
        {
            return new Vector2Int(gridPosition.x + width / 2, gridPosition.y + height / 2);
        }
        
        //TODO: Check for room overlaps??
    }
}