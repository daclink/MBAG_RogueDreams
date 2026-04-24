using MBAG.Pathfinding;
using UnityEngine;

namespace WFC
{
    /// <summary>
    /// Room-tree demo hook: caches 8×8 walk masks, runs one BFS per player cell change from the player goal,
    /// and steers a single test <see cref="NPC"/> using <see cref="RoomBitGrid64"/> (8-neighbor + corner cutting).
    /// Add to the same GameObject as <see cref="RoomTreeDungeonComponent"/>. Assign <see cref="_testNpcPrefab"/> (must have <see cref="NPC"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomTreeRoomPathfindingDriver : MonoBehaviour
    {
        [SerializeField] private RoomTreeDungeonComponent _dungeon;
        [SerializeField] private Grid _grid;
        [Tooltip("Prefab with NPC component; spawned at start room after generate.")]
        [SerializeField] private GameObject _testNpcPrefab;

        private readonly RoomWalkMaskCache _cache = new RoomWalkMaskCache();
        private readonly int[] _dist = new int[RoomBitGrid64.CellCount];
        private NPC _testNpc;

        private Vector2Int _lastRoomKey = new Vector2Int(int.MinValue, int.MinValue);
        private int _lastGoalBit = -2;
        private int _lastLayoutVer = -1;

        void Awake()
        {
            if (_dungeon == null)
                _dungeon = GetComponent<RoomTreeDungeonComponent>();
        }

        void OnEnable()
        {
            RoomTreeDungeonComponent.OnRoomTreeGenerated += OnDungeonGenerated;
        }

        void OnDisable()
        {
            RoomTreeDungeonComponent.OnRoomTreeGenerated -= OnDungeonGenerated;
        }

        void Start()
        {
            if (_grid == null && _dungeon != null)
                _grid = _dungeon.DungeonGrid;
            if (_dungeon != null && _dungeon.Generator != null && _dungeon.Generator.Nodes != null &&
                _dungeon.Generator.Nodes.Count > 0)
                OnDungeonGenerated(_dungeon);
        }

        void Update()
        {
            if (_dungeon == null || _dungeon.Generator == null || _grid == null || _testNpc == null)
                return;

            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null)
                return;

            Vector3Int playerCell = _grid.WorldToCell(playerGo.transform.position);
            RoomTreeNode playerRoom = FindRoomContainingCell(playerCell);
            if (playerRoom == null)
                return;

            Vector3Int npcCell = _grid.WorldToCell(_testNpc.transform.position);
            RoomTreeNode npcRoom = FindRoomContainingCell(npcCell);
            if (npcRoom == null)
                return;

            // Room-local pathing: only chase when player and NPC share the same room (demo / v1).
            if (playerRoom.GridPosition != npcRoom.GridPosition)
                return;

            RoomTreeNode room = playerRoom;
            Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(room.WorldPosition);
            if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, playerCell, out int rawGoalBit))
                return;

            int ver = _dungeon.LayoutVersion;
            ulong mask = _cache.GetOrBuild(room.GridPosition, ver, room.TileData);

            int goalBit = RoomBitGrid64.NearestWalkable(mask, rawGoalBit);
            if (goalBit < 0)
                return;

            bool needRebuild = ver != _lastLayoutVer ||
                                room.GridPosition != _lastRoomKey ||
                                goalBit != _lastGoalBit;

            if (needRebuild)
            {
                RoomBitGrid64.ComputeDistancesFromGoal(mask, goalBit, _dist);
                _lastLayoutVer = ver;
                _lastRoomKey = room.GridPosition;
                _lastGoalBit = goalBit;
            }

            if (!RoomWalkMaskBuilder.TryWorldCellToInteriorBitIndex(origin, npcCell, out int npcBit))
                return;

            if (RoomBitGrid64.TryGreedyTowardGoal(mask, _dist, npcBit, out int nextBit))
            {
                Vector3Int nextCell = RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, nextBit);
                Vector3 w = _grid.GetCellCenterWorld(nextCell);
                _testNpc.SetMoveTarget(new Vector2(w.x, w.y));
            }
        }

        private void OnDungeonGenerated(RoomTreeDungeonComponent dungeon)
        {
            _dungeon = dungeon;
            if (_grid == null && _dungeon != null)
                _grid = _dungeon.DungeonGrid;
            if (_dungeon?.Generator?.Nodes == null || _grid == null)
                return;

            _cache.Clear();
            _cache.WarmNeighbors(_dungeon.LayoutVersion, _dungeon.Generator.Nodes.Values);

            DestroyTestNpcChild();
            _lastLayoutVer = -1;
            _lastRoomKey = new Vector2Int(int.MinValue, int.MinValue);
            _lastGoalBit = -2;

            if (_testNpcPrefab == null)
                return;

            RoomTreeNode root = _dungeon.Generator.Root;
            if (root?.TileData == null)
                return;

            ulong mask = _cache.GetOrBuild(root.GridPosition, _dungeon.LayoutVersion, root.TileData);
            int spawnBit = FirstWalkableBit(mask);
            if (spawnBit < 0)
                return;

            Vector3Int origin = RoomWalkMaskBuilder.RoomOriginCell(root.WorldPosition);
            Vector3Int spawnCell = RoomWalkMaskBuilder.InteriorBitIndexToWorldCell(origin, spawnBit);
            Vector3 world = _grid.GetCellCenterWorld(spawnCell);

            GameObject go = Instantiate(_testNpcPrefab, world, Quaternion.identity, transform);
            go.name = "RoomPathTestNpc";
            _testNpc = go.GetComponent<NPC>();
            if (_testNpc == null)
            {
                Destroy(go);
                Debug.LogWarning("RoomTreeRoomPathfindingDriver: prefab needs an NPC component.");
            }
        }

        private static int FirstWalkableBit(ulong mask)
        {
            for (int i = 0; i < RoomBitGrid64.CellCount; i++)
            {
                if (RoomBitGrid64.IsWalkable(mask, i))
                    return i;
            }

            return -1;
        }

        private void DestroyTestNpcChild()
        {
            Transform t = transform.Find("RoomPathTestNpc");
            if (t != null)
                Destroy(t.gameObject);
            _testNpc = null;
        }

        private RoomTreeNode FindRoomContainingCell(Vector3Int cell)
        {
            if (_dungeon?.Generator?.Nodes == null) return null;
            foreach (var n in _dungeon.Generator.Nodes.Values)
            {
                if (n == null) continue;
                var o = RoomWalkMaskBuilder.RoomOriginCell(n.WorldPosition);
                if (cell.x >= o.x && cell.x < o.x + RoomTreeLayout.RoomSize &&
                    cell.y >= o.y && cell.y < o.y + RoomTreeLayout.RoomSize)
                    return n;
            }

            return null;
        }
    }
}
