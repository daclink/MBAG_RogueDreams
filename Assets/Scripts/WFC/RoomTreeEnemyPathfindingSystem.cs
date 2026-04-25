using System.Collections.Generic;
using MBAG.Pathfinding;
using UnityEngine;

namespace WFC
{
    /// <summary>
    /// Room-tree enemy controller: spawns/pools room-local NPCs and steers the active room's enemies
    /// toward the Player using the cached 8x8 room walk mask.
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomTreeEnemyPathfindingSystem : MonoBehaviour
    {
        [SerializeField] private RoomTreeDungeonComponent _dungeon;
        [SerializeField] private Grid _grid;
        [SerializeField] private GameObject _npcPrefab;
        [Min(0)]
        [SerializeField] private int _enemiesPerRoom = 2;
        [Tooltip("When > 0, applied to NPC on each spawn. Use with prefabs that need NPC added at runtime (e.g. Melee_Enemy). 0 = do not override move speed.")]
        [Min(0f)]
        [SerializeField] private float _pathNpcMoveSpeed;

        [Header("Off-room idle (enemies in streamed neighbors)")]
        [Tooltip("When not in the player’s room, enemies pick random walkable cells; min time between new wander targets per NPC.")]
        [Min(0.05f)]
        [SerializeField] private float _idleWanderMinInterval = 0.6f;
        [Tooltip("Max time between wander retargets when the previous path finished.")]
        [Min(0.05f)]
        [SerializeField] private float _idleWanderMaxInterval = 1.6f;

        private readonly Dictionary<NPC, float> _nextIdleWanderTime = new Dictionary<NPC, float>();

        private sealed class RoomEnemyState
        {
            public readonly List<NPC> Enemies = new List<NPC>();
            public readonly List<Vector3> SavedPositions = new List<Vector3>();
        }

        private readonly RoomWalkMaskCache _cache = new RoomWalkMaskCache();
        private readonly Dictionary<Vector2Int, RoomEnemyState> _roomEnemies =
            new Dictionary<Vector2Int, RoomEnemyState>();
        private readonly int[] _dist = new int[RoomBitGrid64.CellCount];
        private readonly int[] _distIdleWander = new int[RoomBitGrid64.CellCount];

        private RoomTreeNode _currentRoom;
        private Vector2Int _lastRoomKey = new Vector2Int(int.MinValue, int.MinValue);
        private int _lastGoalBit = -2;
        private int _lastLayoutVer = -1;
        private readonly HashSet<Vector2Int> _activeRoomKeys = new HashSet<Vector2Int>();

        private void Awake()
        {
            if (_dungeon == null)
                _dungeon = GetComponent<RoomTreeDungeonComponent>();
        }

        private void OnEnable()
        {
            RoomTreeDungeonComponent.OnRoomTreeGenerated += OnDungeonGenerated;
        }

        private void OnDisable()
        {
            RoomTreeDungeonComponent.OnRoomTreeGenerated -= OnDungeonGenerated;
        }

        private void Start()
        {
            if (_grid == null && _dungeon != null)
                _grid = _dungeon.DungeonGrid;

            if (_dungeon?.Generator?.Nodes != null)
                OnDungeonGenerated(_dungeon);
        }

        private void Update()
        {
            if (_dungeon == null || _dungeon.Generator == null || _grid == null || _npcPrefab == null)
                return;

            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null)
                return;

            Vector3Int playerCell = _grid.WorldToCell(playerGo.transform.position);
            RoomTreeNode playerRoom = FindRoomContainingCell(playerCell);
            UpdateCurrentRoom(playerRoom, playerCell);

            if (_currentRoom != null)
                MoveActiveRoomEnemies(playerCell);

            MoveStreamedEnemiesIdleOutsidePlayerRoom();
        }

        private void OnDungeonGenerated(RoomTreeDungeonComponent dungeon)
        {
            _dungeon = dungeon;
            if (_grid == null && _dungeon != null)
                _grid = _dungeon.DungeonGrid;

            _cache.Clear();
            ClearAllEnemies();
            _currentRoom = null;
            _activeRoomKeys.Clear();
            ResetDistanceCache();

            if (_dungeon?.Generator?.Nodes != null)
                _cache.WarmNeighbors(_dungeon.LayoutVersion, _dungeon.Generator.Nodes.Values);
        }

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

            // Deactivate rooms that fell out of the streamed set.
            foreach (Vector2Int key in _activeRoomKeys)
            {
                if (!desired.Contains(key))
                    DeactivateRoomEnemies(key);
            }

            // Activate/spawn rooms that entered the streamed set.
            foreach (Vector2Int key in desired)
            {
                if (_activeRoomKeys.Contains(key))
                    continue;

                if (_dungeon.Generator.Nodes.TryGetValue(key, out RoomTreeNode room) && room != null)
                    ActivateOrSpawnRoomEnemies(room, playerCell);
            }

