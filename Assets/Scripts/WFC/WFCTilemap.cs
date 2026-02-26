using System;
using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = Unity.Mathematics.Random;
/**
 * Update to use one tilemap that represents one room. All the room data is stored in a hashmap with a
 * key for room and values for connecting rooms
 * When the player is in a room, the adjacent room data is loaded.
 * When the player walks to the next room, a black screen can come up which wont last long but will allow the correct room to
 * be passed to dual grid and generated.
 * 
 * 
 */


/**
 * This needs to generate both a map grid using one tile per room (generate map shape in a 2d array)
 * This also needs to generate each room, considering all entrances and exits to create paths.
 */
namespace WFC
{
    public class WFCTilemap : MonoBehaviour
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
        private RoomLayoutGenerator layoutGenerator;
        
        //constructor
        public WFCTilemap(int[,] roomLayout, List<Vector2Int> roomPositions, RoomLayoutGenerator layoutGen, int pathWidth)
        {
            this.roomLayout = roomLayout;
            this.roomPositions = roomPositions;
            this.layoutGenerator = layoutGen;
            this.pathWidth = pathWidth;

            roomWidth = roomLayout.GetLength(0);
            roomHeight = roomLayout.GetLength(1);

            // Calculate final tilemap size
            // Formula: (numRooms * roomSize) + ((numRooms + 1) * wallThickness)
            tilemapWidth = (roomWidth * roomSize) + ((roomWidth + 1) * wallThickness);
            tilemapHeight = (roomHeight * roomSize) + ((roomHeight + 1) * wallThickness);
            
            InitializeAdjacencyRules();
        }
        
        // public void SetPathWidth(int pathWidth)
        // {
        //     pathWidth = Mathf.Clamp(pathWidth, 1, roomSize - 2); // Ensure it fits in room
        // }

