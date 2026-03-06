#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Slices an 11×5 texture into 55 sprites (canonical tiles), imports them, creates Tile assets,
/// and populates BiomeTileSet. Order matches Aseprite Generate55CanonicalTiles output.
/// Layout: 11 cols × 5 rows, row 0 = bottom of texture. Index = col + row*11.
/// </summary>
public class BiomeTileSheetSlicer55 : EditorWindow
{
    private Texture2D sourceTexture;
    private BiomeTileSet targetTileSet;
    private string tileAssetFolder = "Assets/Tables/BiomeTiles/GeneratedTiles";
    private int cellWidth = 64;
    private int cellHeight = 64;
    private const int Cols = 11;
    private const int Rows = 5;
    private const int TileCount = 55;

    [MenuItem("Tools/Biome Tiles/Slice 55-Tile Sheet to BiomeTileSet")]
    [MenuItem("Window/Biome Tiles/Slice 55-Tile Sheet to BiomeTileSet")]
    public static void ShowWindow()
    {
        GetWindow<BiomeTileSheetSlicer55>("55 Canonical Tile Slicer");
    }

    private void OnGUI()
    {
        GUILayout.Label("55 Canonical Tile Slicer", EditorStyles.boldLabel);
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet (11×5)", sourceTexture, typeof(Texture2D), false);
        targetTileSet = (BiomeTileSet)EditorGUILayout.ObjectField("Target BiomeTileSet", targetTileSet, typeof(BiomeTileSet), false);
        tileAssetFolder = EditorGUILayout.TextField("Tile Output Folder", tileAssetFolder);
        cellWidth = EditorGUILayout.IntField("Cell Width", cellWidth);
        cellHeight = EditorGUILayout.IntField("Cell Height", cellHeight);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Slices an 11×5 grid (55 tiles) into sprites. Order: left-to-right, bottom-to-top. " +
            "Must match Aseprite Generate55CanonicalTiles export. Default 64×64 cells.",
            MessageType.Info);

        GUI.enabled = sourceTexture != null && targetTileSet != null;
        if (GUILayout.Button("Slice and Assign"))
            SliceAndAssign();
        GUI.enabled = true;
    }

    private void SliceAndAssign()
    {
        if (sourceTexture == null || targetTileSet == null)
        {
            EditorUtility.DisplayDialog("Error", "Assign texture and BiomeTileSet.", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not get TextureImporter.", "OK");
            return;
        }

        int cols = sourceTexture.width / cellWidth;
        int rows = sourceTexture.height / cellHeight;
        if (cols < Cols || rows < Rows)
        {
            EditorUtility.DisplayDialog("Error", $"Texture too small for {Cols}×{Rows}. Got {cols}×{rows} cells.", "OK");
            return;
        }

        SpriteMetaData[] sheet = new SpriteMetaData[TileCount];
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        int idx = 0;
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                float x = col * cellWidth;
                float y = sourceTexture.height - (row + 1) * cellHeight;
                sheet[idx] = new SpriteMetaData
                {
                    name = $"{sourceTexture.name}_{idx}",
                    rect = new Rect(x, y, cellWidth, cellHeight),
                    pivot = pivot,
                    alignment = (int)SpriteAlignment.Center
                };
                idx++;
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = cellHeight;
        importer.filterMode = FilterMode.Point;
#pragma warning disable CS0618
        importer.spritesheet = sheet;
#pragma warning restore CS0618
        importer.SaveAndReimport();

        AssetDatabase.Refresh();

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite[] sprites = new Sprite[TileCount];
        int spriteCount = 0;
        foreach (var obj in assets)
        {
            if (obj is Sprite s && spriteCount < TileCount)
                sprites[spriteCount++] = s;
        }

        if (spriteCount != TileCount)
        {
            EditorUtility.DisplayDialog("Error", $"Expected {TileCount} sprites, got {spriteCount}.", "OK");
            return;
        }

        EnsureFolderPath(tileAssetFolder);
        string baseName = sourceTexture.name + "_tile";

        SerializedObject so = new SerializedObject(targetTileSet);
        SerializedProperty tilesProp = so.FindProperty("tiles");
        if (tilesProp.arraySize != TileCount)
            tilesProp.arraySize = TileCount;

        for (int i = 0; i < TileCount; i++)
        {
            Tile tile = CreateOrUpdateTile(sprites[i], tileAssetFolder, baseName + i);
            tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = tile;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(targetTileSet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done", $"Created {TileCount} tiles and assigned to {targetTileSet.name}.", "OK");
    }

    private static Tile CreateOrUpdateTile(Sprite sprite, string folder, string assetName)
    {
        string path = $"{folder.TrimEnd('/')}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null)
        {
            existing.sprite = sprite;
            existing.flags = TileFlags.None;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.flags = TileFlags.None;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    private static void EnsureFolderPath(string path)
    {
        string[] parts = path.Replace('\\', '/').TrimEnd('/').Split('/');
        string current = "";
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]) || parts[i] == "Assets") { current = "Assets"; continue; }
            string next = current == "" ? parts[i] : current + "/" + parts[i];
            if (current != "" && !AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
