#if UNITY_EDITOR
using DataSchemas.PackedItem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Tables;
using WFC;

namespace WFC.Editor
{
    public static class WFC_Editor
    {
        private const string WfcTilesPath = "Assets/Art/Tiles/World/WFC_Tiles";
        private const string BiomeTileRegistryPath = "Assets/ScriptableObjects/BiomeTileRegistry.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player_Prefabs/Player_body.prefab";
        private const string SpriteTable2DPath = "Assets/ScriptableObjects/SpriteTable2D.asset";
        private const string TextTable2DPath = "Assets/ScriptableObjects/TextTable2D.asset";
        private const string PackedItemPickupPrefabPath = "Assets/Prefabs/Items/PackedItemPickup.prefab";
        private const string RoomTreeEnemyPrefabPath = "Assets/Prefabs/Enemies/Melee_Enemy.prefab";

        [MenuItem("Tools/WFC/Create New WFC Demo Scene")]
        public static void CreateWFCDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var dungeonRoot = new GameObject("DungeonGeneration");
            var dungeonGen = dungeonRoot.AddComponent<DungeonGeneration>();
            AssignWfcTilesToDungeonGeneration(dungeonGen);

            var dualGridGo = new GameObject("DualGridTilemap");
            dualGridGo.transform.SetParent(dungeonRoot.transform, false);
            var dualGrid = dualGridGo.AddComponent<DualGridTilemap>();

            var so = new SerializedObject(dungeonGen);
            so.FindProperty("dualGridTilemap").objectReferenceValue = dualGrid;
            var biomeRegistry = AssetDatabase.LoadAssetAtPath<BiomeTileRegistry>(BiomeTileRegistryPath);
            if (biomeRegistry != null)
                so.FindProperty("biomeTileRegistry").objectReferenceValue = biomeRegistry;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetupCamera(61, 61, 65); // Center on DungeonGeneration tilemap (10x10 rooms, ~122x122)
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = dungeonRoot;

            EditorUtility.DisplayDialog(
                "WFC Demo Scene Created",
                "DungeonGeneration + DualGrid ready.\n\n" +
                "Enter Play Mode to generate. Camera follows Player (tag); scroll = zoom.\n" +
                "Press G to toggle raw WFC grid visibility.",
                "OK");
        }

        [MenuItem("Tools/WFC/Create Room Tree Demo Scene")]
        public static void CreateRoomTreeDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("RoomTreeDungeon");
            var comp = root.AddComponent<RoomTreeDungeonComponent>();
            AssignWfcTilesToRoomTree(comp);

            // Wire DualGrid + BiomeTileRegistry so Room Tree demo uses canonical hazard tiles.
            var dualGridGo = new GameObject("DualGridTilemap");
            dualGridGo.transform.SetParent(root.transform, false);
            var dualGrid = dualGridGo.AddComponent<DualGridTilemap>();

