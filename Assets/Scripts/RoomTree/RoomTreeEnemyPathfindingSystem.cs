using System;
using System.Collections.Generic;
using MBAG.Pathfinding;
using UnityEngine;

namespace WFC
{
    [DisallowMultipleComponent]
    public class RoomTreeEnemyPathfindingSystem : MonoBehaviour
    {
        [SerializeField] private RoomTreeDungeonComponent _dungeon;
        [SerializeField] private Grid _grid;
        [Min(0)]
        [SerializeField] private int _enemiesPerRoom = 2;
        [Tooltip("When > 0, applied to NPC on each spawn. 0 = do not override move speed.")]
        [Min(0f)]
        [SerializeField] private float _pathNpcMoveSpeed;

        [Header("NPC Types")]
        [SerializeField] private NPCSpawner.NPCPrefabEntry[] _npcPrefabs;
        [SerializeField] private NPCSpawnWeight[]  _spawnWeights;

        [Header("Off-room idle (enemies in streamed neighbors)")]
        [Min(0.05f)]
        [SerializeField] private float _idleWanderMinInterval = 0.6f;
        [Min(0.05f)]
        [SerializeField] private float _idleWanderMaxInterval = 1.6f;

        [Serializable]
        public struct NPCSpawnWeight
        {
            public NPCType type;
            [Min(0f)] public float weight;
        }

        private sealed class RoomEnemyState
        {
            public readonly List<NPC>     Enemies        = new List<NPC>();
            public readonly List<Vector3> SavedPositions = new List<Vector3>();
        }

        // Spawn weight lookup — precomputed in Awake, zero alloc per spawn.
        private NPCType[] _weightedTypes;
        private float[]   _cumulativeWeights;
        private Dictionary<NPCType, GameObject> _prefabLookup;

        // Pathfinding
        private readonly RoomWalkMaskCache                    _cache      = new RoomWalkMaskCache();
        private readonly Dictionary<Vector2Int, RoomEnemyState> _roomEnemies = new Dictionary<Vector2Int, RoomEnemyState>();
        private readonly int[] _dist            = new int[RoomBitGrid64.CellCount];
        private readonly int[] _distIdleWander  = new int[RoomBitGrid64.CellCount];

        // Occupancy — per-room bitboard, per-NPC tracking.
        private readonly Dictionary<Vector2Int, Bitboard> _roomOccupancy    = new Dictionary<Vector2Int, Bitboard>();
        private readonly Dictionary<NPC, int>             _npcOccupancyIdx  = new Dictionary<NPC, int>();
        private readonly Dictionary<NPC, Vector2Int>      _npcRoomKey       = new Dictionary<NPC, Vector2Int>();
        private readonly Dictionary<NPC, int>             _jumperPendingDest = new Dictionary<NPC, int>();

        // Idle wander cooldown (walkers only — jumpers use ConsumeMovementSteps).
        private readonly Dictionary<NPC, float> _nextIdleWanderTime = new Dictionary<NPC, float>();

        // Room tracking — List mirrors HashSet so idle loop avoids enumerator boxing.
        private readonly HashSet<Vector2Int> _activeRoomKeys    = new HashSet<Vector2Int>();
        private readonly List<Vector2Int>    _activeRoomKeyList = new List<Vector2Int>();

        private RoomTreeNode _currentRoom;
        private Vector2Int   _lastRoomKey   = new Vector2Int(int.MinValue, int.MinValue);
        private int          _lastGoalBit   = -2;
        private int          _lastLayoutVer = -1;

        // ─── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (_dungeon == null)
                _dungeon = GetComponent<RoomTreeDungeonComponent>();

            BuildPrefabLookup();
            BuildWeightTable();
        }

        private void OnEnable()  => RoomTreeDungeonComponent.OnRoomTreeGenerated += OnDungeonGenerated;
        private void OnDisable() => RoomTreeDungeonComponent.OnRoomTreeGenerated -= OnDungeonGenerated;

