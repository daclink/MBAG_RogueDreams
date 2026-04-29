# `Assets/Scripts`

Runtime and editor scripts. Namespaces vary (e.g. `WFC`, `MBAG.Pathfinding`, default for gameplay).

## Procedural / dungeon code

| Folder | What lives here |
|--------|-----------------|
| **`WFC/`** | WFC **core** (`WFCCore`), **classic** full-tilemap WFC (`WFCTilemap`), **single-room** WFC (`RoomWFCTilemap`), `RoomLayoutGenerator` for the classic pipeline, **classic demo** (`DungeonGeneration`, `DungeonSetup`, `MinimapRenderer`, `WFC_DemoCameraController`), `WFC_DualGrid_Setup.md`. |
| **`RoomTree/`** | **Room-tree** dungeon: spanning tree, corridors, streaming (`RoomTreeDungeonComponent`), walk masks, `RoomTreeEnemyPathfindingSystem`, `RoomTreePackedItemSpawner`, `RoomTreeGrid`, `RoomTreeDungeon_Spec.md`. Types use `namespace WFC`. |
| **`Tilemaps/`** | **`WfcTilemapCollisionUtility`** — TilemapCollider2D + CompositeCollider2D + static RB2D for merged 2D collision (room-tree streaming + classic demo). |
| **`Editor/WFC/`** | **`WFC_Editor.cs`** — menu **Tools → WFC** (see table below). Editor assembly only. |
| **`Pathfinding_Scripts/`** | `NPC`, `RoomBitGrid64`, `BbGrid`, spawners; room-tree uses masks from **RoomTree** + `RoomLocalPathfinding_Plan.md` in this folder. |
| **`Dual_Grid_Scripts/`** | DualGrid pipeline; **`DualGrid_System_Explanation.md`**, **`DualGridBiomeDesign.md`**. |

### Tools → WFC (editor)

| Menu item | Effect |
|-----------|--------|
| **Create New WFC Demo Scene** | Scene with `DungeonGeneration` + `DualGridTilemap` (classic pipeline). |
| **Create Room Tree Demo Scene** | Scene with `RoomTreeDungeon`, items, player, enemies, camera — see `docs/room-tree-demo-scene.md`. |
| **Add Integration to Scene** | Adds `DualGridTilemap` under existing `DungeonGeneration` if missing. |
| **Debug Room Layout (Console)** | Prints debug room grid via `RoomLayoutGenerator`. |

## In-repo docs (procedural)

- **[`docs/room-tree-demo-scene.md`](../../docs/room-tree-demo-scene.md)** — room-tree demo setup and verification.
- **`WFC/WFC_DualGrid_Setup.md`**, **`Dual_Grid_Scripts/DualGrid_System_Explanation.md`**, **`RoomTree/RoomTreeDungeon_Spec.md`**, **`Pathfinding_Scripts/RoomLocalPathfinding_Plan.md`**.

## Other gameplay folders

`Player_Scripts/`, `Enemy_Scripts/`, `Item_Scripts/`, `Combat_Scripts/`, etc. — discover by feature.

Project root **[`README.md`](../../README.md)** — external design links and tutorials.
