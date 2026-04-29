using System;
using System.Collections.Generic;

namespace MBAG.Pathfinding
{
    /// <summary>
    /// 8×8 room-local navigation on a 64-bit walk mask (bit = walkable).
    /// Index order matches <c>BbGrid</c>: <c>index = y * 8 + x</c> with <c>x = index % 8</c>, <c>y = index / 8</c>.
    /// Supports 8-neighbor moves with strict corner-cutting for diagonals.
    /// </summary>
    public static class RoomBitGrid64
    {
        public const int Dim = 8;
        public const int CellCount = Dim * Dim;

        public static bool IsWalkable(ulong walkMask, int index)
        {
            if ((uint)index >= CellCount) return false;
            return (walkMask & (1UL << index)) != 0;
        }

        /// <summary>BFS from <paramref name="goalIndex"/>; <paramref name="dist"/>[i] = steps from cell i to goal, or -1 if unreachable.</summary>
        public static void ComputeDistancesFromGoal(ulong walkMask, int goalIndex, int[] dist)
        {
            if (dist == null || dist.Length < CellCount)
                throw new ArgumentException("dist must have length >= 64.", nameof(dist));

            for (int i = 0; i < CellCount; i++)
                dist[i] = -1;

            if (!IsWalkable(walkMask, goalIndex))
                return;

            var q = new Queue<int>(32);
            dist[goalIndex] = 0;
            q.Enqueue(goalIndex);

            while (q.Count > 0)
            {
                int i = q.Dequeue();
                int d = dist[i];
                int x = i % Dim;
                int y = i / Dim;

                void TryEdge(int ni, bool allow)
                {
                    if (!allow) return;
                    if (!IsWalkable(walkMask, ni)) return;
                    if (dist[ni] != -1) return;
                    dist[ni] = d + 1;
                    q.Enqueue(ni);
                }

                // 4-neighbors
                TryEdge(i + 8, y < Dim - 1);
                TryEdge(i - 8, y > 0);
                TryEdge(i + 1, x < Dim - 1);
                TryEdge(i - 1, x > 0);

                // Diagonals with strict corner-cutting
                TryEdge(i + 9, y < Dim - 1 && x < Dim - 1 &&
                    IsWalkable(walkMask, i + 1) && IsWalkable(walkMask, i + 8));
                TryEdge(i + 7, y < Dim - 1 && x > 0 &&
                    IsWalkable(walkMask, i - 1) && IsWalkable(walkMask, i + 8));
                TryEdge(i - 7, y > 0 && x < Dim - 1 &&
                    IsWalkable(walkMask, i + 1) && IsWalkable(walkMask, i - 8));
                TryEdge(i - 9, y > 0 && x > 0 &&
                    IsWalkable(walkMask, i - 1) && IsWalkable(walkMask, i - 8));
            }
        }

        /// <summary>If <paramref name="goalIndex"/> is blocked, pick a walkable cell with smallest Manhattan distance.</summary>
        public static int NearestWalkable(ulong walkMask, int goalIndex)
        {
            if (IsWalkable(walkMask, goalIndex))
                return goalIndex;

            int gx = goalIndex % Dim;
            int gy = goalIndex / Dim;
            int best = -1;
            int bestDist = int.MaxValue;

            for (int i = 0; i < CellCount; i++)
            {
                if (!IsWalkable(walkMask, i)) continue;
                int x = i % Dim;
                int y = i / Dim;
                int md = Math.Abs(x - gx) + Math.Abs(y - gy);
                if (md < bestDist || (md == bestDist && i < best))
                {
                    bestDist = md;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Pick a neighbor of <paramref name="fromIndex"/> that strictly decreases distance-to-goal (BFS field).</summary>
        public static bool TryGreedyTowardGoal(ulong walkMask, int[] dist, int fromIndex, out int nextIndex)
        {
            nextIndex = fromIndex;
            if ((uint)fromIndex >= CellCount) return false;
            if (!IsWalkable(walkMask, fromIndex)) return false;

            int my = dist[fromIndex];
            if (my < 0) return false;
            if (my == 0) return false;

            int x = fromIndex % Dim;
            int y = fromIndex / Dim;
            int best = int.MaxValue;
            int bestIdx = -1;

            void Consider(int ni, bool topologyOk, bool cornerOk)
            {
                if (!topologyOk || !cornerOk) return;
                if (!IsWalkable(walkMask, ni)) return;
                int nd = dist[ni];
                if (nd < 0) return;
                if (nd >= my) return;
                if (nd < best || (nd == best && (bestIdx < 0 || ni < bestIdx)))
                {
                    best = nd;
                    bestIdx = ni;
                }
            }

            Consider(fromIndex + 8, y < Dim - 1, true);
            Consider(fromIndex - 8, y > 0, true);
            Consider(fromIndex + 1, x < Dim - 1, true);
            Consider(fromIndex - 1, x > 0, true);

            Consider(fromIndex + 9, y < Dim - 1 && x < Dim - 1,
                IsWalkable(walkMask, fromIndex + 1) && IsWalkable(walkMask, fromIndex + 8));
            Consider(fromIndex + 7, y < Dim - 1 && x > 0,
                IsWalkable(walkMask, fromIndex - 1) && IsWalkable(walkMask, fromIndex + 8));
            Consider(fromIndex - 7, y > 0 && x < Dim - 1,
                IsWalkable(walkMask, fromIndex + 1) && IsWalkable(walkMask, fromIndex - 8));
            Consider(fromIndex - 9, y > 0 && x > 0,
                IsWalkable(walkMask, fromIndex - 1) && IsWalkable(walkMask, fromIndex - 8));

            if (bestIdx < 0) return false;
            nextIndex = bestIdx;
            return true;
        }
    }
}
