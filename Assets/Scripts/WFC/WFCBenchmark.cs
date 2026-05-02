// using System.Collections.Generic;
// using System.Diagnostics;
// using UnityEngine;
// using WFC;
// using MBAG;
//
// public class WFCBenchmark : MonoBehaviour
// {
//     [SerializeField] private int runsPerSize = 1000;
//
//     // Each entry: (mapWidth, mapHeight, minRooms, maxRooms)
//     private readonly (int w, int h, int min, int max)[] _testSizes = new[]
//     {
//         (3,  3,  2,  4),
//         (5,  5,  4,  8),
//         (8,  8,  8,  16),
//         (10, 10, 10, 20),
//         (15, 15, 15, 30),
//     };
//
//     void Start()
//     {
//         foreach (var (w, h, min, max) in _testSizes)
//         {
//             double totalMs = 0;
//
//             for (int i = 0; i < runsPerSize; i++)
//             {
//                 // Fresh layout each run
//                 var layoutGen = new RoomLayoutGenerator();
//                 var roomLayout = layoutGen.GenerateRoomGrid(w, h, min, max);
//                 var roomPositions = layoutGen.GetRoomPositions();
//
//                 var sw = Stopwatch.StartNew();
//
//                 var wfc = new WFCTilemap(roomLayout, roomPositions, layoutGen, pathWidth: 2);
//                 wfc.Generate();
//
//                 sw.Stop();
//                 totalMs += sw.Elapsed.TotalMilliseconds;
//             }
//
//             double avgMs = totalMs / runsPerSize;
//             UnityEngine.Debug.Log($"Grid {w}x{h} | {runsPerSize} runs | Avg: {avgMs:F3} ms");
//         }
//     }
// }

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using WFC;

public class WFCBenchmark : MonoBehaviour
{
    void Start()
    {
        // (mapW, mapH, minRooms, maxRooms, runsForThisSize)
        var testSizes = new (int w, int h, int min, int max, int runs)[]
        {
            (3,  3,  3,  4,   1000),
            (5,  5,  4,  8,   1000),
            (8,  8,  8,  16,  500),
            (10, 10, 10, 20,  500),
            (15, 15, 15, 30,  200),
            (20, 20, 20, 40,  100),
            (30, 30, 30, 60,  50),
            (50, 50, 50, 100, 10),
        };

        UnityEngine.Debug.Log("=== WFC Benchmark Start ===");

        foreach (var (w, h, min, max, runs) in testSizes)
        {
            double totalMs = 0;
            int failedRuns = 0;

            // 3 warm-up runs only
            for (int i = 0; i < 3; i++)
            {
                var lg = new RoomLayoutGenerator();
                var rl = lg.GenerateRoomGrid(w, h, min, max);
                new WFCTilemap(rl, lg.GetRoomPositions(), lg, 2).Generate();
            }

            for (int i = 0; i < runs; i++)
            {
                try
                {
                    var layoutGen = new RoomLayoutGenerator();
                    var roomLayout = layoutGen.GenerateRoomGrid(w, h, min, max);
                    var roomPositions = layoutGen.GetRoomPositions();

                    var sw = Stopwatch.StartNew();
                    new WFCTilemap(roomLayout, roomPositions, layoutGen, 2).Generate();
                    sw.Stop();

                    totalMs += sw.Elapsed.TotalMilliseconds;
                }
                catch (System.Exception e)
                {
                    failedRuns++;
                    UnityEngine.Debug.LogWarning($"Run failed at {w}x{h}: {e.Message}");
                }
            }

            int successful = runs - failedRuns;
            double avgMs = successful > 0 ? totalMs / successful : 0;

            UnityEngine.Debug.Log(
                $"Grid {w,3}x{h,-3} | " +
                $"Runs: {runs,4} | " +
                $"Avg: {avgMs,8:F3} ms | " +
                $"Failed: {failedRuns}/{runs}"
            );
        }

        UnityEngine.Debug.Log("=== WFC Benchmark Complete ===");
    }
}