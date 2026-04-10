using UnityEngine;
using UnityEngine.Tilemaps;
using MBAG;

/// <summary>
/// Dual-grid tilemap using 8-bit corner keys. 55 canonical tiles per biome (rotation + reflection).
/// Reads input tilemap, maps to 4 types, computes key, looks up canonical tile + transform.
/// </summary>
[DefaultExecutionOrder(100)]
public class DualGridTilemap : MonoBehaviour
{
    // NEIGHBOURS: [0]=botLeft(0,0), [1]=botRight(1,0), [2]=topLeft(0,1), [3]=topRight(1,1)
    protected static readonly Vector3Int[] NEIGHBOURS = {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0)
    };

    public Tilemap inputTilemap;
    public BoundsInt bounds;
    public int inputHeight;
    public int inputWidth;
    public Vector3Int origin;

    public Tilemap floorTilemap;
    public Tilemap wallTilemap;

    [Header("Placeholders (match WFC tile assets by reference)")]
    [SerializeField] private Tile grassPlaceholderTile;
    [SerializeField] private Tile dirtPlaceholderTile;
    [SerializeField] private Tile emptyPlaceholderTile;
    [SerializeField] private Tile pathPlaceholderTile;
    [SerializeField] private Tile waterPlaceholderTile;
    [SerializeField] private Tile wallPlaceholderTile;

    /// <summary>Set placeholder tiles from WFC (must match DungeonGeneration tile assets by reference).</summary>
    public void SetPlaceholderTiles(Tile empty, Tile grass, Tile dirt, Tile path, Tile water, Tile wall)
    {
        emptyPlaceholderTile = empty;
        grassPlaceholderTile = grass;
        dirtPlaceholderTile = dirt;
        pathPlaceholderTile = path;
        waterPlaceholderTile = water;
        wallPlaceholderTile = wall;
    }

    [Header("Biome tiles (55 canonical tiles, set by bridge or Inspector)")]
    [SerializeField] private BiomeTileSet tileSet;

    [Header("Diagnostics (enable to log gaps and missing tiles)")]
    [SerializeField] private bool logDualGridDiagnostics;

    [Header("Interpretation")]
    [Tooltip("If enabled, the Empty placeholder tile (and null tiles) are treated as Wall for DualGrid key-building. " +
             "This only affects DualGrid rendering (not the underlying WFC TileType logic).")]
    [SerializeField] private bool treatEmptyPlaceholderAsWall = true;

    public BiomeTileSet TileSet
    {
        get => tileSet;
        set => tileSet = value;
    }

    void Start()
    {
        if (inputTilemap == null)
        {
            Debug.LogError("DualGridTilemap: inputTilemap is null.");
            return;
        }

        inputTilemap.CompressBounds();
        bounds = inputTilemap.cellBounds;
        inputHeight = bounds.size.y;
        inputWidth = bounds.size.x;
        origin = bounds.position;

        if (tileSet == null)
        {
            Debug.LogError("DualGridTilemap: BiomeTileSet not assigned.");
            return;
        }

        RefreshFloorTilemap();
    }

    private TileType GetInputTileTypeAt(Vector3Int coords)
    {
        var t = inputTilemap.GetTile(coords);
        if (t == null) return treatEmptyPlaceholderAsWall ? TileType.Wall : TileType.Empty;
        if (t == grassPlaceholderTile) return TileType.Grass;
        if (t == dirtPlaceholderTile) return TileType.Dirt;
        if (t == emptyPlaceholderTile) return treatEmptyPlaceholderAsWall ? TileType.Wall : TileType.Empty;
        if (t == pathPlaceholderTile) return TileType.Path;
        if (t == waterPlaceholderTile) return TileType.Water;
        if (t == wallPlaceholderTile) return TileType.Wall;
        return TileType.Empty;
    }

    protected (Tile tile, int rotation, bool reflected) CalculateFloorTileAndRotation(Vector3Int coords, out int key, out int canonicalIndex, out bool usedFallback)
    {
        TileType tr = GetInputTileTypeAt(coords + NEIGHBOURS[3]);
        TileType tl = GetInputTileTypeAt(coords + NEIGHBOURS[2]);
        TileType br = GetInputTileTypeAt(coords + NEIGHBOURS[1]);
        TileType bl = GetInputTileTypeAt(coords + NEIGHBOURS[0]);

        var dgBl = DualGridTileTypeMapping.FromTileType(bl);
        var dgBr = DualGridTileTypeMapping.FromTileType(br);
        var dgTl = DualGridTileTypeMapping.FromTileType(tl);
        var dgTr = DualGridTileTypeMapping.FromTileType(tr);

        key = DualGridCanonicalKeys.BuildKey(dgBl, dgBr, dgTl, dgTr);
        var (idx, rotation, reflected) = DualGridCanonicalKeys.GetCanonicalIndexRotationAndReflection(key);
        canonicalIndex = idx;

        Tile tile = tileSet.GetTile(canonicalIndex);
        usedFallback = false;
        if (tile == null)
        {
            usedFallback = true;
            if (logDualGridDiagnostics)
                Debug.LogWarning($"[DualGrid] GAP at {coords}: key=0x{key:X2} bl={bl} br={br} tl={tl} tr={tr} -> canonical={canonicalIndex} rot={rotation} refl={reflected} (using fallback).");
            else
                Debug.LogWarning($"DualGrid: No tile for canonical {canonicalIndex} at {coords}, using fallback.");
            return (grassPlaceholderTile != null ? grassPlaceholderTile : null, 0, false);
        }

        return (tile, rotation, reflected);
    }

    protected Tile CalculateFloorTile(Vector3Int coords)
    {
        return CalculateFloorTileAndRotation(coords, out _, out _, out _).tile;
    }

    /// <summary>Build transform to display canonical tile as the cell's key.
    /// GetCanonicalIndexRotationAndReflection finds r such that Rotate90^r(key) = min.
    /// Rotate90 on a key ≡ rotating the sprite 90° CW. We have 'min' and want 'key',
    /// so we undo r CW rotations → r CCW rotations → Euler(0, 0, +r*90).
    /// For reflected: key = Reflect(Rotate90^(4-r)(min)), so reflect first then rotate.
    /// Matrix = Scale * Rotate applies rotate first (CCW), then scale (reflect).</summary>
    private static Matrix4x4 BuildTileTransform(int rotation, bool reflected)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, rotation * 90f);
        Vector3 scale = new Vector3(reflected ? -1f : 1f, 1f, 1f);
        return Matrix4x4.Scale(scale) * Matrix4x4.Rotate(rot);
    }

    protected void SetFloorTile(Vector3Int pos, System.Collections.Generic.HashSet<int> missingCanonicalIndices, ref int gapCount, ref int sampleLogged)
    {
        for (int i = 0; i < NEIGHBOURS.Length; i++)
        {
            Vector3Int newPos = pos + NEIGHBOURS[i];
            var (tile, rotation, reflected) = CalculateFloorTileAndRotation(newPos, out int key, out int canonicalIndex, out bool usedFallback);
            if (tile == null) continue;
            if (missingCanonicalIndices != null && usedFallback)
            {
                missingCanonicalIndices.Add(canonicalIndex);
                gapCount++;
            }
            if (missingCanonicalIndices != null && sampleLogged < 8)
            {
                Debug.Log($"[DualGrid] Sample {sampleLogged}: pos={newPos} key=0x{key:X2} canon={canonicalIndex} rot={rotation} refl={reflected}");
                sampleLogged++;
            }
            if (floorTilemap != null)
            {
                floorTilemap.SetTile(newPos, tile);
                floorTilemap.SetTransformMatrix(newPos, BuildTileTransform(rotation, reflected));
            }
        }
    }

    public void RefreshFloorTilemap()
    {
        if (tileSet == null || floorTilemap == null) return;

        var missingCanonical = logDualGridDiagnostics ? new System.Collections.Generic.HashSet<int>() : null;
        int gapCount = 0;
        int totalCells = 0;
        int sampleLogged = 0;

        for (int x = bounds.xMin - 1; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin - 1; y < bounds.yMax; y++)
            {
                SetFloorTile(new Vector3Int(x, y, 0), missingCanonical, ref gapCount, ref sampleLogged);
                totalCells += 4;
            }
        }

        if (logDualGridDiagnostics)
        {
            if (gapCount > 0)
                Debug.Log($"[DualGrid] Summary: {totalCells} dual cell lookups, {gapCount} gaps (fallback used). Missing canonical indices in BiomeTileSet: {string.Join(", ", missingCanonical ?? new System.Collections.Generic.HashSet<int>())}. Fix by assigning tiles for those indices.");
            else
                Debug.Log($"[DualGrid] Summary: {totalCells} dual cell lookups, no gaps. Compare sample lines above with Aseprite/Lua key order (bl|br<<2|tl<<4|tr<<6).");
        }
    }
}
