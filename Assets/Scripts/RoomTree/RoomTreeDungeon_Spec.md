# Room Tree Dungeon – Design Specification

## Overview

A dungeon system where rooms are nodes in a spanning tree over a **gridSize × gridSize** grid. Each room occupies a fixed **10×10** tile block in world space, with doors on the outer wall. Corridors connect adjacent rooms via a single global corridor tilemap. A separate base tilemap can be built as DualGrid input (registry-driven canonical tiles).

---

## 1. Grid & Spanning Tree

| Setting | Value |
|---------|-------|
| Grid size | `gridSize × gridSize` (Inspector-configurable; default 4×4) |
| Root | Random placement |
| Tree | Random spanning tree; prefer branching; reduce (not eliminate) dead ends |
| Max depth | Determined by tree structure (tree depth from root) |

---

## 2. Room Structure

| Setting | Value |
|---------|-------|
| Total size | 10×10 tiles |
| Outer walls | 1 tile thick on all sides (perimeter ring) |
| Doors | On N/E/S/W wall edges (when a neighbor exists), with door position chosen per edge |
| Interior shape | “Organic” interior carved around the persistent path (walls follow the path) |

---

## 3. Corridors

| Setting | Value |
|---------|-------|
| Width | 1 tile (match RoomTreeLayout.PathWidth) |
| Extent | Runs from the door on room A to the door on room B across the gap |
| Tilemap | Single global corridor tilemap for all corridors |

---

## 4. Coordinate System

- **Room grid**: `(gx, gy)` where `0 ≤ gx, gy < gridSize`
- **World position**:
  - `worldX = gx * RoomTreeLayout.Spacing`
  - `worldY = gy * RoomTreeLayout.Spacing`
- **Spacing**:
  - `RoomTreeLayout.Spacing = RoomTreeLayout.RoomSize + RoomTreeLayout.CorridorGap`
  - With RoomSize=10 and PathWidth=1, CorridorGap=1 → Spacing=11 (default)

---

## 5. Special Rooms

| Type | Placement |
|------|-----------|
| **Start** | Root room |
| **End** | Room at max tree depth |
| **Item** | Depths 2, 4, 8, … (powers of 2) |

**Item spawn**:
- First item at depth 2
- Once a room at depth D has an item, other rooms at depth D have reduced chance for another item
- Second item at depth 4, third at depth 8, etc. (same diminishing chance within each depth)

---

## 6. Generation Flow

1. Build `gridSize × gridSize` grid of room positions
2. Pick random root
3. Compute random spanning tree (prefer branching, reduce dead ends)
4. Assign depths from root via BFS
5. Assign special rooms: start=root, end=max-depth room, items at depths 2,4,8,… with diminishing per-depth chance
6. Assign door positions per tree edge (door start chosen along the wall; written to both rooms)
7. Generate rooms (RoomWFCTilemap):
   - place perimeter walls + door openings
   - carve a persistent 1-tile-wide path that connects all doors inside the room
   - carve an “organic” interior region around the path (cells far from the path become walls)
   - run WFC for terrain types inside the remaining interior region
8. Generate corridors on the global corridor tilemap (door-to-door across the gap)
8. Store `TileType[,]` per room in node

---

## 7. Rendering

| Setting | Value |
|---------|-------|
| Per room | One Tilemap per room (Option A) |
| Corridors | One global corridor tilemap |
| DualGrid input (optional) | One base tilemap covering the dungeon bounds (rooms + corridors) |

---

## 8. Edge Blending

- DualGrid rendering uses a 2×2 neighborhood key per dual cell and a BiomeTileRegistry/BiomeTileSet to pick a canonical tile + transform.
- For the RoomTree demo, the simplest approach is a single base tilemap covering the bounds as DualGrid input (rooms + corridors).

---

## 9. Biome

- Single biome for all rooms
- Set before generation starts

---

## 10. WFC Architecture

- **Extract shared logic** from `WFCTilemap`: adjacency rules, collapse, propagate, entropy
- **RoomWFCTilemap** (or similar): single-room variant
  - Input: room size, door positions, optional boundary constraints (N/S/E/W edges)
  - Output: `TileType[,]` for that room
- **Corridor generation**: separate logic for corridor paths (deterministic from spanning tree edges)

---

## 11. Data Structures

```
RoomTreeNode
  - GridPosition (Vector2Int)
  - Depth (int)
  - RoomType (Start | End | Item | Normal)
  - TileData (TileType[,])  // 10×10
  - Neighbors (List<RoomTreeNode>)  // up to 4, from spanning tree
  - BiomeIndex (int)  // same for all for now
```

---

## 12. Implementation Status

- [x] Shared WFC core extraction (WFCCore.cs)
- [x] RoomWFCTilemap with door placement + persistent path + organic interior
- [x] Spanning tree algorithm (RoomTreeSpanningTreeBuilder)
- [x] Corridor path calculation and tile placement (RoomTreeCorridorGenerator)
- [x] RoomTreeDungeonGenerator orchestrator
- [x] RoomTreeDungeonComponent – MonoBehaviour for testing
- [ ] RoomManager: current room, visible set, transition triggers
- [x] DualGrid adaptation (registry-driven canonical tiles via base tilemap input)
