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
                "Enter Play Mode to generate. WASD/Arrows = pan, Scroll = zoom.\n" +
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

            SetupCamera(26, 26, 30); // Center on Room Tree dungeon (4×4 grid, 52×52 bounds)
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog(
                "Room Tree Demo Scene Created",
                "RoomTreeDungeon ready!\n\n" +
                "Enter Play Mode to generate the 4×4 room-tree dungeon.\n\n" +
                "WASD/Arrows = pan, Scroll = zoom.",
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
