using System;
using System.Collections.Generic;
using UnityEngine;
using MBAG;

/**
 * MAYBE:
 * Update to use one tilemap that represents one room. All the room data is stored in a hashmap with a
 * key for room and values for connecting rooms
 * When the player is in a room, the adjacent room data is loaded.
 * When the player walks to the next room, a black screen can come up which wont last long but will allow the correct room to
 * be passed to dual grid and generated.
 */


namespace WFC
{
    public class WFCTilemap
    {
        //Grid values
        private int roomWidth;
        private int roomHeight;
        private int roomSize = 10;
        private int wallThickness = 2;

        private int tilemapWidth;
        private int tilemapHeight;

        //Serializable settings
        private int pathWidth = 2;

        //WFC
        private HashSet<TileType>[,] tilePossibilities;
        private TileType[,] collapsedTilemap;
        private bool[,] isPrePlaced;
        
        //Adjacency rules
        private Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>> adjacencyRules;
        
        //Room layout data
        private int[,] roomLayout;
        private List<Vector2Int> roomPositions;
        private HashSet<Vector2Int> roomPositionsSet;  // O(1) lookup for GetRoomConnections
        private RoomLayoutGenerator layoutGenerator;

        private List<Vector2Int> _uncollapsedCells;  // Shrinks each collapse; scan only these instead of full grid

        //constructor
        public WFCTilemap(int[,] roomLayout, List<Vector2Int> roomPositions, RoomLayoutGenerator layoutGen, int pathWidth)
        {
            this.roomLayout = roomLayout;
            this.roomPositions = roomPositions;
            this.roomPositionsSet = new HashSet<Vector2Int>(roomPositions);
            this.layoutGenerator = layoutGen;
            this.pathWidth = pathWidth;

            roomWidth = roomLayout.GetLength(0);
            roomHeight = roomLayout.GetLength(1);

            // Calculate final tilemap size
            // Formula: (numRooms * roomSize) + ((numRooms + 1) * wallThickness)
            tilemapWidth = (roomWidth * roomSize) + ((roomWidth + 1) * wallThickness);
            tilemapHeight = (roomHeight * roomSize) + ((roomHeight + 1) * wallThickness);
            
            adjacencyRules = WFCCore.GetAdjacencyRules();
        }
        
        // --------------------------  DRIVER METHOD  ----------------------------
        
        public TileType[,] Generate()
        {
            // Uncomment method call below to print out the map layout in the console
            // DebugRoomLayout(); 
            
            // Initialize arrays
            InitializeSuperposition();

            // Pre-place walls and paths
            PrePlaceStaticTiles();

            // Build list of uncollapsed cells (scan only these for lowest entropy; shrinks each iteration)
            BuildUncollapsedCellsList();

            // Run WFC on remaining tiles
            int iterations = 0;
            int maxIterations = tilemapWidth * tilemapHeight * 3;

            while (!IsFullyCollapsed() && iterations < maxIterations)
            {
                Vector2Int? cell = FindLowestEntropyCell();

                if (cell == null)
                    break;

                WFCCore.CollapseCell(cell.Value, tilePossibilities, collapsedTilemap, tilemapWidth, tilemapHeight, adjacencyRules);
                WFCCore.Propagate(cell.Value, tilePossibilities, isPrePlaced, adjacencyRules, IsInBounds);
                _uncollapsedCells.Remove(cell.Value);
                iterations++;
            }

            WFCCore.FinalizeTilemap(tilePossibilities, collapsedTilemap, isPrePlaced, tilemapWidth, tilemapHeight);
            return collapsedTilemap;
        }
        
        // ----------------------------  WFC MAIN METHODS  --------------------------
        
        // --------------------------- WFC HELPER METHODS  ---------------------------------

        /**
         * Gets the room connections for a specific room and places the connections in a bool[]
         */
        private bool[] GetRoomConnections(Vector2Int roomPos)
        {
            bool[] connections = new bool[4]; // [North, East, South, West]
            connections[0] = roomPositionsSet.Contains(roomPos + Vector2Int.up);
            connections[1] = roomPositionsSet.Contains(roomPos + Vector2Int.right);
            connections[2] = roomPositionsSet.Contains(roomPos + Vector2Int.down);
            connections[3] = roomPositionsSet.Contains(roomPos + Vector2Int.left);
            return connections;
        }

