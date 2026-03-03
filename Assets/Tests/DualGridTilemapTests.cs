using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DualGridTilemapTests
{
    private GameObject _root;
    private Grid _grid;

    private Tilemap _input;
    private Tilemap _floor;
    private Tilemap _wall;

    private DualGridTilemap _sut;

    private Tile _grassPlaceholder;
    private Tile _dirtPlaceholder;
    private Tile _emptyPlaceholder;
    private Tile _pathPlaceholder;
    private Tile _waterPlaceholder;
    private Tile _wallPlaceholder;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("DualGridTilemapTests_Root");

        // Important in Unity 6: make a Grid and parent tilemaps under it.
        _grid = _root.AddComponent<Grid>();
        Assert.NotNull(_grid, "Grid component failed to add.");

        _input = CreateTilemapUnderGrid("Input");
        _floor = CreateTilemapUnderGrid("Floor");
        _wall  = CreateTilemapUnderGrid("Wall");

        Assert.NotNull(_input, "Input Tilemap was not created.");
        Assert.NotNull(_floor, "Floor Tilemap was not created.");
        Assert.NotNull(_wall,  "Wall Tilemap was not created.");

        _sut = _root.AddComponent<DualGridTilemap>();
        Assert.NotNull(_sut, "DualGridTilemap component failed to add.");

        _sut.inputTilemap = _input;
        _sut.floorTilemap = _floor;
        _sut.wallTilemap  = _wall;

        // Create placeholder tiles (distinct instances)
        _grassPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _dirtPlaceholder  = ScriptableObject.CreateInstance<Tile>();
        _emptyPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _pathPlaceholder  = ScriptableObject.CreateInstance<Tile>();
        _waterPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _wallPlaceholder  = ScriptableObject.CreateInstance<Tile>();

        Assert.NotNull(_grassPlaceholder);
        Assert.NotNull(_dirtPlaceholder);
        Assert.NotNull(_emptyPlaceholder);
        Assert.NotNull(_pathPlaceholder);
        Assert.NotNull(_waterPlaceholder);
        Assert.NotNull(_wallPlaceholder);

        // Assign placeholders so GetInputTileTypeAt matches by reference
        _sut.grassPlaceholderTile = _grassPlaceholder;
        _sut.dirtPlaceholderTile  = _dirtPlaceholder;
        _sut.emptyPlaceholderTile = _emptyPlaceholder;
        _sut.pathPlaceholderTile  = _pathPlaceholder;
        _sut.waterPlaceholderTile = _waterPlaceholder;
        _sut.wallPlaceholderTile  = _wallPlaceholder;

        // Fill input with a minimal 2x2 block BEFORE calling Start()
        // TL (0,1) = Grass, TR (1,1) = Dirt, BL (0,0) = Empty, BR (1,0) = Path
        SetInputPlaceholder(new Vector3Int(0, 1, 0), TileType.Grass);
        SetInputPlaceholder(new Vector3Int(1, 1, 0), TileType.Dirt);
        SetInputPlaceholder(new Vector3Int(0, 0, 0), TileType.Empty);
        SetInputPlaceholder(new Vector3Int(1, 0, 0), TileType.Path);

        // Build sut.tiles with the length Start() requires
        TileType[] values = (TileType[])Enum.GetValues(typeof(TileType));
        int n = values.Length;
        int required = n * n * n * n;

        _sut.tiles = new Tile[required];
        for (int i = 0; i < required; i++)
            _sut.tiles[i] = ScriptableObject.CreateInstance<Tile>();

        // Ensure input bounds are set up for Start()
        _input.CompressBounds();

        // Now call Start() so it builds the dictionary + refreshes floor
        InvokeStart(_sut);
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            UnityEngine.Object.DestroyImmediate(_root);
    }

    [Test]
    public void Start_RuleBuilding_WorksWithNonContiguousEnumValues()
    {
        var origin = new Vector3Int(0, 0, 0);
        Assert.DoesNotThrow(() => InvokeCalculateFloorTile(origin));
    }

    [Test]
    public void CalculateFloorTile_ReturnsTileForNeighbourTuple()
    {
        // Expected index in the exact ordering Start() uses (values[] order)
        var expected = ExpectedTile(TileType.Grass, TileType.Dirt, TileType.Empty, TileType.Path);
        var actual = InvokeCalculateFloorTile(new Vector3Int(0, 0, 0));
        Assert.AreSame(expected, actual);
    }

    [Test]
    public void SetFloorTile_WritesFourTiles()
    {
        _floor.ClearAllTiles();

        InvokeSetFloorTile(new Vector3Int(0, 0, 0));

        Assert.IsNotNull(_floor.GetTile(new Vector3Int(0, 0, 0)));
        Assert.IsNotNull(_floor.GetTile(new Vector3Int(1, 0, 0)));
        Assert.IsNotNull(_floor.GetTile(new Vector3Int(0, 1, 0)));
        Assert.IsNotNull(_floor.GetTile(new Vector3Int(1, 1, 0)));
    }

    [Test]
    public void RefreshFloorTilemap_WritesTilesAcrossBounds()
    {
        _floor.ClearAllTiles();

        // Ensure bounds are current (Start sets this, but keep it explicit)
        _sut.bounds = _input.cellBounds;

        _sut.RefreshFloorTilemap();

        Assert.IsNotNull(_floor.GetTile(new Vector3Int(0, 0, 0)));
        Assert.IsNotNull(_floor.GetTile(new Vector3Int(1, 1, 0)));
    }

    // ---------------- Helpers ----------------

    private Tilemap CreateTilemapUnderGrid(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_grid.transform, false);

        // Order doesn't usually matter, but keep renderer present.
        var tilemap = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();

        return tilemap;
    }

    private void SetInputPlaceholder(Vector3Int pos, TileType type)
    {
        Assert.NotNull(_input, "Input Tilemap is null in SetInputPlaceholder.");

        Tile t = type switch
        {
            TileType.Grass => _grassPlaceholder,
            TileType.Dirt  => _dirtPlaceholder,
            TileType.Empty => _emptyPlaceholder,
            TileType.Path  => _pathPlaceholder,
            TileType.Water => _waterPlaceholder,
            TileType.Wall  => _wallPlaceholder,
            _ => _emptyPlaceholder
        };

        Assert.NotNull(t, $"Placeholder tile for {type} was null.");
        _input.SetTile(pos, t);
    }

    private void InvokeStart(MonoBehaviour mb)
    {
        var method = mb.GetType().GetMethod("Start",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(method, "Could not find Start() via reflection.");
        method.Invoke(mb, null);
    }

    private Tile InvokeCalculateFloorTile(Vector3Int coords)
    {
        var method = typeof(DualGridTilemap).GetMethod("CalculateFloorTile",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method, "Could not find CalculateFloorTile via reflection.");
        return (Tile)method.Invoke(_sut, new object[] { coords });
    }

    private void InvokeSetFloorTile(Vector3Int coords)
    {
        var method = typeof(DualGridTilemap).GetMethod("SetFloorTile",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method, "Could not find SetFloorTile via reflection.");
        method.Invoke(_sut, new object[] { coords });
    }

    // Mirror the exact ordering your Start() uses:
    // values = Enum.GetValues(TileType) and then tl,tr,bl,br nested loops,
    // assigning tiles[index++] in that iteration order.
    private Tile ExpectedTile(TileType topLeft, TileType topRight, TileType botLeft, TileType botRight)
    {
        TileType[] values = (TileType[])Enum.GetValues(typeof(TileType));
        int n = values.Length;

        int tl = Array.IndexOf(values, topLeft);
        int tr = Array.IndexOf(values, topRight);
        int bl = Array.IndexOf(values, botLeft);
        int br = Array.IndexOf(values, botRight);

        Assert.GreaterOrEqual(tl, 0);
        Assert.GreaterOrEqual(tr, 0);
        Assert.GreaterOrEqual(bl, 0);
        Assert.GreaterOrEqual(br, 0);

        int idx = ((tl * n + tr) * n + bl) * n + br;
        return _sut.tiles[idx];
    }
}