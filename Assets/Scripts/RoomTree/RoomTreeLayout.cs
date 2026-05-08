using UnityEngine;

namespace WFC
{
    /// <summary>
    /// Shared layout constants for the RoomTree dungeon.
    /// Keep these in one place so WorldPosition, corridor carving, and room WFC agree.
    /// </summary>
    public static class RoomTreeLayout
    {
        public const int RoomSize = 10;
        public const int PathWidth = 1;

        /// <summary>
        /// Empty space between adjacent room walls. Set to the minimum needed for a corridor segment.
        /// </summary>
        public const int CorridorGap = PathWidth;

        /// <summary>
        /// Grid-to-world step for room origins (bottom-left of each 10×10 room).
        /// </summary>
        public const int Spacing = RoomSize + CorridorGap;

        /// <summary>
        /// Valid door start range (keeps at least 1 wall tile at each corner).
        /// Door occupies [start, start+PathWidth-1] along the wall.
        /// </summary>
        public static int DoorStartMin => 1;
        public static int DoorStartMax => RoomSize - PathWidth - 1;

        public static int RandomDoorStart()
        {
            int min = DoorStartMin;
            int max = DoorStartMax;
            if (max < min) return 0;
            return Random.Range(min, max + 1);
        }
    }
}

