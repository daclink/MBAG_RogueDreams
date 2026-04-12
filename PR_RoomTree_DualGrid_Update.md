## Summary

- **What does this PR change?**
  - Refines the **Room Tree dungeon generator** to support configurable grid size, 1‑tile‑wide persistent paths that connect doors through rooms and corridors, and more organic room interiors whose walls follow the path shape.
  - Fixes and documents the **DualGrid canonical tile pipeline**, including correct rotation/reflection handling, void-as-wall behavior between rooms, and better diagnostics.
  - Updates **documentation** (RoomTree spec, DualGrid system explanation, WFC→DualGrid setup) and adds a **GitHub PR template** for future work.
- **Why are these changes needed now?**
  - To make the Room Tree demo a more faithful prototype of the intended dungeon flow (navigable continuous paths, non‑rectangular rooms) and to solidify the canonical-tile pipeline so art/biome work is on a stable foundation.

## Technical details

- **RoomTree / WFC changes**
  - `RoomTreeDungeonGenerator` now takes a **configurable `gridSize`** (from `RoomTreeDungeonComponent`) instead of hardcoding 4×4; bounds and corridor generation use this value.
  - **Door positions** are chosen per tree edge, stored symmetrically on both rooms via `RoomTreeNode.DoorStarts` so each corridor connects perfectly aligned doors.
  - `RoomWFCTilemap`:
    - Keeps the fixed **10×10 footprint** but:
      - Places perimeter walls + door openings based on `DoorStarts`.
      - Carves a **persistent 1‑tile‑wide path** from each door entry into the room to a central hub.
      - Uses a BFS distance field from the path to turn “far” interior tiles into walls, giving an **organic room shape** whose walls follow the path.
      - Removes `TileType.Path` from the random superposition so paths are deterministic; uses WFC only for interior terrain (Grass/Dirt/Water) around the carved shape.
      - Propagates constraints from all pre‑placed cells (walls/doors/path) before normal WFC collapse to keep adjacency rules consistent.
- **DualGrid / canonical tiles changes**
  - `DualGridTilemap.BuildTileTransform` now uses the correct **rotation sign and matrix order** so that:
    - The rotation is the inverse of the key’s CW rotations (CCW in Unity).
    - Reflection + rotation matches the D₄ orbit logic used by `DualGridCanonicalKeys` and the Aseprite script.
  - Added options and behaviors for how the **void between rooms** is treated:
    - `RoomTreeDungeonComponent` can write outside‑room cells as `Wall` in the DualGrid base tilemap.
    - `DualGridTilemap` can treat null / empty placeholder tiles as `Wall` for key‑building, which makes the canonical tiles render proper walls around the room grid.
  - Improved DualGrid **diagnostics**:
    - Optional logging of per‑cell key → canonical index / rotation / reflection samples.
    - Summary of fallback usage and missing canonical indices when BiomeTileSet entries are missing.
  - Expanded `DualGrid_System_Explanation.md` to fully document:
    - The Room Tree → base tilemap → DualGrid pipeline.
    - 8‑bit key packing/unpacking, `Rotate90`/`Reflect`, orbits, canonical minima, and how transforms are applied.
- **Other systems touched**
  - Updated `RoomTreeDungeon_Spec.md`, `RoomTree_Display_Issue.md`, and `WFC_DualGrid_Setup.md` to match the new behavior (grid size, spacing, path width, organic rooms, DualGrid wiring).
  - Added `.github/pull_request_template.md` to standardize future PR descriptions.
  
## Testing

- **Manual testing**
  - [ ] RoomTree demo scene runs without errors, generates rooms and corridors at different `gridSize` values.
  - [ ] Paths are **1 tile wide** and are visually continuous from room → corridor → adjacent room for all edges.
  - [ ] Room interiors show **organic wall shapes** hugging the path, rather than uniform 8×8 blocks.
  - [ ] Regular WFC demo scene runs without errors and still renders correctly.
  - [ ] DualGrid renders with:
    - aligned walls/borders (no jagged mis-rotated tiles),
    - correct void‑as‑wall behavior between rooms (when enabled),
    - no warnings about missing canonical tiles when BiomeTileSet is properly populated.
- **Automated tests**
  - [ ] All existing tests pass.
  - [ ] (Optional) New unit tests added for:
    - `RoomTreeLayout` constants.
    - `RoomWFCTilemap` path carving / organic interior logic.
    - `DualGridCanonicalKeys` rotation/reflect mapping (if desired).

**Suggested manual test steps:**

1. Open the **Room Tree demo scene**.
2. Vary `gridSize` (e.g. 3, 4, 5) on `RoomTreeDungeonComponent` and click **Generate Room Tree Dungeon**.
3. In Scene/Game view, verify:
   - Each room has a 1‑wide path that:
     - emerges from each door,
     - connects through the room,
     - and lines up with the corridor between rooms.
   - Room shapes look organic (walls bend around the path).
4. Enable the DualGrid integration for both the regular WFC demo and the Room Tree demo:
   - Confirm walls and edges are aligned and the void between rooms renders as wall (if the corresponding toggles are enabled).
5. Check the Console for:
   - absence of exceptions,
   - reasonable DualGrid diagnostic summaries when enabled (no “gaps” if BiomeTileSet is complete).

## Screenshots / GIFs (optional)

- [ ] Before/after of a Room Tree dungeon showing:
  - old rectangular 8×8 rooms vs organic rooms,
  - 2‑wide vs 1‑wide paths,
  - improved DualGrid wall rendering.

## Risks & roll-back plan

- **Potential side effects**
  - Changes to `RoomWFCTilemap` and `RoomTreeDungeonGenerator` may alter the “feel” of generated dungeons (denser walls, narrower paths).
  - DualGrid changes affect how canonical tiles are transformed; if BiomeTileSets were imported with a mismatched canonical order, misalignments might surface more clearly.
- **How to roll back**
  - Revert this PR to restore:
    - previous rectangular 8×8 room shapes,
    - 2‑wide paths and original spacing,
    - earlier DualGrid transform behavior and docs.

## Checklist

- [ ] Code builds and runs locally
- [ ] New/changed behavior is documented (`DualGrid_System_Explanation.md`, `RoomTreeDungeon_Spec.md`, `WFC_DualGrid_Setup.md`)
- [ ] New/updated public APIs and assets follow existing naming/structure conventions
- [ ] No leftover debug logs or temporary code

