using System.Collections.Generic;
using UnityEngine;

namespace WFC
{
    
    /**
     * This class will generate the room layout grid by picking the positions of the necessary rooms then connecting them together
     */
    public class RoomLayoutGenerator
    {
        private int mapWidth = 10;
        private int mapHeight = 10;

        private int minRooms = 8;
        private int maxRooms = 16;

        //This will store the final room layout in a 2dArray
        private int[,] roomGrid;
        
        //This will store the individual room positions (unsure if needed at the moment)
        private List<Vector2Int> placedRooms = new List<Vector2Int>();

        private int startRoomIndex = -1;
        private int endRoomIndex = -1;
        private int itemRoomIndex = -1;
        
        private Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        /**
         * This is the driver method for this class that calls all necessary methods to generate the room grid
         */
        public int[,] GenerateRoomGrid()
        {
            //Initialization
            roomGrid = new int[mapWidth, mapHeight];
            InitializeRoomGrid();
            placedRooms.Clear();
            
            PlaceStartRoom();
            
            GrowRoomsFromStartRoom(minRooms, maxRooms);

            AssignSpecialRooms();
            
            MarkRoomsOnLayout();
            
            return roomGrid;
        }
        
        /**
         * This method initializes the room grid to empty tiles, all are -1 to start
         */
        private void InitializeRoomGrid()
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    roomGrid[x, y] = -1;
                }
            }
        }
        
        /**
         * Randomly places the start room in the grid
         */
        private void PlaceStartRoom()
        {
            
            Vector2Int startPos = new Vector2Int(Random.Range(0, mapWidth), Random.Range(0, mapHeight));
            placedRooms.Add(startPos);
            startRoomIndex = 0;

            Debug.Log($"Start room was placed at {startPos}");
        }

        /**
         * This uses a growth algorithm to generate the room layout. WFC is used for the final tilemap generation
         */
        private void GrowRoomsFromStartRoom(int minRooms, int maxRooms)
        {
            int roomCount = Random.Range(minRooms, maxRooms + 1);
            int maxAttempts = roomCount * 10; // can adjust the max attempts if failing
            int attempts = 0;

            while (placedRooms.Count < roomCount && attempts < maxAttempts)
            {
                attempts++;
                
                //Pick a random room from placedRooms and try to add an adjacent room
                Vector2Int randRoom = placedRooms[Random.Range(0, placedRooms.Count)];
                Vector2Int newRoomPos = GetAdjacentEmptyPosition(randRoom);

                if (newRoomPos != Vector2Int.one * -1)
                {
                    //This checks if the newly selected position is valid
                    if (IsValidPosition(newRoomPos) && !placedRooms.Contains(newRoomPos))
                    {
                        placedRooms.Add(newRoomPos);
                    }
                }
                
            }
        }
        
        /**
         * This gets an empty position that is adjacent to the given room position from a random vector direction.
         * This will be passed back to the GrowRooms Method and added to placedRooms if valid
         */
        private Vector2Int GetAdjacentEmptyPosition(Vector2Int baseRoomPos)
        {
            //shuffles the directions array to give randomness to growth
            ShuffleArray(directions);
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int newRoomPos = baseRoomPos + dir;
                if (IsValidPosition(newRoomPos) && !placedRooms.Contains(newRoomPos))
                {
                    return newRoomPos;
                }
            }
            return Vector2Int.one * -1;
        }

        /**
         * Simply checks if a newly selected position in in bounds of the map.
         * If the position is valid, it will be added to the placedRooms
         */
        private bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < mapWidth && pos.y >= 0 && pos.y < mapHeight;
        }

        /**
         * This is a driver method to assign the special rooms we need which are item and boss rooms
         * Utilizes other helper methods to accomplish the assignments
         */
        private void AssignSpecialRooms()
        {
            if (placedRooms.Count < 3)
            {
                Debug.LogError("Not enough rooms for special rooms");
                return;
            }
            
            //Find the furthest room from the start room to be used as the boss/end room
            endRoomIndex = GetFarthestRoomIndex(startRoomIndex);
            
            //find a dead end for the item room. Will be assigned to any random room if no deadend found
            itemRoomIndex = GetRandomDeadEnd();

            while (itemRoomIndex == -1 || itemRoomIndex == startRoomIndex || itemRoomIndex == endRoomIndex)
            {
                itemRoomIndex = Random.Range(0, placedRooms.Count);
            }
            
        }
        
        /**
         * This is used to get the farthest room position from the start room which will become the boss room
         */
        private int GetFarthestRoomIndex(int fromIndex)
        {
            Vector2Int startRoom = placedRooms[fromIndex];
            int farthestRoomIndex = -1;
            float maxDistance = 0;

            for (int i = 0; i < placedRooms.Count; i++)
            {
                if (i == fromIndex) continue;
                
                //calculate the distance between start room and current room in for loop
                float distance = Vector2Int.Distance(startRoom, placedRooms[i]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestRoomIndex = i;
                }
            }
            return farthestRoomIndex;
        }
        
        /**
         * This is used to get a random dead end which will be assigned as the item room
         */
        private int GetRandomDeadEnd()
        {
            List<int> deadEnds = new List<int>();

            for (int i = 0; i < placedRooms.Count; i++)
            {
                if (GetAdjacentRoomCount(placedRooms[i]) == 1)
                {
                    deadEnds.Add(i);
                }
            }

            //possibly can change to check that the dead end selected is not a start or end room.
            //This check is already done in Assign special rooms for now
            if (deadEnds.Count > 0)
            {
                return deadEnds[Random.Range(0, deadEnds.Count)];
            }

            return -1;
        }

        
        private int GetAdjacentRoomCount(Vector2Int pos)
        {
            int count = 0;

            foreach (Vector2Int dir in directions)
            {
                Vector2Int checkPosition = pos + dir;
                if (placedRooms.Contains(checkPosition))
                {
                    count++;
                }
            }
            return count;
        }
        
        /**
         * This method uses the placed rooms list to mark each room on the grid with its corresponding room type value.
         * This needs to consider the specific room types which are defined in DungeonGeneration under the RoomType enum
         */
        private void MarkRoomsOnLayout()
        {
            for (int i = 0; i < placedRooms.Count; i++)
            {
                Vector2Int currPos = placedRooms[i];
                
                if (i == startRoomIndex)
                {
                    roomGrid[currPos.x, currPos.y] = (int)RoomType.Start;
                }
                else if (i == endRoomIndex)
                {
                    roomGrid[currPos.x, currPos.y] = (int)RoomType.End;
                }
                else if (i == itemRoomIndex)
                {
                    roomGrid[currPos.x, currPos.y] = (int)RoomType.Item;
                }
                else
                {
                    roomGrid[currPos.x, currPos.y] = (int)RoomType.Normal;
                }
            }
            
            Debug.Log("Room layout marked with types");
        }
        

        /**
         * This is used to shuffle the directions array to provide randomness in the growth algorithm
         */
        private void ShuffleArray(Vector2Int[] vector2Ints)
        {
            for (int i = vector2Ints.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector2Int temp = vector2Ints[i];
                vector2Ints[i] = vector2Ints[j];
                vector2Ints[j] = temp;
            }
        }
        
        // getters
        public List<Vector2Int> GetRoomPositions() => placedRooms;
        public Vector2Int GetStartRoomPosition() => placedRooms[startRoomIndex];
        public Vector2Int GetEndRoomPosition() => placedRooms[endRoomIndex];
        public Vector2Int GetItemRoomPosition() => placedRooms[itemRoomIndex];
        public RoomType GetRoomType(Vector2Int pos)
        {
            int index = placedRooms.IndexOf(pos);
            if (index == startRoomIndex) return RoomType.Start;
            if (index == endRoomIndex) return RoomType.End;
            if (index == itemRoomIndex) return RoomType.Item;
            return RoomType.Normal;
        }
    }
}