        private void Start()
        {
            if (_grid == null && _dungeon != null)
                _grid = _dungeon.DungeonGrid;

            if (_dungeon?.Generator?.Nodes != null)
                OnDungeonGenerated(_dungeon);
        }

        private void Update()
        {
            if (_dungeon == null || _dungeon.Generator == null || _grid == null || _prefabLookup.Count == 0)
                return;

            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null)
                return;

            Vector3Int playerCell = _grid.WorldToCell(playerGo.transform.position);
            RoomTreeNode playerRoom = RoomTreeGrid.FindRoomContainingCell(_dungeon.Generator, playerCell);
            UpdateCurrentRoom(playerRoom, playerCell);

            if (_currentRoom != null)
                MoveActiveRoomEnemies(playerCell);

            MoveIdleEnemies();
        }

        // ─── Dungeon events ───────────────────────────────────────────────────

        private void OnDungeonGenerated(RoomTreeDungeonComponent dungeon)
        {
            _dungeon = dungeon;
            if (_grid == null && _dungeon != null)
                _grid = _dungeon.DungeonGrid;

            _cache.Clear();
            ClearAllEnemies();
            _currentRoom = null;
            _activeRoomKeys.Clear();
            _activeRoomKeyList.Clear();
            ResetDistanceCache();

            if (_dungeon?.Generator?.Nodes != null)
                _cache.WarmNeighbors(_dungeon.LayoutVersion, _dungeon.Generator.Nodes.Values);
        }

        // ─── Room streaming ───────────────────────────────────────────────────

        private void UpdateCurrentRoom(RoomTreeNode playerRoom, Vector3Int playerCell)
        {
            _currentRoom = playerRoom;
            SyncActiveRoomsAroundPlayer(playerCell);
            ResetDistanceCache();
        }

        private void SyncActiveRoomsAroundPlayer(Vector3Int playerCell)
        {
            if (_dungeon?.Generator?.Nodes == null)
                return;

            var desired = new HashSet<Vector2Int>(_dungeon.LoadedRoomKeys);

            foreach (Vector2Int key in _activeRoomKeys)
            {
                if (!desired.Contains(key))
                    DeactivateRoomEnemies(key);
            }

            foreach (Vector2Int key in desired)
            {
                if (_activeRoomKeys.Contains(key))
                    continue;

                if (_dungeon.Generator.Nodes.TryGetValue(key, out RoomTreeNode room) && room != null)
                    ActivateOrSpawnRoomEnemies(room, playerCell);
            }

            _activeRoomKeys.Clear();
            _activeRoomKeyList.Clear();
            foreach (Vector2Int key in desired)
            {
                _activeRoomKeys.Add(key);
                _activeRoomKeyList.Add(key);
            }
        }

        private void ActivateOrSpawnRoomEnemies(RoomTreeNode room, Vector3Int playerCell)
        {
            RoomEnemyState state = GetOrCreateRoomState(room.GridPosition);

            if (state.Enemies.Count == 0)
            {
                SpawnRoomEnemies(room, state, playerCell);
                return;
            }

            // Reactivate — restore positions and rebuild occupancy.
            Bitboard occ = new Bitboard();
            Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(room.WorldPosition);
            int ver = _dungeon.LayoutVersion;
            ulong mask = _cache.GetOrBuild(room.GridPosition, ver, room.TileData);

            for (int i = 0; i < state.Enemies.Count; i++)
            {
                NPC npc = state.Enemies[i];
                if (npc == null) continue;

                Vector3 savedPos = i < state.SavedPositions.Count ? state.SavedPositions[i] : npc.transform.position;
                npc.transform.position = savedPos;
                npc.gameObject.SetActive(true);

                Vector3Int cell = _grid.WorldToCell(savedPos);
                if (RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, cell, out int bit) &&
                    RoomBitGrid64.IsWalkable(mask, bit))
                {
                    occ.TryLock(bit);
                    _npcOccupancyIdx[npc] = bit;
                }
            }

            _roomOccupancy[room.GridPosition] = occ;
        }

        private void DeactivateRoomEnemies(Vector2Int roomKey)
        {
            if (!_roomEnemies.TryGetValue(roomKey, out RoomEnemyState state))
                return;

            for (int i = 0; i < state.Enemies.Count; i++)
            {
                NPC npc = state.Enemies[i];
                if (npc == null) continue;

                while (state.SavedPositions.Count <= i)
                    state.SavedPositions.Add(Vector3.zero);

                state.SavedPositions[i] = npc.transform.position;
                npc.ClearTarget();
                _nextIdleWanderTime.Remove(npc);

                if (npc is Jumper jumper)
                    jumper.Arrived -= OnNPCArrived;

                npc.gameObject.SetActive(false);
            }

            _roomOccupancy.Remove(roomKey);
        }

        private void SpawnRoomEnemies(RoomTreeNode room, RoomEnemyState state, Vector3Int playerCell)
        {
            if (room?.TileData == null || _grid == null)
                return;

            Vector2Int roomKey = room.GridPosition;
            ulong mask = _cache.GetOrBuild(roomKey, _dungeon.LayoutVersion, room.TileData);
            Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(room.WorldPosition);
            RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, playerCell, out int playerBit);

            List<int> spawnBits = PickSpawnBits(mask, playerBit, _enemiesPerRoom);
            Bitboard occ = new Bitboard();

            for (int i = 0; i < spawnBits.Count; i++)
            {
                int spawnBit = spawnBits[i];
                NPCType type = PickWeightedType();

                if (!_prefabLookup.TryGetValue(type, out GameObject prefab) || prefab == null)
                {
                    Debug.LogWarning($"RoomTreeEnemyPathfindingSystem: no prefab for NPCType.{type}.");
                    continue;
                }

                Vector3Int spawnCell = RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, spawnBit);
                Vector3 world = _grid.GetCellCenterWorld(spawnCell);

                GameObject go = Instantiate(prefab, world, Quaternion.identity, transform);
                go.name = $"RoomEnemy_{roomKey.x}_{roomKey.y}_{i}";

                foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                {
                    if (mb is BaseEnemy) mb.enabled = false;
                }

                NPC npc = go.GetComponent<NPC>() ?? go.AddComponent<NPC>();

                if (_pathNpcMoveSpeed > 0f)
                    npc.SetMoveSpeedForPathfinding(_pathNpcMoveSpeed);

                occ.TryLock(spawnBit);
                _npcOccupancyIdx[npc] = spawnBit;
                _npcRoomKey[npc]      = roomKey;

                if (npc is Jumper jumper)
                    jumper.Arrived += OnNPCArrived;

                state.Enemies.Add(npc);
                state.SavedPositions.Add(world);
            }

            _roomOccupancy[roomKey] = occ;
        }

        // ─── Active room movement ─────────────────────────────────────────────

        private void MoveActiveRoomEnemies(Vector3Int playerCell)
        {
            if (_currentRoom?.TileData == null)
                return;

            Vector2Int roomKey = _currentRoom.GridPosition;
            if (!_roomEnemies.TryGetValue(roomKey, out RoomEnemyState state))
                return;

            Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(_currentRoom.WorldPosition);
            if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, playerCell, out int rawGoalBit))
                return;

            int ver = _dungeon.LayoutVersion;
            ulong mask = _cache.GetOrBuild(roomKey, ver, _currentRoom.TileData);
            int goalBit = RoomBitGrid64.NearestWalkable(mask, rawGoalBit);
            if (goalBit < 0) return;

            if (NeedsDistanceRebuild(ver, roomKey, goalBit))
            {
                RoomBitGrid64.ComputeDistancesFromGoal(mask, goalBit, _dist);
                _lastLayoutVer = ver;
                _lastRoomKey   = roomKey;
                _lastGoalBit   = goalBit;
            }

            for (int i = 0; i < state.Enemies.Count; i++)
            {
                NPC npc = state.Enemies[i];
                if (npc == null || !npc.gameObject.activeInHierarchy || npc.HasTarget()) continue;

                Vector3Int npcCell = _grid.WorldToCell(npc.transform.position);
                if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, npcCell, out int npcBit)) continue;
                _npcOccupancyIdx[npc] = npcBit;

                _ = npc is Jumper j
                    ? TryMoveActiveJumper(j, mask, roomKey, npcBit, origin)
                    : TryMoveActiveWalker(npc, mask, roomKey, npcBit, origin);
            }
        }

        private bool TryMoveActiveWalker(NPC npc, ulong mask, Vector2Int roomKey, int npcBit, Vector3Int origin)
        {
            if (!RoomBitGrid64.TryGreedyTowardGoal(mask, _dist, npcBit, out int nextBit)) return false;

            Bitboard occ = _roomOccupancy[roomKey];
            if (!occ.TryTransfer(npcBit, nextBit)) return false;
            _roomOccupancy[roomKey] = occ;

            _npcOccupancyIdx[npc] = nextBit;
            npc.SetMoveTarget(_grid.GetCellCenterWorld(RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, nextBit)));
            return true;
        }

        private bool TryMoveActiveJumper(Jumper jumper, ulong mask, Vector2Int roomKey, int npcBit, Vector3Int origin)
        {
            int steps = jumper.ConsumeMovementSteps();
            if (steps == 0) return false;

            // Try the farthest LOS-clear, unoccupied cell from 'steps' down to 1.
            int destBit = -1;
            for (int s = steps; s >= 1; s--)
            {
                if (!RoomBitGrid64.TryAdvanceAlongPath(mask, _dist, npcBit, s, out int candidate)) continue;
                if (!RoomBitGrid64.HasLineOfSight(mask, npcBit, candidate)) continue;
                Bitboard peek = _roomOccupancy[roomKey];
                if (peek.IsSet(candidate)) continue;
                destBit = candidate;
                break;
            }
            if (destBit < 0) return false;

            Bitboard occ = _roomOccupancy[roomKey];
            if (!occ.TryLock(destBit)) return false;
            _roomOccupancy[roomKey] = occ;

            _jumperPendingDest[jumper] = destBit;
            jumper.SetMoveTarget(_grid.GetCellCenterWorld(RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, destBit)));
            return true;
        }

        // ─── Idle movement (non-player rooms) ────────────────────────────────

        private void MoveIdleEnemies()
        {
            if (_dungeon?.Generator?.Nodes == null || _grid == null)
                return;

            int ver = _dungeon.LayoutVersion;
            float minT = Mathf.Min(_idleWanderMinInterval, _idleWanderMaxInterval);
            float maxT = Mathf.Max(_idleWanderMinInterval, _idleWanderMaxInterval);

            for (int k = 0; k < _activeRoomKeyList.Count; k++)
            {
                Vector2Int key = _activeRoomKeyList[k];
                if (_currentRoom != null && key == _currentRoom.GridPosition) continue;
                if (!_roomEnemies.TryGetValue(key, out RoomEnemyState state)) continue;
                if (!_dungeon.Generator.Nodes.TryGetValue(key, out RoomTreeNode room) || room?.TileData == null) continue;

                ulong mask = _cache.GetOrBuild(key, ver, room.TileData);
                Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(room.WorldPosition);

                for (int i = 0; i < state.Enemies.Count; i++)
                {
                    NPC npc = state.Enemies[i];
                    if (npc == null || !npc.gameObject.activeInHierarchy || npc.HasTarget()) continue;

                    Vector3Int npcCell = _grid.WorldToCell(npc.transform.position);
                    if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, npcCell, out int npcBit)) continue;
                    _npcOccupancyIdx[npc] = npcBit;

                    _ = npc is Jumper j
                        ? TryMoveIdleJumper(j, mask, key, npcBit, origin)
                        : TryMoveIdleWalker(npc, mask, key, npcBit, origin, minT, maxT);
                }
            }
        }

        private bool TryMoveIdleWalker(NPC npc, ulong mask, Vector2Int roomKey, int npcBit, Vector3Int origin, float minT, float maxT)
        {
            if (_nextIdleWanderTime.TryGetValue(npc, out float nextTime) && Time.time < nextTime) return false;

            int goalBit = PickRandomWalkableBit(mask);
            if (goalBit < 0) { _nextIdleWanderTime[npc] = Time.time + UnityEngine.Random.Range(minT, maxT); return false; }

            RoomBitGrid64.ComputeDistancesFromGoal(mask, goalBit, _distIdleWander);
            if (!RoomBitGrid64.TryGreedyTowardGoal(mask, _distIdleWander, npcBit, out int nextBit))
            {
                _nextIdleWanderTime[npc] = Time.time + UnityEngine.Random.Range(minT, maxT);
                return false;
            }

            Bitboard occ = _roomOccupancy[roomKey];
            if (!occ.TryTransfer(npcBit, nextBit)) return false;
            _roomOccupancy[roomKey] = occ;

            _npcOccupancyIdx[npc] = nextBit;
            npc.SetMoveTarget(_grid.GetCellCenterWorld(RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, nextBit)));
            _nextIdleWanderTime[npc] = Time.time + UnityEngine.Random.Range(minT, maxT);
            return true;
        }

        private bool TryMoveIdleJumper(Jumper jumper, ulong mask, Vector2Int roomKey, int npcBit, Vector3Int origin)
        {
            if (jumper.ConsumeMovementSteps() == 0) return false;

            int goalBit = PickRandomWalkableBit(mask);
            if (goalBit < 0) return false;

            RoomBitGrid64.ComputeDistancesFromGoal(mask, goalBit, _distIdleWander);

            // Try farthest LOS-clear, unoccupied cell from JumpDistance down to 1.
            int destBit = -1;
            for (int s = jumper.JumpDistance; s >= 1; s--)
            {
                if (!RoomBitGrid64.TryAdvanceAlongPath(mask, _distIdleWander, npcBit, s, out int candidate)) continue;
                if (!RoomBitGrid64.HasLineOfSight(mask, npcBit, candidate)) continue;
                Bitboard peek = _roomOccupancy[roomKey];
                if (peek.IsSet(candidate)) continue;
                destBit = candidate;
                break;
            }
            if (destBit < 0) return false;

            Bitboard occ = _roomOccupancy[roomKey];
            if (!occ.TryLock(destBit)) return false;
            _roomOccupancy[roomKey] = occ;

            _jumperPendingDest[jumper] = destBit;
            jumper.SetMoveTarget(_grid.GetCellCenterWorld(RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, destBit)));
            return true;
        }

        // ─── Arrival handler ──────────────────────────────────────────────────

        private void OnNPCArrived(NPC npc)
        {
            if (!_jumperPendingDest.TryGetValue(npc, out int destIdx)) return;

            if (_npcRoomKey.TryGetValue(npc, out Vector2Int roomKey) &&
                _roomOccupancy.TryGetValue(roomKey, out Bitboard occ))
            {
                occ.Unlock(_npcOccupancyIdx[npc]);
                _roomOccupancy[roomKey] = occ;
            }

            _npcOccupancyIdx[npc] = destIdx;
            _jumperPendingDest.Remove(npc);
            npc.RecordLanding();
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────

        private void ClearAllEnemies()
        {
            foreach (RoomEnemyState state in _roomEnemies.Values)
            {
                for (int i = 0; i < state.Enemies.Count; i++)
                {
                    NPC npc = state.Enemies[i];
                    if (npc == null) continue;
                    if (npc is Jumper j) j.Arrived -= OnNPCArrived;
                    DestroyGameObject(npc.gameObject);
                }
            }

            _roomEnemies.Clear();
            _roomOccupancy.Clear();
            _npcOccupancyIdx.Clear();
            _npcRoomKey.Clear();
            _jumperPendingDest.Clear();
            _nextIdleWanderTime.Clear();
        }

        // ─── Spawn helpers ────────────────────────────────────────────────────

        private NPCType PickWeightedType()
        {
            float roll = UnityEngine.Random.value;
            for (int i = 0; i < _cumulativeWeights.Length; i++)
            {
                if (roll <= _cumulativeWeights[i])
                    return _weightedTypes[i];
            }
            return _weightedTypes[_weightedTypes.Length - 1];
        }

        private void BuildPrefabLookup()
        {
            _prefabLookup = new Dictionary<NPCType, GameObject>(_npcPrefabs?.Length ?? 0);
            if (_npcPrefabs == null) return;

            foreach (NPCSpawner.NPCPrefabEntry entry in _npcPrefabs)
            {
                if (!_prefabLookup.ContainsKey(entry.type))
                    _prefabLookup[entry.type] = entry.prefab;
                else
                    Debug.LogWarning($"RoomTreeEnemyPathfindingSystem: duplicate prefab entry for NPCType.{entry.type}.");
            }
        }

        private void BuildWeightTable()
        {
            int count = _spawnWeights?.Length ?? 0;
            _weightedTypes      = new NPCType[count];
            _cumulativeWeights  = new float[count];

            if (count == 0) return;

            float total = 0f;
            for (int i = 0; i < count; i++)
                total += Mathf.Max(0f, _spawnWeights[i].weight);

            float cumulative = 0f;
            for (int i = 0; i < count; i++)
            {
                _weightedTypes[i]     = _spawnWeights[i].type;
                cumulative           += total > 0f ? Mathf.Max(0f, _spawnWeights[i].weight) / total : 1f / count;
                _cumulativeWeights[i] = cumulative;
            }
        }

        private static int PickRandomWalkableBit(ulong mask)
        {
            for (int k = 0; k < 40; k++)
            {
                int b = UnityEngine.Random.Range(0, RoomBitGrid64.CellCount);
                if (RoomBitGrid64.IsWalkable(mask, b)) return b;
            }

            for (int i = 0; i < RoomBitGrid64.CellCount; i++)
            {
                if (RoomBitGrid64.IsWalkable(mask, i)) return i;
            }

            return -1;
        }

        private List<int> PickSpawnBits(ulong mask, int playerBit, int count)
        {
            var candidates = new List<int>(RoomBitGrid64.CellCount);
            for (int i = 0; i < RoomBitGrid64.CellCount; i++)
            {
                if (!RoomBitGrid64.IsWalkable(mask, i) || i == playerBit) continue;
                candidates.Add(i);
            }

            Shuffle(candidates);
            int take = Mathf.Min(count, candidates.Count);
            if (take < candidates.Count)
                candidates.RemoveRange(take, candidates.Count - take);

            return candidates;
        }

        // ─── Distance cache ───────────────────────────────────────────────────

        private bool NeedsDistanceRebuild(int version, Vector2Int roomKey, int goalBit) =>
            version != _lastLayoutVer || roomKey != _lastRoomKey || goalBit != _lastGoalBit;

        private void ResetDistanceCache()
        {
            _lastLayoutVer = -1;
            _lastRoomKey   = new Vector2Int(int.MinValue, int.MinValue);
            _lastGoalBit   = -2;
        }

        private RoomEnemyState GetOrCreateRoomState(Vector2Int roomKey)
        {
            if (_roomEnemies.TryGetValue(roomKey, out RoomEnemyState state)) return state;
            state = new RoomEnemyState();
            _roomEnemies[roomKey] = state;
            return state;
        }

        // ─── Utilities ────────────────────────────────────────────────────────

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void DestroyGameObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }
}
