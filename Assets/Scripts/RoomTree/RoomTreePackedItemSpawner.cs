using System;
using System.Collections.Generic;
using DataSchemas.PackedItem;
using MBAG;
using MBAG.Pathfinding;
using UnityEngine;

namespace WFC
{
    /// <summary>
    /// After <see cref="RoomTreeDungeonComponent"/> generates, spawns <see cref="PackedItemPickup"/> on random walkable
    /// 8×8 interior cells. Uses a reference pool, or <see cref="ItemTableBootstrap"/>/random loaded item when the pool is empty.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomTreePackedItemSpawner : MonoBehaviour
    {
        [SerializeField] private RoomTreeDungeonComponent _dungeon;
        [SerializeField] private GameObject _pickupPrefab;
        [Min(0)]
        [SerializeField] private int _minPickupsPerRoom = 0;
        [Min(0)]
        [SerializeField] private int _maxPickupsPerRoom = 1;
        [Tooltip("If non-empty, a random entry is used per spawn. If empty, uses a random entry from the loaded item table.")]
        [SerializeField] private PackedItemReference[] _referencePool;
        [SerializeField] private Transform _pickupParent;

        private readonly RoomWalkMaskCache _maskCache = new RoomWalkMaskCache();
        private readonly List<GameObject> _spawned = new List<GameObject>();

        void Awake()
        {
            if (_dungeon == null)
                _dungeon = GetComponent<RoomTreeDungeonComponent>();
        }

        void OnEnable()
        {
            RoomTreeDungeonComponent.OnRoomTreeGenerated += OnGenerated;
        }

        void OnDisable()
        {
            RoomTreeDungeonComponent.OnRoomTreeGenerated -= OnGenerated;
        }

        void OnGenerated(RoomTreeDungeonComponent dungeon)
        {
            _dungeon = dungeon;
            ClearSpawned();
            if (_pickupPrefab == null || _dungeon == null || _dungeon.Generator?.Nodes == null)
                return;
            if (_minPickupsPerRoom > _maxPickupsPerRoom)
            {
                int t = _minPickupsPerRoom;
                _minPickupsPerRoom = _maxPickupsPerRoom;
                _maxPickupsPerRoom = t;
            }

            var grid = _dungeon.DungeonGrid;
            if (grid == null) return;

            int seed = _dungeon.LayoutVersion * 1009 + 17;
            var rng = new System.Random(seed);
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            Vector3Int playerCell = default;
            var hasPlayer = playerGo != null;
            if (hasPlayer) playerCell = grid.WorldToCell(playerGo.transform.position);

            var maskCache = _maskCache;
            int ver = _dungeon.LayoutVersion;
            var nodes = _dungeon.Generator.Nodes;

            foreach (RoomTreeNode room in nodes.Values)
            {
                if (room?.TileData == null) continue;
                int count = _maxPickupsPerRoom == _minPickupsPerRoom
                    ? _minPickupsPerRoom
                    : rng.Next(_minPickupsPerRoom, _maxPickupsPerRoom + 1);
                if (count <= 0) continue;

                ulong mask = maskCache.GetOrBuild(room.GridPosition, ver, room.TileData);
                var origin = RoomWalkMaskBuilder.RoomOriginCell(room.WorldPosition);
                int playerBit = -1;
                if (hasPlayer && RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, playerCell, out int pb))
                    playerBit = pb;

                var bits = new List<int>(32);
                for (int b = 0; b < RoomBitGrid64.CellCount; b++)
                {
                    if (!RoomBitGrid64.IsWalkable(mask, b)) continue;
                    if (b == playerBit) continue;
                    bits.Add(b);
                }
                for (int i = bits.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (bits[i], bits[j]) = (bits[j], bits[i]);
                }

                int take = Math.Min(count, bits.Count);
                for (int i = 0; i < take; i++)
                {
                    if (!TryPickReference(out PackedItemReference pref)) continue;

                    var cell = RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, bits[i]);
                    var world = grid.GetCellCenterWorld(cell);
                    var parent = _pickupParent != null ? _pickupParent : transform;
                    var go = UnityEngine.Object.Instantiate(_pickupPrefab, world, Quaternion.identity, parent);
                    go.name = $"PackedPickup_{room.GridPosition.x}_{room.GridPosition.y}_{i}";

                    var pickup = go.GetComponent<PackedItemPickup>();
                    if (pickup != null)
                        pickup.SetReference(pref);
                    _spawned.Add(go);
                }
            }
        }

        void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    UnityEngine.Object.Destroy(_spawned[i]);
            }
            _spawned.Clear();
            _maskCache.Clear();
        }

        bool TryPickReference(out PackedItemReference r)
        {
            r = default;
            if (_referencePool != null && _referencePool.Length > 0)
            {
                r = _referencePool[UnityEngine.Random.Range(0, _referencePool.Length)];
                return true;
            }

            var bootstrap = ItemTableBootstrap.Instance;
            if (bootstrap == null || bootstrap.Table == null) return false;
            return bootstrap.Table.TryPickRandomReference(out r);
        }
    }
}