        private void InitializeAdjacencyRules()
        {
            adjacencyRules = new Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>>();

            // Path rules: Can only be adjacent to Dirt and Grass (and itself)
            adjacencyRules[TileType.Path] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } },
                { Direction.South, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } },
                { Direction.East, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } },
                { Direction.West, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } }
            };

            // Dirt rules: Can be adjacent to Path, Water, and Grass
            adjacencyRules[TileType.Dirt] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } },
                { Direction.South, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } },
                { Direction.East, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } },
                { Direction.West, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } }
            };

            // Grass rules: Can only be adjacent to Dirt and Water
            adjacencyRules[TileType.Grass] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } },
                { Direction.South, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } },
                { Direction.East, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } },
                { Direction.West, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } }
            };

            // Water rules: Can be adjacent to Dirt and Grass
            adjacencyRules[TileType.Water] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } },
                { Direction.South, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } },
                { Direction.East, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } },
                { Direction.West, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } }
            };

            // Wall rules: Can be adjacent to anything (walls don't constrain neighbors)
            adjacencyRules[TileType.Wall] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } },
                { Direction.South, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } },
                { Direction.East, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } },
                { Direction.West, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } }
            };

            // Empty rules (for areas outside the map, if any)
            adjacencyRules[TileType.Empty] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Empty } },
                { Direction.South, new HashSet<TileType> { TileType.Empty } },
                { Direction.East, new HashSet<TileType> { TileType.Empty } },
                { Direction.West, new HashSet<TileType> { TileType.Empty } }
            };
        }
        
        public TileType[,] Generate()
        {
            // DebugRoomLayout(); 
            
            // Initialize arrays
            InitializeSuperposition();

            // Pre-place walls and paths
            PrePlaceStaticTiles();

            // Run WFC on remaining tiles
            int iterations = 0;
            int maxIterations = tilemapWidth * tilemapHeight * 3;

            while (!IsFullyCollapsed() && iterations < maxIterations)
            {
                Vector2Int? cell = FindLowestEntropyCell();

                if (cell == null)
                    break;

                CollapseCell(cell.Value);
                Propagate(cell.Value);
                iterations++;
            }

            return collapsedTilemap;
        }

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
            // Debug.Log("Pre-placing walls and paths...");

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

            // Debug.Log("Static tiles pre-placed");
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
    
            Debug.Log("Empty rooms filled with Empty tiles");
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
         * Gets the room connections for a specific room and places the connections in a bool[]
         */
        private bool[] GetRoomConnections(Vector2Int roomPos)
        {
            bool[] connections = new bool[4]; // [North, East, South, West]

            connections[0] = roomPositions.Contains(roomPos + Vector2Int.up);    // North
            connections[1] = roomPositions.Contains(roomPos + Vector2Int.right); // East
            connections[2] = roomPositions.Contains(roomPos + Vector2Int.down);  // South
            connections[3] = roomPositions.Contains(roomPos + Vector2Int.left);  // West

            return connections;
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
            int connectionCount = connections.Count(c => c);
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
         * Skips pre placed tiles, finding the lowest entropy cell on the grid
         */
        private Vector2Int? FindLowestEntropyCell()
        {
            int lowestEntropy = int.MaxValue;
            List<Vector2Int> candidates = new List<Vector2Int>();

            for (int x = 0; x < tilemapWidth; x++)
            {
                for (int y = 0; y < tilemapHeight; y++)
                {
                    // Skip pre-placed tiles
                    if (isPrePlaced[x, y])
                        continue;

                    int entropy = tilePossibilities[x, y].Count;

                    if (entropy <= 1) continue;

                    if (entropy < lowestEntropy)
                    {
                        lowestEntropy = entropy;
                        candidates.Clear();
                        candidates.Add(new Vector2Int(x, y));
                    }
                    else if (entropy == lowestEntropy)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (candidates.Count > 0)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            return null;
        }

        /**
         * Retrieves the tile possiblities at the position, checks for contradiction, and chooses the weighted tile for
         * the position before adding to the final collapsed tilemap
         */
        private void CollapseCell(Vector2Int pos)
        {
            HashSet<TileType> possibilities = tilePossibilities[pos.x, pos.y];

            if (possibilities.Count == 0)
            {
                // Fallback to grass if contradiction
                possibilities.Add(TileType.Grass);
            }

            TileType chosen = ChooseWeightedTile(possibilities, pos);

            tilePossibilities[pos.x, pos.y].Clear();
            tilePossibilities[pos.x, pos.y].Add(chosen);
            collapsedTilemap[pos.x, pos.y] = chosen;
        }

        /**
         * given the position and the possibilities of tiles, chooses a tiletype based on weight
         */
        private TileType ChooseWeightedTile(HashSet<TileType> possibilities, Vector2Int pos)
        {
            // Weight tiles based on neighbors (clustering effect)
            Dictionary<TileType, float> weights = new Dictionary<TileType, float>();

            foreach (TileType type in possibilities)
            {
                // Base weight
                weights[type] = GetBaseWeight(type);

                // Increase weight if neighbors are same type (clustering)
                int sameTypeNeighbors = CountNeighborsOfType(pos, type);
                weights[type] += sameTypeNeighbors * 1.2f;
            }

            // Weighted random selection
            float totalWeight = weights.Values.Sum();
            float roll = UnityEngine.Random.Range(0, totalWeight);
            float cumulative = 0;

            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                    return kvp.Key;
            }

            return possibilities.First();
        }

        /**
         * Sets the weight of tiles
         */
        private float GetBaseWeight(TileType type)
        {
            switch (type)
            {
                case TileType.Grass:
                    return 4.0f; // Most common
                case TileType.Dirt:
                    return 3.0f; // Common
                case TileType.Water:
                    return 1.0f; // Rare
                case TileType.Path:
                    return 2.0f; // Medium (though mostly pre-placed)
                default:
                    return 1.0f;
            }
        }

        /**
         * Counts the neighboring tiles that are the same tile type
         */
        private int CountNeighborsOfType(Vector2Int pos, TileType type)
        {
            int count = 0;
            Vector2Int[] neighbors = {
                pos + Vector2Int.up,
                pos + Vector2Int.down,
                pos + Vector2Int.left,
                pos + Vector2Int.right
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (IsInBounds(neighbor) &&
                    tilePossibilities[neighbor.x, neighbor.y].Count == 1 &&
                    tilePossibilities[neighbor.x, neighbor.y].Contains(type))
                {
                    count++;
                }
            }

            return count;
        }

        /**
         * Ensures that neighbor cells are aware of what tile is picked in order to constrain tile types to follow the adjacency rules
         */
        private void Propagate(Vector2Int startPos)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(startPos);

            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                if (visited.Contains(current))
                    continue;

                visited.Add(current);

                Vector2Int[] neighbors = {
                    current + Vector2Int.up,
                    current + Vector2Int.down,
                    current + Vector2Int.left,
                    current + Vector2Int.right
                };

                Direction[] directions = {
                    Direction.North,
                    Direction.South,
                    Direction.West,
                    Direction.East
                };

                for (int i = 0; i < neighbors.Length; i++)
                {
                    Vector2Int neighbor = neighbors[i];

                    if (!IsInBounds(neighbor))
                        continue;

                    // Skip pre-placed tiles
                    if (isPrePlaced[neighbor.x, neighbor.y])
                        continue;

                    if (ConstrainCell(neighbor, current, directions[i]))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
        
        /**
         * Constrains a cells neighbor tile possiblities to follow adjacency rules
         */
        private bool ConstrainCell(Vector2Int cell, Vector2Int neighbor, Direction directionToNeighbor)
        {
            HashSet<TileType> cellPoss = tilePossibilities[cell.x, cell.y];
            HashSet<TileType> neighborPoss = tilePossibilities[neighbor.x, neighbor.y];

            if (cellPoss.Count <= 1)
                return false;

            bool changed = false;
            HashSet<TileType> validStates = new HashSet<TileType>();

            foreach (TileType neighborType in neighborPoss)
            {
                Direction oppositeDir = GetOppositeDirection(directionToNeighbor);

                if (adjacencyRules[neighborType].ContainsKey(oppositeDir))
                {
                    validStates.UnionWith(adjacencyRules[neighborType][oppositeDir]);
                }
            }

            List<TileType> toRemove = new List<TileType>();
            foreach (TileType type in cellPoss)
            {
                if (!validStates.Contains(type))
                {
                    toRemove.Add(type);
                    changed = true;
                }
            }

            foreach (TileType type in toRemove)
            {
                cellPoss.Remove(type);
            }

            return changed;
        }

        /**
         * Simply returns opposite directions from what is passed in
         */
        private Direction GetOppositeDirection(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return Direction.South;
                case Direction.South: return Direction.North;
                case Direction.East: return Direction.West;
                case Direction.West: return Direction.East;
                default: return Direction.North;
            }
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

        /**
         * Checks that a position in in bounds of the tilemap
         */
        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < tilemapWidth &&
                   pos.y >= 0 && pos.y < tilemapHeight;
        }

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