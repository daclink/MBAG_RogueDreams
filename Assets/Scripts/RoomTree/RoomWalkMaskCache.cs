using System.Collections.Generic;
using MBAG;
using UnityEngine;

namespace WFC
{
    /// <summary>Caches 8×8 walk masks per room grid key and dungeon layout version.</summary>
    public sealed class RoomWalkMaskCache
    {
        private readonly Dictionary<(Vector2Int roomGrid, int version), ulong> _masks =
            new Dictionary<(Vector2Int, int), ulong>();

        public ulong GetOrBuild(Vector2Int roomGrid, int layoutVersion, TileType[,] tileData10)
        {
            var key = (roomGrid, layoutVersion);
            if (_masks.TryGetValue(key, out ulong existing))
                return existing;

            ulong mask = RoomWalkMaskBuilder.BuildInteriorWalkMask8x8(tileData10);
            _masks[key] = mask;
            return mask;
        }

        public void Clear() => _masks.Clear();

        /// <summary>Warm masks for the given rooms (same version).</summary>
        public void WarmNeighbors(int layoutVersion, IEnumerable<RoomTreeNode> nodes)
        {
            if (nodes == null) return;
            foreach (var n in nodes)
            {
                if (n?.TileData == null) continue;
                GetOrBuild(n.GridPosition, layoutVersion, n.TileData);
            }
        }
    }
}
