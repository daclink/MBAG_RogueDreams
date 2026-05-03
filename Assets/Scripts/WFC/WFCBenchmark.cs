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
            (3,  3,  3,  6,   1000),
            (6,  6,  4,  12,  1000),
            (12, 12, 8,  24,  500),
            (16, 16, 10, 32,  500),
            (24, 24, 15, 48,  200),
            (36, 36, 20, 72,  100),
            (48, 48, 30, 96,  50),
            (64, 64, 40, 128, 10),
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