        private void BuildUncollapsedCellsList()
        {
            _uncollapsedCells = new List<Vector2Int>(tilemapWidth * tilemapHeight);
            for (int x = 0; x < tilemapWidth; x++)
            {
                for (int y = 0; y < tilemapHeight; y++)
                {
                    if (!isPrePlaced[x, y])
                        _uncollapsedCells.Add(new Vector2Int(x, y));
                }
            }
        }

/**
         * Skips pre-placed tiles, finding the lowest entropy cell. Scans only uncollapsed cells (shrinks each iteration).
         */
        private Vector2Int? FindLowestEntropyCell()
        {
            return WFCCore.FindLowestEntropyCell(tilePossibilities, isPrePlaced, _uncollapsedCells);
        }

        /**
         * Checks if the tilemap is fully collapsed by ensuring that the tile possiblities per tile is not more than 1
         */
        private bool IsFullyCollapsed()
        {
            for (int x = 0; x < tilemapWidth; x++)
            {
                for (int y = 0; y < tilemapHeight; y++)
                {
                    if (!isPrePlaced[x, y] && tilePossibilities[x, y].Count > 1)
                        return false;
                }
            }
            return true;
        }        
        
        
        
        // ---------------------------  INITIALIZING METHODS  ------------------------------

        /**
         * Initializes the data sets
         */
        private void InitializeSuperposition()
        {
            tilePossibilities = new HashSet<TileType>[tilemapWidth, tilemapHeight];
            collapsedTilemap = new TileType[tilemapWidth, tilemapHeight];
            isPrePlaced = new bool[tilemapWidth, tilemapHeight];

            for (int x = 0; x < tilemapWidth; x++)
            {
                for (int y = 0; y < tilemapHeight; y++)
                {
                    // Initially, all tiles can be any type except Empty and Wall
                    // (Walls will be pre-placed)
                    tilePossibilities[x, y] = new HashSet<TileType>
                    {
                        TileType.Grass,
                        TileType.Dirt,
                        TileType.Water,
                        TileType.Path
                    };

                    collapsedTilemap[x, y] = TileType.Empty;
                    isPrePlaced[x, y] = false;
                }
            }
        }

        /**
         * Preplaces the static tiles which are the wall, paths, and fills empty rooms
         */
        private void PrePlaceStaticTiles()
        {
            // 1. Fill all non-existent rooms with Empty tiles FIRST
            FillEmptyRooms();

            // 2. Place outer boundary walls
            PlaceOuterWalls();

            // 3. Place walls between rooms
            PlaceRoomWalls();

            // 4. Constrain special rooms (no water allowed)
            ConstrainSpecialRooms();

            // 5. Place paths connecting rooms
            PlacePaths();
        }
        
        /**
        * Fills the empty rooms with empty tiles
        * May not need
        */
        private void FillEmptyRooms()
        {
            for (int roomGridX = 0; roomGridX < roomWidth; roomGridX++)
            {
                for (int roomGridY = 0; roomGridY < roomHeight; roomGridY++)
                {
                    // Check if this room doesn't exist (value is -1)
                    if (roomLayout[roomGridX, roomGridY] < 0)
                    {
                        FillRoomWithEmptyTiles(roomGridX, roomGridY);
                    }
                }
            }
        }

        /**
         * Places the static outerwall tiles
         */
        private void PlaceOuterWalls()
        {
            // Top and bottom walls (2 tiles thick)
            for (int x = 0; x < tilemapWidth; x++)
            {
                for (int t = 0; t < wallThickness; t++)
                {
                    // Bottom wall
                    PlaceTile(x, t, TileType.Wall);
                    
                    // Top wall
                    PlaceTile(x, tilemapHeight - 1 - t, TileType.Wall);
                }
            }

            // Left and right walls (2 tiles thick)
            for (int y = 0; y < tilemapHeight; y++)
            {
                for (int t = 0; t < wallThickness; t++)
                {
                    // Left wall
                    PlaceTile(t, y, TileType.Wall);
                    
                    // Right wall
                    PlaceTile(tilemapWidth - 1 - t, y, TileType.Wall);
                }
            }
        }
        
