# Room-local 64-bit pathfinding (Room-tree) — implementation plan

This document is the agreed design for integrating **room-local** enemy pathfinding with **Room-tree** dungeons. It extends the spirit of `BbGrid` (BFS distance field + greedy steps) with **8-neighbor** moves, **strict corner-cutting**, and **caching**.

---

## Resolved decisions

| Topic | Decision |
|--------|-----------|
| **Room owner** | **Room-tree** owns room identity, bounds, and lifecycle. |
| **Room id** | **Stable** across a run; safe to key caches by `roomId`. |
| **Interior** | **Exactly 8×8** cells per room (64 cells → one `ulong` walk mask). |
| **Walk mask source** | **`TileType`** → walkable / blocked; layout **static** until regen / explicit layout bump. |
| **Outer ring** | Walkable on the perimeter **only** where the room has a **connector** into another room (enforce when building mask or via connector bitmask OR). |
| **Pillars** | Rare **single-tile** obstacles inside the interior; **strict diagonal corner-cutting** is required. |
| **Enemy scope** | Enemies **path only inside the current room**; no cross-room paths in v1. |
| **Max enemies** | **≤ 3** active in the current room (tune later). |
| **BFS usage** | **One BFS per relevant update**, **sourced from the player’s cell** in room-local coordinates. Distance field is **shared** by all enemies in that room (same goal source). |
| **Invalid goal** | If the player cell is not walkable (bug / edge case), snap goal to **nearest walkable** cell (BFS from candidate or Manhattan scan on small grid—pick one implementation and document). |
| **Adjacent rooms** | **May prebuild** walk masks for **neighbor** rooms when entering / warming cache. |
| **Multiplayer** | **None**; no extra determinism requirements. |
| **Bit order** | **Match `BbGrid`** index convention (see below). |

---

## Bit index ↔ cell (BbGrid ordering)

Canonical index (same as `BbGrid.InstantiateSquares`):

- `index = y * 8 + x` with `x, y ∈ {0,…,7}`.
- Equivalently: `x = index % 8`, `y = index / 8`.

**4-neighbor deltas** (same as `BbGrid.FloodFill`):

| Direction | Delta |
|-----------|--------|
| North | `+8` |
| South | `-8` |
| West | `-1` |
| East | `+1` |

**8-neighbor diagonal deltas** (consistent with the above):

| Diagonal | Delta |
|----------|--------|
| NE | `+9` |
| NW | `+7` |
| SE | `-7` |
| SW | `-9` |

**Grid bounds:** Before applying a delta, ensure the neighbor stays in bounds (`0 ≤ x < 8`, `0 ≤ y < 8`).

**World / tilemap:** `BbGrid` places sprites with an **x flip** in world space (`7 - x` in one coordinate). When wiring **Tilemap** cells to `index`, implement **one** explicit mapping function (room min cell + local `(x,y)` → index) so **bit `k` always means the same tile** as in this table—do not mix conventions.

---

## Strict corner-cutting (diagonals)

For a diagonal step from cell `C` to `D`, allow only if **both** orthogonal neighbors that share the edge between `C` and `D` are walkable (prevents cutting through single-tile pinch geometry).

Examples (from `(x,y)`):

- **NE** toward `(x+1,y+1)`: require **E** `(x+1,y)` and **N** `(x,y+1)` walkable.
- **NW** toward `(x-1,y+1)`: require **W** `(x-1,y)` and **N** `(x,y+1)` walkable.
- **SE** toward `(x+1,y-1)`: require **E** `(x+1,y)` and **S** `(x,y-1)` walkable.
- **SW** toward `(x-1,y-1)`: require **W** `(x-1,y)` and **S** `(x,y-1)` walkable.

---

## Walk mask `ulong walkMask`

- Bit `i` is **1** if cell `i` is walkable for local pathfinding, **0** if blocked.
- Built from **`TileType`** for the 8×8 interior, plus rules for **connector** perimeter cells.
- **Cache key:** `(roomId, layoutVersion)` or equivalent; **invalidate** when layout version bumps (dungeon regen, room rebuild, any future “mutate room” event).

---

## Runtime flow (per room, per “goal refresh”)

