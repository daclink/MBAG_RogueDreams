# Generate 55 Canonical Tiles — Aseprite Script

Builds the 55 D4-symmetric tile variants (rotation + reflection) from your 4 base tiles.

## What You Provide

### 1. A single Aseprite file with 4 frames

- **Frame 1** = Ground tile (full 64×64)
- **Frame 2** = Wall tile (full 64×64)
- **Frame 3** = Water tile (full 64×64)
- **Frame 4** = Grass tile (full 64×64)

Each frame must be at least **64×64 pixels** (change `TILE_SIZE` in the script if you use another size).

### 2. Layout of each tile

Each base tile is split into four quadrants:

```
+----+----+
| TL | TR |
+----+----+
| BL | BR |
+----+----+
```

- **TL** = top-left (0,0)–(31,31)
- **TR** = top-right (32,0)–(63,31)
- **BL** = bottom-left (0,32)–(31,63)
- **BR** = bottom-right (32,32)–(63,63)

For each of the 55 output tiles, the script takes the correct quadrant from each of the 4 base tiles and pastes it into that position. Each output tile shows the correct mix of Ground / Wall / Water / Grass in its four corners.

## How to Run

1. In Aseprite, open your 4-frame sprite file.
2. Go to **File > Scripts > Open Scripts Folder** and copy `Generate55CanonicalTiles.lua` there.
3. Run **File > Scripts > Generate55CanonicalTiles**.
4. A new sprite with 55 frames will open.

## Output

- **55 frames**, one per D4 canonical key (rotation + reflection).
- Each frame is a 64×64 tile.
- Export as an **11×5** sprite sheet (704×320 px), then slice in Unity with **Tools → Biome Tiles → Slice 55-Tile Sheet to BiomeTileSet**.

## Customization

Edit the script and change:

```lua
local TILE_SIZE = 64
```

if your tiles are, for example, 16×16 or 32×32.
