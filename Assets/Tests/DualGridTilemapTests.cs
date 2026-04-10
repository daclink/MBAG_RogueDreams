using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using MBAG;

public class DualGridTilemapTests
{
    private GameObject _root;
    private Grid _grid;

    private Tilemap _input;
    private Tilemap _floor;
    private Tilemap _wall;

    private DualGridTilemap _sut;
    private BiomeTileSet _tileSet;

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
        _grid = _root.AddComponent<Grid>();
        Assert.NotNull(_grid);

        _input = CreateTilemapUnderGrid("Input");
        _floor = CreateTilemapUnderGrid("Floor");
        _wall = CreateTilemapUnderGrid("Wall");

        _sut = _root.AddComponent<DualGridTilemap>();
        Assert.NotNull(_sut);

        _sut.inputTilemap = _input;
        _sut.floorTilemap = _floor;
        _sut.wallTilemap = _wall;

        _grassPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _dirtPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _emptyPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _pathPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _waterPlaceholder = ScriptableObject.CreateInstance<Tile>();
        _wallPlaceholder = ScriptableObject.CreateInstance<Tile>();

        _sut.SetPlaceholderTiles(_emptyPlaceholder, _grassPlaceholder, _dirtPlaceholder,
            _pathPlaceholder, _waterPlaceholder, _wallPlaceholder);

        // Input: TL(0,1)=Grass, TR(1,1)=Dirt, BL(0,0)=Empty, BR(1,0)=Path
        SetInputPlaceholder(new Vector3Int(0, 1, 0), TileType.Grass);
        SetInputPlaceholder(new Vector3Int(1, 1, 0), TileType.Dirt);
        SetInputPlaceholder(new Vector3Int(0, 0, 0), TileType.Empty);
        SetInputPlaceholder(new Vector3Int(1, 0, 0), TileType.Path);

        _tileSet = ScriptableObject.CreateInstance<BiomeTileSet>();
        for (int i = 0; i < BiomeTileSet.TileCount; i++)
            _tileSet.SetTile(i, ScriptableObject.CreateInstance<Tile>());
        _sut.TileSet = _tileSet;

        _input.CompressBounds();
        InvokeStart(_sut);
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            UnityEngine.Object.DestroyImmediate(_root);
        if (_tileSet != null)
            UnityEngine.Object.DestroyImmediate(_tileSet);
    }

    [Test]
    public void Start_WithBiomeTileSet_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => InvokeCalculateFloorTile(new Vector3Int(0, 0, 0)));
    }

    [Test]
    public void CalculateFloorTile_ReturnsTileForCanonicalKey()
    {
        // BL=Empty(Ground), BR=Path(Ground), TL=Grass, TR=Dirt(Ground) -> key maps to some canonical index
        int key = DualGridCanonicalKeys.BuildKey(
            DualGridTileType.Ground, DualGridTileType.Ground,
            DualGridTileType.Grass, DualGridTileType.Ground);
        var (canonicalIndex, _) = DualGridCanonicalKeys.GetCanonicalIndexAndRotation(key);
        var expected = _tileSet.GetTile(canonicalIndex);
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
        _sut.bounds = _input.cellBounds;
        _sut.RefreshFloorTilemap();

        Assert.IsNotNull(_floor.GetTile(new Vector3Int(0, 0, 0)));
        Assert.IsNotNull(_floor.GetTile(new Vector3Int(1, 1, 0)));
    }

    private Tilemap CreateTilemapUnderGrid(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_grid.transform, false);
        go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return go.GetComponent<Tilemap>();
    }

    private void SetInputPlaceholder(Vector3Int pos, TileType type)
    {
        Tile t = type switch
        {
            TileType.Grass => _grassPlaceholder,
            TileType.Dirt => _dirtPlaceholder,
            TileType.Empty => _emptyPlaceholder,
            TileType.Path => _pathPlaceholder,
            TileType.Water => _waterPlaceholder,
            TileType.Wall => _wallPlaceholder,
            _ => _emptyPlaceholder
        };
        _input.SetTile(pos, t);
    }

    private void InvokeStart(MonoBehaviour mb)
    {
        var method = mb.GetType().GetMethod("Start",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method.Invoke(mb, null);
    }

    private Tile InvokeCalculateFloorTile(Vector3Int coords)
    {
        var method = typeof(DualGridTilemap).GetMethod("CalculateFloorTile",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (Tile)method.Invoke(_sut, new object[] { coords });
    }

    private void InvokeSetFloorTile(Vector3Int coords)
    {
        var method = typeof(DualGridTilemap).GetMethod("SetFloorTile",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var gapCount = new int[1];
        var sampleLogged = new int[1];
        method.Invoke(_sut, new object[] { coords, null, gapCount, sampleLogged });
    }
}
