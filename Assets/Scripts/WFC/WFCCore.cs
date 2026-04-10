using System.Collections.Generic;
using UnityEngine;
using MBAG;

namespace WFC
{
    /// <summary>
    /// Shared WFC algorithms: adjacency rules, propagate, constrain, collapse.
    /// Used by WFCTilemap and RoomWFCTilemap.
    /// </summary>
    public static class WFCCore
    {
        private static Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>> _adjacencyRules;
        private static readonly Dictionary<TileType, float> WeightBuffer = new Dictionary<TileType, float>();
        private static readonly HashSet<TileType> ValidStatesBuffer = new HashSet<TileType>();
        private static readonly List<TileType> ToRemoveBuffer = new List<TileType>();
        private static readonly List<Vector2Int> CandidatesBuffer = new List<Vector2Int>();

        public static Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>> GetAdjacencyRules()
        {
            if (_adjacencyRules != null) return _adjacencyRules;

            _adjacencyRules = new Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>>();

            _adjacencyRules[TileType.Path] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } },
                { Direction.South, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } },
                { Direction.East, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } },
                { Direction.West, new HashSet<TileType> { TileType.Path, TileType.Dirt, TileType.Grass } }
            };

            _adjacencyRules[TileType.Dirt] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } },
                { Direction.South, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } },
                { Direction.East, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } },
                { Direction.West, new HashSet<TileType> { TileType.Dirt, TileType.Path, TileType.Water, TileType.Grass } }
            };

            _adjacencyRules[TileType.Grass] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } },
                { Direction.South, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } },
                { Direction.East, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } },
                { Direction.West, new HashSet<TileType> { TileType.Grass, TileType.Dirt, TileType.Water } }
            };

            _adjacencyRules[TileType.Water] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } },
                { Direction.South, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } },
                { Direction.East, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } },
                { Direction.West, new HashSet<TileType> { TileType.Water, TileType.Dirt, TileType.Grass } }
            };

            _adjacencyRules[TileType.Wall] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } },
                { Direction.South, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } },
                { Direction.East, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } },
                { Direction.West, new HashSet<TileType> { TileType.Wall, TileType.Path, TileType.Dirt, TileType.Grass, TileType.Water } }
            };

            _adjacencyRules[TileType.Empty] = new Dictionary<Direction, HashSet<TileType>>
            {
                { Direction.North, new HashSet<TileType> { TileType.Empty } },
                { Direction.South, new HashSet<TileType> { TileType.Empty } },
                { Direction.East, new HashSet<TileType> { TileType.Empty } },
                { Direction.West, new HashSet<TileType> { TileType.Empty } }
            };

            return _adjacencyRules;
        }

        /// <summary>Propagate constraints from startPos outward (BFS). Call after collapsing a cell.</summary>
        public static void Propagate(
            Vector2Int startPos,
            HashSet<TileType>[,] tilePossibilities,
            bool[,] isPrePlaced,
            Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>> adjacencyRules,
            System.Func<Vector2Int, bool> isInBounds)
        {
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            queue.Enqueue(startPos);

            var neighborOffsets = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var directions = new[] { Direction.North, Direction.South, Direction.West, Direction.East };

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (visited.Contains(current)) continue;
                visited.Add(current);

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighbor = current + neighborOffsets[i];
                    if (!isInBounds(neighbor)) continue;
                    if (isPrePlaced[neighbor.x, neighbor.y]) continue;

                    if (ConstrainCell(tilePossibilities, neighbor, current, directions[i], adjacencyRules))
                        queue.Enqueue(neighbor);
                }
            }
        }

        public static bool ConstrainCell(
            HashSet<TileType>[,] tilePossibilities,
            Vector2Int cell,
            Vector2Int neighbor,
            Direction directionToNeighbor,
            Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>> adjacencyRules)
        {
            var cellPoss = tilePossibilities[cell.x, cell.y];
            var neighborPoss = tilePossibilities[neighbor.x, neighbor.y];
            if (cellPoss.Count <= 1) return false;

            ValidStatesBuffer.Clear();
            Direction oppositeDir = GetOppositeDirection(directionToNeighbor);
            foreach (TileType neighborType in neighborPoss)
            {
                if (adjacencyRules[neighborType].TryGetValue(oppositeDir, out var valid))
                    ValidStatesBuffer.UnionWith(valid);
            }

            ToRemoveBuffer.Clear();
            foreach (TileType type in cellPoss)
                if (!ValidStatesBuffer.Contains(type))
                    ToRemoveBuffer.Add(type);

            if (ToRemoveBuffer.Count == 0) return false;
            foreach (TileType type in ToRemoveBuffer)
                cellPoss.Remove(type);
            return true;
        }

        public static Vector2Int? FindLowestEntropyCell(
            HashSet<TileType>[,] tilePossibilities,
            bool[,] isPrePlaced,
            List<Vector2Int> uncollapsedCells)
        {
            int lowestEntropy = int.MaxValue;
            CandidatesBuffer.Clear();

            for (int i = 0; i < uncollapsedCells.Count; i++)
            {
                Vector2Int p = uncollapsedCells[i];
                int entropy = tilePossibilities[p.x, p.y].Count;
                if (entropy <= 1) continue;

                if (entropy < lowestEntropy)
                {
                    lowestEntropy = entropy;
                    CandidatesBuffer.Clear();
                    CandidatesBuffer.Add(p);
                }
                else if (entropy == lowestEntropy)
                    CandidatesBuffer.Add(p);
            }

            if (CandidatesBuffer.Count > 0)
                return CandidatesBuffer[Random.Range(0, CandidatesBuffer.Count)];
            return null;
        }

        public static void CollapseCell(
            Vector2Int pos,
            HashSet<TileType>[,] tilePossibilities,
            TileType[,] collapsedTilemap,
            int width,
            int height,
            Dictionary<TileType, Dictionary<Direction, HashSet<TileType>>> adjacencyRules)
        {
            var possibilities = tilePossibilities[pos.x, pos.y];
            if (possibilities.Count == 0)
            {
                Debug.LogWarning($"WFC contradiction at {pos} - fallback to Grass.");
                possibilities.Add(TileType.Grass);
            }

            TileType chosen = ChooseWeightedTile(possibilities, pos, tilePossibilities, width, height);
            possibilities.Clear();
            possibilities.Add(chosen);
            collapsedTilemap[pos.x, pos.y] = chosen;
        }

        public static TileType ChooseWeightedTile(
            HashSet<TileType> possibilities,
            Vector2Int pos,
            HashSet<TileType>[,] tilePossibilities,
            int width,
            int height)
        {
            WeightBuffer.Clear();
            float totalWeight = 0f;

            foreach (TileType type in possibilities)
            {
                float w = GetBaseWeight(type) + CountNeighborsOfType(pos, type, tilePossibilities, width, height) * 1.2f;
                WeightBuffer[type] = w;
                totalWeight += w;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var kvp in WeightBuffer)
            {
                cumulative += kvp.Value;
                if (roll < cumulative) return kvp.Key;
            }
            foreach (TileType t in possibilities) return t;
            return TileType.Grass;
        }

        public static float GetBaseWeight(TileType type)
        {
            return type switch
            {
                TileType.Grass => 4.0f,
                TileType.Dirt => 3.0f,
                TileType.Water => 1.0f,
                TileType.Path => 2.0f,
                _ => 1.0f
            };
        }

        public static int CountNeighborsOfType(
            Vector2Int pos,
            TileType type,
            HashSet<TileType>[,] tilePossibilities,
            int width,
            int height)
        {
            int count = 0;
            var neighbors = new[] { pos + Vector2Int.up, pos + Vector2Int.down, pos + Vector2Int.left, pos + Vector2Int.right };
            foreach (Vector2Int n in neighbors)
            {
                if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height &&
                    tilePossibilities[n.x, n.y].Count == 1 && tilePossibilities[n.x, n.y].Contains(type))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Sweep all cells: any non-pre-placed cell whose possibilities were reduced to exactly 1
        /// by propagation (but never explicitly collapsed) still has TileType.Empty in collapsedTilemap.
        /// This writes the actual resolved type. Also handles contradictions (0 possibilities → Grass).
        /// Call after the WFC loop finishes.
        /// </summary>
        public static void FinalizeTilemap(
            HashSet<TileType>[,] tilePossibilities,
            TileType[,] collapsedTilemap,
            bool[,] isPrePlaced,
            int width,
            int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (isPrePlaced[x, y]) continue;

                    var poss = tilePossibilities[x, y];
                    if (poss.Count == 1)
                    {
                        foreach (TileType t in poss)
                        {
                            collapsedTilemap[x, y] = t;
                            break;
                        }
                    }
                    else if (poss.Count == 0)
                    {
                        collapsedTilemap[x, y] = TileType.Grass;
                    }
                }
            }
        }

        public static Direction GetOppositeDirection(Direction dir)
        {
            return dir switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => Direction.North
            };
        }
    }
}
