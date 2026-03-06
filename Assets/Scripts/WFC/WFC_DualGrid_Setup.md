# WFC → DualGrid Integration Setup

## Quick Setup

1. **Add components to scene**
   - Ensure your scene has a GameObject with `DungeonGeneration` (e.g. from Tools → WFC-DualGrid → Create New WFC Demo Scene).
   - Create a `DualGridTilemap` GameObject (or use Tools → WFC-DualGrid → Add Integration to Scene).

2. **Configure DungeonGeneration** (Inspector → DualGrid section)
   - Assign `Dual Grid Tilemap` → your DualGridTilemap component.
   - Assign `Biome Tile Registry` → your BiomeTileRegistry asset.
   - Set `Current Biome` (0-3 for now).

3. **Biome tiles (55 per biome)**
   - Create a BiomeTileSet asset (right-click → Create → Tables → Biome Tile Set).
   - Generate 55 canonical tiles in Aseprite (File → Scripts → Generate55CanonicalTiles), export as 11×5 sheet.
   - Use **Tools → Biome Tiles → Slice 55-Tile Sheet to BiomeTileSet** to slice and populate.
   - Add the BiomeTileSet to your BiomeTileRegistry (index = biome ID).

4. **WFC tile assets**
   - DungeonGeneration's grass/dirt/path/etc. tiles must be **Tile** assets (not RuleTile).
   - DualGrid matches by reference; DungeonGeneration passes these as placeholders when DualGrid is configured.

## RoomTree Demo Notes (registry-driven DualGrid)

The RoomTree demo uses the same registry-driven DualGrid approach:

- A single **base tilemap** is built that covers the full dungeon bounds (rooms + corridors).
- `DualGridTilemap.inputTilemap` reads from that base tilemap.
- DualGrid uses the **BiomeTileRegistry / BiomeTileSet** to render the canonical blended floor.

Common gotchas:

- **Placeholder matching is by reference**: the tiles written into the base tilemap must be the same `Tile` assets assigned as placeholders on `DualGridTilemap`.
- **Void between rooms**: the RoomTree demo can write the space outside rooms as `Wall` in the base tilemap so the registry renders it as wall rather than “ground”.

## Execution Order

- DungeonGeneration: -100 (runs first)
- DualGridTilemap: 100 (runs last)

When both dualGridTilemap and biomeTileRegistry are assigned, DungeonGeneration wires DualGrid after WFC build, before DualGrid.Start runs.

## Optional

- `hideBaseLayerAfterDualGrid`: if true, hides the raw WFC layer so only DualGrid's blended floor is visible.
- Floor/wall tilemaps are created automatically by DungeonGeneration if not assigned.
- Press **G** to toggle raw WFC grid visibility for troubleshooting.
