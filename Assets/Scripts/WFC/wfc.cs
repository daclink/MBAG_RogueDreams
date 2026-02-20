using System;
using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This needs to generate both a map grid using one tile per room (generate map shape in a 2d array)
 * This also needs to generate each room, considering all entrances and exits to create paths.
 * If room is a dead end, tiles can be all the same
 */
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
    
    [Serializable]
    public class TileData
    {
        public TileType tileType;
        public Sprite tileSprite;
    }
    
    public class wfc : MonoBehaviour
    {
        //List of each tile and sprite to be used in wfc
        [SerializeField] private List<TileData> tileData;

        //Max grid size will be:
        // Rooms 10x10
        // Room size: 10x10
        // Total for rooms without padding: 100x100
        // With padding for walls 2 tiles thick: 122x122
        
        private uint gridWidth = 122;
        private uint gridHeight = 122;
        private presetRooms startRoom;
        private presetRooms endRoom;
        private presetRooms itemRoom;

        private int roomWidth = 10;
        private int roomHeight = 10;
        private int padding = 2;
        
        

        //Assign grid coordinates to the presetRooms
        // Have rules to make sure the start and end room are not adjacent
        // Wherever the rooms are assigned, ensure there is padding around the coordinates of the chosen width/2 and height/2
        public int AssignStartRoomCoords()
        {
            
            
            
            return -1;
        }

    }
}