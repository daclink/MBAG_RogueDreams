using Unity.VisualScripting;
using System.Collections;
using UnityEngine;

namespace WFC
{
    // This will store all of the tile types to be marked on the first layer grid
    public enum tileType
    {
        none,
        grass,
        dirt
    }

    // This will keep track of the rooms that need to exist on each level no matter what
    // Each of these rooms will be given a location on a grid to then generate connections to each
    // for the player to traverse
    public enum presetRooms
    {
        startRoom,
        endRoom,
        itemRoom
    }
    
    public class wfc
    {
        // Need to decide on max grid size for each map
        private uint gridWidth = 128;
        private uint gridHeight = 128;
        private presetRooms startRoom;
        private presetRooms endRoom;
        private presetRooms itemRoom;

        private uint maxRoomWidth = 10;
        private uint maxRoomHeight = 10;
        private uint minRoomWidth = 5;
        private uint minRoomHeight = 5;
        

        //Assign grid coordinates to the presetRooms
        // Have rules to make sure the start and end room are not adjacent
        // Wherever the rooms are assigned, ensure there is padding around the coordinates of the chosen width/2 and height/2
        public int AssignStartRoomCoords()
        {
            
            
            
            return -1;
        }

    }
}