        /**
         * Places the inner map walls, creating the grid like structure of rooms
         */
        private void PlaceRoomWalls()
        {
            // Place walls between rooms (horizontal and vertical corridors of walls)
            
            // Vertical walls (between columns of rooms)
            for (int roomX = 0; roomX < roomWidth - 1; roomX++)
            {
                int wallStartX = wallThickness + (roomX + 1) * roomSize + roomX * wallThickness;
                
                for (int y = 0; y < tilemapHeight; y++)
                {
                    for (int t = 0; t < wallThickness; t++)
                    {
                        PlaceTile(wallStartX + t, y, TileType.Wall);
                    }
                }
            }

            // Horizontal walls (between rows of rooms)
            for (int roomY = 0; roomY < roomHeight - 1; roomY++)
            {
                int wallStartY = wallThickness + (roomY + 1) * roomSize + roomY * wallThickness;
                
                for (int x = 0; x < tilemapWidth; x++)
                {
                    for (int t = 0; t < wallThickness; t++)
                    {
                        PlaceTile(x, wallStartY + t, TileType.Wall);
                    }
                }
            }
        }

        /**
         * This method gets the special rooms and removes the water tiles from them by calling the removeWaterFromRoom method
         */
        private void ConstrainSpecialRooms()
        {
            Vector2Int startRoom = layoutGenerator.GetStartRoomPosition();
            Vector2Int endRoom = layoutGenerator.GetEndRoomPosition();
            Vector2Int itemRoom = layoutGenerator.GetItemRoomPosition();

            RemoveWaterFromRoom(startRoom);
            RemoveWaterFromRoom(endRoom);
            RemoveWaterFromRoom(itemRoom);
        }
        
        /**
        * Places paths in each room
        */
        private void PlacePaths()
        {
            // For each room, check connections and place door paths
            foreach (Vector2Int roomPos in roomPositions)
            {
                bool[] connections = GetRoomConnections(roomPos);
                PlaceRoomDoors(roomPos, connections);
            }
        }
        
        /**
         * Places the door openings in a room based on the connections array
         */
        private void PlaceRoomDoors(Vector2Int roomGridPos, bool[] connections)
        {
            // Convert room grid position to tilemap position (accounting for walls)
            int roomTileX = wallThickness + roomGridPos.x * (roomSize + wallThickness);
            int roomTileY = wallThickness + roomGridPos.y * (roomSize + wallThickness);

            int doorWidth = pathWidth; // Door width matches path width
            int doorCenter = roomSize / 2;

            // North door
            if (connections[0])
            {
                int doorX = roomTileX + doorCenter - doorWidth / 2;
                int doorY = roomTileY + roomSize; // At top of room
                
                for (int i = 0; i < doorWidth; i++)
                {
                    for (int t = 0; t < wallThickness; t++)
                    {
                        PlaceTile(doorX + i, doorY + t, TileType.Path);
                    }
                }
            }

            // East door
            if (connections[1])
            {
                int doorX = roomTileX + roomSize; // At right of room
                int doorY = roomTileY + doorCenter - doorWidth / 2;
                
                for (int i = 0; i < doorWidth; i++)
                {
                    for (int t = 0; t < wallThickness; t++)
                    {
                        PlaceTile(doorX + t, doorY + i, TileType.Path);
                    }
                }
            }

            // South door
            if (connections[2])
            {
                int doorX = roomTileX + doorCenter - doorWidth / 2;
                int doorY = roomTileY - wallThickness; // At bottom of room (in wall below)
                
                for (int i = 0; i < doorWidth; i++)
                {
                    for (int t = 0; t < wallThickness; t++)
                    {
                        PlaceTile(doorX + i, doorY + t, TileType.Path);
                    }
                }
            }

            // West door
            if (connections[3])
            {
                int doorX = roomTileX - wallThickness; // At left of room (in wall to left)
                int doorY = roomTileY + doorCenter - doorWidth / 2;
                
                for (int i = 0; i < doorWidth; i++)
                {
                    for (int t = 0; t < wallThickness; t++)
                    {
                        PlaceTile(doorX + t, doorY + i, TileType.Path);
                    }
                }
            }

            // Place path through center of room if it has connections
            int connectionCount = 0;
            for (int c = 0; c < 4; c++) if (connections[c]) connectionCount++;
            if (connectionCount > 0)
            {
                PlaceRoomCenterPaths(roomTileX, roomTileY, connections);
            }
        }