1. **Resolve room** (Room-tree): `roomId`, **8×8 bounds** (min cell in tile space), `layoutVersion`.
2. **Get or build** `walkMask` from cache; optionally **warm** neighbor rooms’ masks.
3. **Player → local index** `goalIndex` using bounds + BbGrid ordering.
4. If `goalIndex` is not walkable: **`goalIndex := nearestWalkable(goalIndex)`** (documented helper; e.g. BFS layer expand from goal on inverted mask, or spiral search on 8×8).
5. **One BFS** from `goalIndex` over walkable cells with **8 neighbors** + corner-cutting; produce `dist[64]` (`-1` / sentinel = unreachable).
6. Each **enemy**: world → local `startIndex`; if unreachable, idle or last-known policy; else **greedy descent** along decreasing `dist` (same idea as `BbGrid.CreatePathToTarget`) to produce next waypoint(s).

**When to re-run BFS:** On **player cell change** in room-local coords, or on **timer** if you want stale tolerance (start with cell-change only).

---

## Integration points (codebase)

- **Room-tree**: expose `roomId`, interior **`BoundsInt`** (or min `Vector3Int` + 8×8), `layoutVersion`, and **neighbor room ids** for prefetch.
- **Tile read**: one builder `BuildWalkMask(room) -> ulong` reading `TileType` from the same source of truth used to paint the room.
- **Cache service**: `GetOrBuild(roomId, version) → (walkMask, …)`; optional `WarmNeighbors(roomId)`.
- **Movement**: reuse / adapt **`NPC`-style** steering; feed **cell centers** from `Tilemap` / `Grid` using room bounds.

---

## Suggested layering

1. **`RoomBitGrid64`** (platform-agnostic if possible): `walkMask`, BFS8, `dist[]`, `nearestWalkable`, `greedyNextStep`.
2. **`RoomWalkMaskCache`**: Unity-side cache + invalidation + optional neighbor warm.
3. **Thin bridge** from Room-tree events (“player entered room R”, “dungeon regenerated”) to cache invalidation / prefetch.

---

## Out of scope (v1)

- Cross-room paths, corridor graphs, hierarchical pathing.
- Dynamic obstacles (doors closing mid-fight) — would require mask rebuild or version bump.
- Weighted costs (mud); uniform steps only unless we extend later.

---

## First implementation slice (checklist)

1. Document / implement **`TileType` → walkable`** table for mask build.
2. **`BuildWalkMask`** from room bounds + tile query + connector rule.
3. **`RoomBitGrid64`**: BFS from player index with **8-neighbor** + **corner-cutting**; `dist[64]`.
4. **`nearestWalkable`** fallback.
5. **Cache** by `(roomId, layoutVersion)` + optional neighbor warm on room enter.
6. **One enemy** using greedy descent + tile center waypoints; cap at 3.

After this file lands, implementation work can proceed against this checklist.

---

## Implementation (first pass)

| Piece | Location |
|--------|-----------|
| Core BFS + greedy + corner diagonals | `Assets/Scripts/Pathfinding_Scripts/RoomBitGrid64.cs` (`MBAG.Pathfinding`) |
| 8×8 mask from `TileType[,]` + cell helpers | `Assets/Scripts/WFC/RoomWalkMaskBuilder.cs` |
| Per-room mask cache | `Assets/Scripts/WFC/RoomWalkMaskCache.cs` |
| Demo driver (player goal, one NPC, same-room chase) | `Assets/Scripts/WFC/RoomTreeRoomPathfindingDriver.cs` |
| `LayoutVersion`, `DungeonGrid`, `OnRoomTreeGenerated` | `RoomTreeDungeonComponent.cs` |
| Room tree menu wires driver + NPC prefab when present | `WFC/Editor/WFC_Editor.cs` → **Tools/WFC/Create Room Tree Demo Scene** |

**Setup:** Add `RoomTreeRoomPathfindingDriver` next to `RoomTreeDungeonComponent`, assign **NPC** prefab (e.g. `Assets/Prefabs/Pathfinding_Demo/NPC.prefab`). Scene needs a **Player**-tagged object in a room’s **8×8 interior** for the test NPC to chase (same room only in v1).
