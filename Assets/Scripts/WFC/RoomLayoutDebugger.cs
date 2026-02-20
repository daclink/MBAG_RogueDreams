using UnityEngine;
using System.Text;

namespace WFC
{
    /**
     * Generated with claude as a debugger for my RoomLayoutGenerator
     */
    public class RoomLayoutDebugger : MonoBehaviour
    {
        [Header("Room Grid Values")]
        [SerializeField] private int mapWidth;
        [SerializeField] private int mapHeight;
        [SerializeField] private int minRooms;
        [SerializeField] private int maxRooms;
        
        [Header("Testing Values")]
        [SerializeField] private int testAmount;
        
        
        private RoomLayoutGenerator generator;

        public void Start()
        {
            for (int i = 0; i < testAmount; i++)
            {
                TestRoomLayout();    
            }
            
        }
        
        [ContextMenu("Test Room Layout")]
        public void TestRoomLayout()
        {
            generator = new RoomLayoutGenerator();
            int[,] roomGrid = generator.GenerateRoomGrid(mapWidth, mapHeight, minRooms, maxRooms);
            
            // Print to console
            PrintRoomGrid(roomGrid);
            
            // Print statistics
            // PrintStatistics(generator);
        }
        
        private void PrintRoomGrid(int[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Room Layout Grid ===");
            
            // Print top border
            sb.Append("  ");
            for (int x = 0; x < width; x++)
            {
                sb.Append(x % 10);
            }
            sb.AppendLine();
            
            // Print grid (from top to bottom)
            for (int y = height - 1; y >= 0; y--)
            {
                sb.Append($"{y % 10} ");
                
                for (int x = 0; x < width; x++)
                {
                    int value = grid[x, y];
                    
                    switch ((RoomType)value)
                    {
                        case RoomType.Start:
                            sb.Append("S"); // Start room
                            break;
                        case RoomType.End:
                            sb.Append("E"); // End room
                            break;
                        case RoomType.Item:
                            sb.Append("I"); // Item room
                            break;
                        case RoomType.Normal:
                            sb.Append("■"); // Normal room
                            break;
                        default: // Empty
                            sb.Append("·");
                            break;
                    }
                }
                sb.AppendLine();
            }
            
            Debug.Log(sb.ToString());
        }
        
        private void PrintStatistics(RoomLayoutGenerator gen)
        {
            var rooms = gen.GetRoomPositions();
            var start = gen.GetStartRoomPosition();
            var end = gen.GetEndRoomPosition();
            var item = gen.GetItemRoomPosition();
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Statistics ===");
            sb.AppendLine($"Total Rooms: {rooms.Count}");
            sb.AppendLine($"Start Room: {start}");
            sb.AppendLine($"End Room: {end} (Distance from start: {Vector2Int.Distance(start, end):F2})");
            sb.AppendLine($"Item Room: {item}");
            
            // Check connectivity
            int deadEnds = 0;
            int twoConnections = 0;
            int threeConnections = 0;
            int fourConnections = 0;
            
            foreach (var room in rooms)
            {
                int connections = CountAdjacentRooms(room, rooms);
                switch (connections)
                {
                    case 1: deadEnds++; break;
                    case 2: twoConnections++; break;
                    case 3: threeConnections++; break;
                    case 4: fourConnections++; break;
                }
            }
            
            sb.AppendLine($"Dead Ends: {deadEnds}");
            sb.AppendLine($"2 Connections: {twoConnections}");
            sb.AppendLine($"3 Connections: {threeConnections}");
            sb.AppendLine($"4 Connections: {fourConnections}");
            
            Debug.Log(sb.ToString());
        }
        
        private int CountAdjacentRooms(Vector2Int pos, System.Collections.Generic.List<Vector2Int> rooms)
        {
            int count = 0;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (var dir in dirs)
            {
                if (rooms.Contains(pos + dir))
                    count++;
            }
            
            return count;
        }
    }
}