        /**
         * Places the center of the path for the room based on connections of N/S or E/W
         */
        private void PlaceRoomCenterPaths(int roomTileX, int roomTileY, bool[] connections)
        {
            int pathCenter = roomSize / 2;

            // Horizontal path through room
            if (connections[1] || connections[3]) // East or West
            {
                for (int x = 0; x < roomSize; x++)
                {
                    for (int offset = -pathWidth / 2; offset < pathWidth / 2; offset++)
                    {
                        int tileX = roomTileX + x;
                        int tileY = roomTileY + pathCenter + offset;
                        
                        if (IsInBounds(new Vector2Int(tileX, tileY)))
                        {
                            PlaceTile(tileX, tileY, TileType.Path);
                        }
                    }
                }
            }

            // Vertical path through room
            if (connections[0] || connections[2]) // North or South
            {
                for (int y = 0; y < roomSize; y++)
                {
                    for (int offset = -pathWidth / 2; offset < pathWidth / 2; offset++)
                    {
                        int tileX = roomTileX + pathCenter + offset;
                        int tileY = roomTileY + y;
                        
                        if (IsInBounds(new Vector2Int(tileX, tileY)))
                        {
                            // Only overwrite if not already path (to avoid overwriting intersections)
                            if (collapsedTilemap[tileX, tileY] != TileType.Path)
                            {
                                PlaceTile(tileX, tileY, TileType.Path);
                            }
                        }
                    }
                }
            }
        }
        
        /**
        * Removes water from the tilePossibilities list
        */
        private void RemoveWaterFromRoom(Vector2Int roomGridPos)
        {
            // Convert room grid position to tilemap position
            int roomTileX = wallThickness + roomGridPos.x * (roomSize + wallThickness);
            int roomTileY = wallThickness + roomGridPos.y * (roomSize + wallThickness);

            // Remove water from all tiles in this room
            for (int x = 0; x < roomSize; x++)
            {
                for (int y = 0; y < roomSize; y++)
                {
                    int tileX = roomTileX + x;
                    int tileY = roomTileY + y;

                    if (IsInBounds(new Vector2Int(tileX, tileY)))
                    {
                        tilePossibilities[tileX, tileY].Remove(TileType.Water);
                    }
                }
            }
        }

        /**
         * Fills the interior of the room with empty tiles
         * Would like to constrain this into the last method if possible
         */
        private void FillRoomWithEmptyTiles(int roomGridX, int roomGridY)
        {
            // Calculate room position in tilemap coordinates
            int roomTileX = wallThickness + roomGridX * (roomSize + wallThickness);
            int roomTileY = wallThickness + roomGridY * (roomSize + wallThickness);

            // Fill just the room interior (not the walls around it)
            for (int x = 0; x < roomSize; x++)
            {
                for (int y = 0; y < roomSize; y++)
                {
                    int tileX = roomTileX + x;
                    int tileY = roomTileY + y;

                    if (IsInBounds(new Vector2Int(tileX, tileY)))
                    {
                        PlaceTile(tileX, tileY, TileType.Empty);
                    }
                }
            }
        }
        
        
        // -----------------------------  HELPER METHODS  ----------------------------------
        
        /**
        * Places the specified tile at the specified position
        */
        private void PlaceTile(int x, int y, TileType type)
        {
            if (!IsInBounds(new Vector2Int(x, y)))
                return;

            tilePossibilities[x, y].Clear();
            tilePossibilities[x, y].Add(type);
            collapsedTilemap[x, y] = type;
            isPrePlaced[x, y] = true;
        }
        
        /**
         * Checks that a position is in bounds of the tilemap
         */
        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < tilemapWidth &&
                   pos.y >= 0 && pos.y < tilemapHeight;
        }

        /**
         * Returns the size of the tilemap
         */
        public Vector2Int GetTilemapSize()
        {
            return new Vector2Int(tilemapWidth, tilemapHeight);
        }
        
        /**
         * Debugging method to print the room layout
         */
        private void DebugRoomLayout()
        {
            Debug.Log("=== Room Layout Debug ===");
            for (int y = roomHeight - 1; y >= 0; y--)
            {
                string row = "";
                for (int x = 0; x < roomWidth; x++)
                {
                    int value = roomLayout[x, y];
                    row += value < 0 ? "." : "■";
                    row += " ";
                }
                Debug.Log($"Row {y}: {row}");
            }
        }
    }
}