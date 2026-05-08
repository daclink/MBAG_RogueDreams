using System.Collections.Generic;
using UnityEngine;

namespace WFC
{
    /// <summary>
    /// Builds a random spanning tree over a grid. Prefers branching, reduces (not eliminates) dead ends.
    /// </summary>
    public static class RoomTreeSpanningTreeBuilder
    {
        private static readonly Vector2Int[] GridDirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        /// <summary>
        /// Builds a spanning tree over gridSize×gridSize. Returns dictionary of gridPos -> RoomTreeNode.
        /// Root is random. Assigns Depth via BFS from root.
        /// </summary>
        public static Dictionary<Vector2Int, RoomTreeNode> Build(int gridSize, int randomSeed = 0)
        {
            if (randomSeed != 0) Random.InitState(randomSeed);

            var nodes = new Dictionary<Vector2Int, RoomTreeNode>();
            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                    nodes[new Vector2Int(x, y)] = new RoomTreeNode(new Vector2Int(x, y));

            Vector2Int root = new Vector2Int(Random.Range(0, gridSize), Random.Range(0, gridSize));
            var connected = new HashSet<Vector2Int> { root };
            var unconnected = new HashSet<Vector2Int>();
            foreach (var p in nodes.Keys)
                if (p != root) unconnected.Add(p);

            // Edge count per node (for branching bias)
            var edgeCount = new Dictionary<Vector2Int, int>();
            foreach (var p in nodes.Keys) edgeCount[p] = 0;

            while (unconnected.Count > 0)
            {
                var candidates = new List<(Vector2Int from, Vector2Int to)>();
                foreach (Vector2Int c in connected)
                {
                    foreach (Vector2Int dir in GridDirs)
                    {
                        Vector2Int n = c + dir;
                        if (n.x >= 0 && n.x < gridSize && n.y >= 0 && n.y < gridSize && unconnected.Contains(n))
                            candidates.Add((c, n));
                    }
                }

                if (candidates.Count == 0) break;

                // Prefer branching: weight toward (from) nodes that already have more edges
                (Vector2Int from, Vector2Int to) chosen = candidates[Random.Range(0, candidates.Count)];
                float branchBias = 0.65f; // 65% chance to pick from a branch point when available
                if (Random.value < branchBias && candidates.Count > 1)
                {
                    int maxEdges = 0;
                    foreach (var (f, _) in candidates)
                        if (edgeCount[f] > maxEdges) maxEdges = edgeCount[f];
                    if (maxEdges > 0)
                    {
                        var branchCandidates = new List<(Vector2Int, Vector2Int)>();
                        foreach (var c in candidates)
                            if (edgeCount[c.from] == maxEdges) branchCandidates.Add(c);
                        if (branchCandidates.Count > 0)
                            chosen = branchCandidates[Random.Range(0, branchCandidates.Count)];
                    }
                }
                else if (candidates.Count > 1)
                {
                    chosen = candidates[Random.Range(0, candidates.Count)];
                }

                AddEdge(nodes, chosen.from, chosen.to);
                edgeCount[chosen.from]++;
                edgeCount[chosen.to]++;
                connected.Add(chosen.to);
                unconnected.Remove(chosen.to);
            }

            AssignDepths(nodes[root], gridSize);
            return nodes;
        }

        private static void AddEdge(Dictionary<Vector2Int, RoomTreeNode> nodes, Vector2Int a, Vector2Int b)
        {
            var dir = b - a;
            int idx = DirToIndex(dir);
            if (idx >= 0)
            {
                nodes[a].Neighbors[idx] = nodes[b];
                nodes[b].Neighbors[(idx + 2) % 4] = nodes[a];
            }
        }

        private static int DirToIndex(Vector2Int d)
        {
            if (d == Vector2Int.up) return 0;
            if (d == Vector2Int.right) return 1;
            if (d == Vector2Int.down) return 2;
            if (d == Vector2Int.left) return 3;
            return -1;
        }

        private static void AssignDepths(RoomTreeNode root, int gridSize)
        {
            var visited = new HashSet<RoomTreeNode>();
            var queue = new Queue<(RoomTreeNode node, int depth)>();
            queue.Enqueue((root, 0));

            while (queue.Count > 0)
            {
                var (node, depth) = queue.Dequeue();
                if (visited.Contains(node)) continue;
                visited.Add(node);
                node.Depth = depth;

                for (int i = 0; i < 4; i++)
                {
                    var n = node.Neighbors[i];
                    if (n != null && !visited.Contains(n))
                        queue.Enqueue((n, depth + 1));
                }
            }
        }
    }
}