            var so = new SerializedObject(comp);
            so.FindProperty("dualGridTilemap").objectReferenceValue = dualGrid;
            var biomeRegistry = AssetDatabase.LoadAssetAtPath<BiomeTileRegistry>(BiomeTileRegistryPath);
            if (biomeRegistry != null)
                so.FindProperty("biomeTileRegistry").objectReferenceValue = biomeRegistry;
            var hops = so.FindProperty("streamedNeighborHops");
            if (hops != null) hops.intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            var enemyPathfinding = root.AddComponent<WFC.RoomTreeEnemyPathfindingSystem>();
            var roomEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomTreeEnemyPrefabPath);
            if (roomEnemyPrefab != null)
            {
                var enemyPathfindingSo = new SerializedObject(enemyPathfinding);
                enemyPathfindingSo.FindProperty("_dungeon").objectReferenceValue = comp;
                enemyPathfindingSo.FindProperty("_npcPrefab").objectReferenceValue = roomEnemyPrefab;
                enemyPathfindingSo.FindProperty("_enemiesPerRoom").intValue = 2;
                var psp = enemyPathfindingSo.FindProperty("_pathNpcMoveSpeed");
                if (psp != null) psp.floatValue = 5f;
                enemyPathfindingSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Packed items: load items.dat / texts.dat from Assets/GameData/Items/ (or paths on the component)
            var itemBootstrapGo = new GameObject("ItemTableBootstrap");
            var itemBootstrap = itemBootstrapGo.AddComponent<ItemTableBootstrap>();
            var ibSo = new SerializedObject(itemBootstrap);
            var spriteTable = AssetDatabase.LoadAssetAtPath<SpriteTable2D>(SpriteTable2DPath);
            var textTable = AssetDatabase.LoadAssetAtPath<TextTable2D>(TextTable2DPath);
            if (spriteTable != null) ibSo.FindProperty("_spriteTable").objectReferenceValue = spriteTable;
            if (textTable != null) ibSo.FindProperty("_textTable").objectReferenceValue = textTable;
            ibSo.FindProperty("_spriteStorageMode").intValue = (int)ItemSpriteStorageMode.ProjectAssetTextures;
            ibSo.ApplyModifiedPropertiesWithoutUndo();

            var pickupSpawner = root.AddComponent<RoomTreePackedItemSpawner>();
            var pSpawnerSo = new SerializedObject(pickupSpawner);
            pSpawnerSo.FindProperty("_dungeon").objectReferenceValue = comp;
            var packedPickup = AssetDatabase.LoadAssetAtPath<GameObject>(PackedItemPickupPrefabPath);
            if (packedPickup != null) pSpawnerSo.FindProperty("_pickupPrefab").objectReferenceValue = packedPickup;
            pSpawnerSo.FindProperty("_minPickupsPerRoom").intValue = 0;
            pSpawnerSo.FindProperty("_maxPickupsPerRoom").intValue = 1;
            pSpawnerSo.ApplyModifiedPropertiesWithoutUndo();

            // Spawn the Player prefab at the dungeon center so the demo has a collider-equipped Player
            // that walls can actually block (Dynamic Rigidbody2D + non-trigger Collider2D).
            bool playerSpawned = TrySpawnPlayerPrefab(new Vector3(26f, 26f, 0f));
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                EnsurePlayerRoomTreeDemoComponents(playerGo);

            EnsureEventSystem();

            var inventoryRoot = new GameObject("Inventory");
            inventoryRoot.transform.position = Vector3.zero;
            var invUi = inventoryRoot.AddComponent<InventoryScreenUI>();

            var hotbarRoot = new GameObject("Hotbar");
            hotbarRoot.transform.position = Vector3.zero;
            var hotbarUi = hotbarRoot.AddComponent<InventoryHotbarUI>();

            if (playerGo != null)
            {
                var playerInv = playerGo.GetComponent<PlayerInventory>();
                var playerEq = playerGo.GetComponent<PlayerEquipment>();
                if (playerInv != null)
                {
                    var invSo = new SerializedObject(invUi);
                    invSo.FindProperty("_inventory").objectReferenceValue = playerInv;
                    if (playerEq != null) invSo.FindProperty("_equipment").objectReferenceValue = playerEq;
                    invSo.ApplyModifiedPropertiesWithoutUndo();

                    var hotSo = new SerializedObject(hotbarUi);
                    hotSo.FindProperty("_inventory").objectReferenceValue = playerInv;
                    hotSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            SetupCamera(26, 26, 30); // Center on Room Tree dungeon (4×4 grid, 52×52 bounds)
            SetupPixelPerfectOnMainCamera();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog(
                "Room Tree Demo Scene Created",
                "RoomTreeDungeon ready!\n\n" +
                "Enter Play Mode to generate the 4×4 room-tree dungeon.\n\n" +
                "Camera follows Player (tag); scroll = zoom.\n\n" +
                (playerSpawned
                    ? "Player (Player_body) spawned at dungeon center; walls will block movement via TilemapCollider2D."
                    : $"Player prefab not found at {PlayerPrefabPath}; add a Player-tagged object manually.") +
                "\n\n" +
                "ItemTableBootstrap + packed pickups + 3-hop streaming. Player: inventory, equipment, held-weapon visual, weapon throw (LMB when weapon equipped, uses ThrownProjectile2D). Tab = bag, hotbar = first 4 items; world pickups can auto-equip empty weapon/armor. U = unequip weapon.\n\n" +
                (roomEnemyPrefab != null
                    ? "Room enemies: 2x Melee_Enemy per streamed room; BaseEnemy disabled, pathfinding steers added NPC."
                    : $"Optional: assign a prefab on RoomTreeEnemyPathfindingSystem (default {RoomTreeEnemyPrefabPath})."),
                "OK");
        }

        [MenuItem("Tools/WFC/Add Integration to Scene")]
        public static void AddIntegrationToScene()
        {
            var dungeonGen = Object.FindFirstObjectByType<DungeonGeneration>();
            if (dungeonGen == null)
            {
                Debug.LogWarning("No DungeonGeneration in scene. Add it first.");
                return;
            }

            if (Object.FindFirstObjectByType<DualGridTilemap>() != null)
            {
                Debug.Log("DualGridTilemap already in scene.");
                return;
            }

            var dualGridGo = new GameObject("DualGridTilemap");
            dualGridGo.transform.SetParent(dungeonGen.transform, false);
            var dualGrid = dualGridGo.AddComponent<DualGridTilemap>();

            var so = new SerializedObject(dungeonGen);
            so.FindProperty("dualGridTilemap").objectReferenceValue = dualGrid;
            var biomeRegistry = AssetDatabase.LoadAssetAtPath<BiomeTileRegistry>(BiomeTileRegistryPath);
            if (biomeRegistry != null)
                so.FindProperty("biomeTileRegistry").objectReferenceValue = biomeRegistry;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = dungeonGen.gameObject;
            Debug.Log("Added DualGridTilemap and wired BiomeTileRegistry (if found). Ready for Play Mode.");
        }

        [MenuItem("Tools/WFC/Debug Room Layout (Console)")]
        public static void DebugRoomLayout()
        {
            RoomLayoutGenerator.DebugPrintRoomGrid(10, 10, 8, 16);
        }

        private static bool TrySpawnPlayerPrefab(Vector3 position)
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null) return false;

            // Skip if a Player-tagged object is already in the scene (user may have added their own).
            if (GameObject.FindGameObjectWithTag("Player") != null) return true;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            if (instance == null) return false;

            instance.transform.position = position;
            return true;
        }

        /// <summary>Runtime gameplay components expected for the room-tree demo: items, equip UI, and thrown weapons.</summary>
        private static void EnsurePlayerRoomTreeDemoComponents(GameObject player)
        {
            if (player == null) return;
            if (player.GetComponent<PlayerInventory>() == null) player.AddComponent<PlayerInventory>();
            if (player.GetComponent<PlayerEquipment>() == null) player.AddComponent<PlayerEquipment>();
            if (player.GetComponent<EquippedItemVisual>() == null) player.AddComponent<EquippedItemVisual>();
            if (player.GetComponent<WeaponThrowController>() == null) player.AddComponent<WeaponThrowController>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Match Assets/Scenes/Room_Tree/pixel-perfect_lazy-loading: URP 2D Pixel Perfect (64 PPU, 1280×720 ref).</summary>
        private static void SetupPixelPerfectOnMainCamera()
        {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;
            if (cam.GetComponent<UniversalAdditionalCameraData>() == null)
                cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null)
                ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
            var so = new SerializedObject(ppc);
            var ppu = so.FindProperty("m_AssetsPPU");
            if (ppu != null) ppu.intValue = 64;
            var rx = so.FindProperty("m_RefResolutionX");
            if (rx != null) rx.intValue = 1280;
            var ry = so.FindProperty("m_RefResolutionY");
            if (ry != null) ry.intValue = 720;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupCamera(float centerX, float centerY, float orthoSize = 30f)
        {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = orthoSize;
                cam.transform.position = new Vector3(centerX, centerY, -10);
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                if (cam.GetComponent<WFC_DemoCameraController>() == null)
                    cam.gameObject.AddComponent<WFC_DemoCameraController>();
            }
        }

        private static void AssignWfcTilesToDungeonGeneration(DungeonGeneration dungeonGen)
        {
            var empty = AssetDatabase.LoadAssetAtPath<Tile>($"{WfcTilesPath}/Empty.asset");
            var grass = AssetDatabase.LoadAssetAtPath<Tile>($"{WfcTilesPath}/Grass.asset");
            var dirt = AssetDatabase.LoadAssetAtPath<Tile>($"{WfcTilesPath}/Dirt.asset");
            var path = AssetDatabase.LoadAssetAtPath<Tile>($"{WfcTilesPath}/Path.asset");
            var water = AssetDatabase.LoadAssetAtPath<Tile>($"{WfcTilesPath}/Water.asset");
            var wall = AssetDatabase.LoadAssetAtPath<Tile>($"{WfcTilesPath}/Wall.asset");

            var so = new SerializedObject(dungeonGen);
            if (empty != null) so.FindProperty("emptyTile").objectReferenceValue = empty;
            if (grass != null) so.FindProperty("grassTile").objectReferenceValue = grass;
            if (dirt != null) so.FindProperty("dirtTile").objectReferenceValue = dirt;
            if (path != null) so.FindProperty("pathTile").objectReferenceValue = path;
            if (water != null) so.FindProperty("waterTile").objectReferenceValue = water;
            if (wall != null) so.FindProperty("wallTile").objectReferenceValue = wall;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignWfcTilesToRoomTree(RoomTreeDungeonComponent comp)
        {
            var empty = AssetDatabase.LoadAssetAtPath<TileBase>($"{WfcTilesPath}/Empty.asset");
            var grass = AssetDatabase.LoadAssetAtPath<TileBase>($"{WfcTilesPath}/Grass.asset");
            var dirt = AssetDatabase.LoadAssetAtPath<TileBase>($"{WfcTilesPath}/Dirt.asset");
            var path = AssetDatabase.LoadAssetAtPath<TileBase>($"{WfcTilesPath}/Path.asset");
            var water = AssetDatabase.LoadAssetAtPath<TileBase>($"{WfcTilesPath}/Water.asset");
            var wall = AssetDatabase.LoadAssetAtPath<TileBase>($"{WfcTilesPath}/Wall.asset");

            var so = new SerializedObject(comp);
            if (empty != null) so.FindProperty("emptyTile").objectReferenceValue = empty;
            if (grass != null) so.FindProperty("grassTile").objectReferenceValue = grass;
            if (dirt != null) so.FindProperty("dirtTile").objectReferenceValue = dirt;
            if (path != null) so.FindProperty("pathTile").objectReferenceValue = path;
            if (water != null) so.FindProperty("waterTile").objectReferenceValue = water;
            if (wall != null) so.FindProperty("wallTile").objectReferenceValue = wall;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