            _activeRoomKeys.Clear();
            foreach (Vector2Int key in desired)
                _activeRoomKeys.Add(key);
        }

        private void ActivateOrSpawnRoomEnemies(RoomTreeNode room, Vector3Int playerCell)
        {
            RoomEnemyState state = GetOrCreateRoomState(room.GridPosition);
            if (state.Enemies.Count == 0)
                SpawnRoomEnemies(room, state, playerCell);

            for (int i = 0; i < state.Enemies.Count; i++)
            {
                NPC npc = state.Enemies[i];
                if (npc == null) continue;

                if (i < state.SavedPositions.Count)
                    npc.transform.position = state.SavedPositions[i];

                npc.gameObject.SetActive(true);
            }
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
                npc.gameObject.SetActive(false);
            }
        }

        private void SpawnRoomEnemies(RoomTreeNode room, RoomEnemyState state, Vector3Int playerCell)
        {
            if (room?.TileData == null || _npcPrefab == null || _grid == null)
                return;

            ulong mask = _cache.GetOrBuild(room.GridPosition, _dungeon.LayoutVersion, room.TileData);
            Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(room.WorldPosition);
            RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, playerCell, out int playerBit);

            List<int> spawnBits = PickSpawnBits(mask, playerBit, _enemiesPerRoom);
            for (int i = 0; i < spawnBits.Count; i++)
            {
                Vector3Int spawnCell = RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, spawnBits[i]);
                Vector3 world = _grid.GetCellCenterWorld(spawnCell);

                GameObject go = Instantiate(_npcPrefab, world, Quaternion.identity, transform);
                go.name = $"RoomEnemy_{room.GridPosition.x}_{room.GridPosition.y}_{i}";

                // Melee (and other BaseEnemy) prefabs use their own AI; we drive movement with NPC instead.
                foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
                {
                    if (mb is BaseEnemy) mb.enabled = false;
                }

                NPC npc = go.GetComponent<NPC>();
                if (npc == null)
                    npc = go.AddComponent<NPC>();

                if (_pathNpcMoveSpeed > 0f)
                    npc.SetMoveSpeedForPathfinding(_pathNpcMoveSpeed);

                // Disable the legacy demo target so this system fully owns room-local movement.
                npc.debugMove = Vector2.zero;
                state.Enemies.Add(npc);
                state.SavedPositions.Add(world);
            }
        }

        private void MoveActiveRoomEnemies(Vector3Int playerCell)
        {
            if (_currentRoom?.TileData == null)
                return;
            if (!_roomEnemies.TryGetValue(_currentRoom.GridPosition, out RoomEnemyState state))
                return;

            Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(_currentRoom.WorldPosition);
            if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, playerCell, out int rawGoalBit))
                return;

            int ver = _dungeon.LayoutVersion;
            ulong mask = _cache.GetOrBuild(_currentRoom.GridPosition, ver, _currentRoom.TileData);
            int goalBit = RoomBitGrid64.NearestWalkable(mask, rawGoalBit);
            if (goalBit < 0)
                return;

            if (NeedsDistanceRebuild(ver, _currentRoom.GridPosition, goalBit))
            {
                RoomBitGrid64.ComputeDistancesFromGoal(mask, goalBit, _dist);
                _lastLayoutVer = ver;
                _lastRoomKey = _currentRoom.GridPosition;
                _lastGoalBit = goalBit;
            }

            for (int i = 0; i < state.Enemies.Count; i++)
            {
                NPC npc = state.Enemies[i];
                if (npc == null || !npc.gameObject.activeInHierarchy)
                    continue;

                Vector3Int npcCell = _grid.WorldToCell(npc.transform.position);
                if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, npcCell, out int npcBit))
                    continue;

                if (RoomBitGrid64.TryGreedyTowardGoal(mask, _dist, npcBit, out int nextBit))
                {
                    Vector3Int nextCell = RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, nextBit);
                    Vector3 world = _grid.GetCellCenterWorld(nextCell);
                    npc.SetMoveTarget(new Vector2(world.x, world.y));
                }
            }
        }

        /// <summary>
        /// Enemies in streamed rooms other than the player’s use random walkable targets (same mask + greedy steps as chase).
        /// </summary>
        private void MoveStreamedEnemiesIdleOutsidePlayerRoom()
        {
            if (_dungeon?.Generator?.Nodes == null || _grid == null)
                return;

            int ver = _dungeon.LayoutVersion;
            float minT = Mathf.Min(_idleWanderMinInterval, _idleWanderMaxInterval);
            float maxT = Mathf.Max(_idleWanderMinInterval, _idleWanderMaxInterval);

            foreach (Vector2Int key in _activeRoomKeys)
            {
                if (_currentRoom != null && key == _currentRoom.GridPosition)
                    continue;

                if (!_roomEnemies.TryGetValue(key, out RoomEnemyState state))
                    continue;
                if (!_dungeon.Generator.Nodes.TryGetValue(key, out RoomTreeNode room) || room?.TileData == null)
                    continue;

                ulong mask = _cache.GetOrBuild(key, ver, room.TileData);
                Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(room.WorldPosition);

                for (int i = 0; i < state.Enemies.Count; i++)
                {
                    NPC npc = state.Enemies[i];
                    if (npc == null || !npc.gameObject.activeInHierarchy)
                        continue;
                    if (npc.HasTarget())
                        continue;

                    if (_nextIdleWanderTime.TryGetValue(npc, out float nextTime) && Time.time < nextTime)
                        continue;

                    int goalBit = PickRandomWalkableBit(mask);
                    if (goalBit < 0)
                        continue;

                    RoomBitGrid64.ComputeDistancesFromGoal(mask, goalBit, _distIdleWander);
                    Vector3Int npcCell = _grid.WorldToCell(npc.transform.position);
                    if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, npcCell, out int npcBit))
                    {
                        _nextIdleWanderTime[npc] = Time.time + Random.Range(minT, maxT);
                        continue;
                    }

                    if (!RoomBitGrid64.TryGreedyTowardGoal(mask, _distIdleWander, npcBit, out int nextBit))
                    {
                        _nextIdleWanderTime[npc] = Time.time + Random.Range(minT, maxT);
                        continue;
                    }

                    Vector3Int nextCell = RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, nextBit);
                    Vector3 world = _grid.GetCellCenterWorld(nextCell);
                    npc.SetMoveTarget(new Vector2(world.x, world.y));
                    _nextIdleWanderTime[npc] = Time.time + Random.Range(minT, maxT);
                }
            }
        }

        private static int PickRandomWalkableBit(ulong mask)
        {
            for (int k = 0; k < 40; k++)
            {
                int b = Random.Range(0, RoomBitGrid64.CellCount);
                if (RoomBitGrid64.IsWalkable(mask, b))
                    return b;
            }

            for (int i = 0; i < RoomBitGrid64.CellCount; i++)
            {
                if (RoomBitGrid64.IsWalkable(mask, i))
                    return i;
            }

            return -1;
        }

        private List<int> PickSpawnBits(ulong mask, int playerBit, int count)
        {
            var candidates = new List<int>(RoomBitGrid64.CellCount);
            for (int i = 0; i < RoomBitGrid64.CellCount; i++)
            {
                if (!RoomBitGrid64.IsWalkable(mask, i)) continue;
                if (i == playerBit) continue;
                candidates.Add(i);
            }

            Shuffle(candidates);
            int take = Mathf.Min(count, candidates.Count);
            if (take < candidates.Count)
                candidates.RemoveRange(take, candidates.Count - take);

            return candidates;
        }

        private RoomEnemyState GetOrCreateRoomState(Vector2Int roomKey)
        {
            if (_roomEnemies.TryGetValue(roomKey, out RoomEnemyState state))
                return state;

            state = new RoomEnemyState();
            _roomEnemies[roomKey] = state;
            return state;
        }

        private void ClearAllEnemies()
        {
            _nextIdleWanderTime.Clear();
            foreach (RoomEnemyState state in _roomEnemies.Values)
            {
                foreach (NPC npc in state.Enemies)
                {
                    if (npc != null)
                        DestroyGameObject(npc.gameObject);
                }
            }

            _roomEnemies.Clear();
        }

        private RoomTreeNode FindRoomContainingCell(Vector3Int cell)
        {
            if (_dungeon?.Generator?.Nodes == null) return null;
            foreach (RoomTreeNode n in _dungeon.Generator.Nodes.Values)
            {
                if (n == null) continue;
                Vector3Int o = RoomWalkMaskBuilder.RoomOriginCell(n.WorldPosition);
                if (cell.x >= o.x && cell.x < o.x + RoomTreeLayout.RoomSize &&
                    cell.y >= o.y && cell.y < o.y + RoomTreeLayout.RoomSize)
                    return n;
            }

            return null;
        }

        private bool NeedsDistanceRebuild(int version, Vector2Int roomKey, int goalBit)
        {
            return version != _lastLayoutVer ||
                   roomKey != _lastRoomKey ||
                   goalBit != _lastGoalBit;
        }

        private void ResetDistanceCache()
        {
            _lastLayoutVer = -1;
            _lastRoomKey = new Vector2Int(int.MinValue, int.MinValue);
            _lastGoalBit = -2;
        }

        private static bool SameRoom(RoomTreeNode a, RoomTreeNode b)
        {
            if (a == null || b == null)
                return a == b;
            return a.GridPosition == b.GridPosition;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void DestroyGameObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }
    }
}
