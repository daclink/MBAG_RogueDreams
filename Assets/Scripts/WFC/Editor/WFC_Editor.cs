#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using WFC;

namespace WFC.Editor
{
    public static class WFC_Editor
    {
        private const string WfcTilesPath = "Assets/Art/Tiles/World/WFC_Tiles";
        private const string BiomeTileRegistryPath = "Assets/ScriptableObjects/BiomeTileRegistry.asset";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player_Prefabs/Player.prefab";

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
            so.ApplyModifiedPropertiesWithoutUndo();

            var pathfindingDriver = root.AddComponent<RoomTreeRoomPathfindingDriver>();
            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pathfinding_Demo/NPC.prefab");
            if (npcPrefab != null)
            {
                var driverSo = new SerializedObject(pathfindingDriver);
                driverSo.FindProperty("_dungeon").objectReferenceValue = comp;
                driverSo.FindProperty("_testNpcPrefab").objectReferenceValue = npcPrefab;
                driverSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Spawn the Player prefab at the dungeon center so the demo has a collider-equipped Player
            // that walls can actually block (Dynamic Rigidbody2D + non-trigger Collider2D).
            bool playerSpawned = TrySpawnPlayerPrefab(new Vector3(26f, 26f, 0f));

            SetupCamera(26, 26, 30); // Center on Room Tree dungeon (4×4 grid, 52×52 bounds)
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog(
                "Room Tree Demo Scene Created",
                "RoomTreeDungeon ready!\n\n" +
                "Enter Play Mode to generate the 4×4 room-tree dungeon.\n\n" +
                "Camera follows Player (tag); scroll = zoom.\n\n" +
                (playerSpawned
                    ? "Player spawned at dungeon center; walls will block movement via TilemapCollider2D."
                    : $"Player prefab not found at {PlayerPrefabPath}; add a Player-tagged object manually.") +
                "\n\n" +
                (npcPrefab != null
                    ? "Room pathfinding test: NPC prefab wired; NPC will chase Player when they share a room."
                    : "Optional: assign NPC prefab on RoomTreeRoomPathfindingDriver for room-local chase demo."),
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
