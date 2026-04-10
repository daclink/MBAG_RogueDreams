# DualGrid + Biome Tile System - Design Specification

## Overview

- WFC outputs dungeon layout → DualGrid consumes it with 4-bit connectivity mask
- One 16-tile sheet per biome, 4 biomes now, 16 max later
- Per-biome tables + registry (no database)
- One biome per dungeon (chosen before WFC, not yet implemented)

---

## Mask & Bit Layout

**Bit order** (4 corners, each = 0 or 1 for "matches primary"):

- **bit 0** → bottom-left
- **bit 1** → bottom-right
- **bit 2** → top-left
- **bit 3** → top-right

**Formula:**

```csharp
mask = (botLeft ? 1 : 0) | ((botRight ? 1 : 0) << 1) | ((topLeft ? 1 : 0) << 2) | ((topRight ? 1 : 0) << 3);
```

**4×4 sprite sheet** (row-major):

- `index = col + row * 4` (0–15)
- Tile at (col, row) ↔ mask value `col + row * 4`

---

## Type System

### WFC Output (6 types)

Empty, Grass, Dirt, Path, Water, Wall

### DualGrid Input (4 types)

Ground, Wall, Water, Grass

### Mapping (WFC → DualGrid)

- **Ground** ← Dirt, Path, Empty
- **Wall** ← Wall
- **Water** ← Water
- **Grass** ← Grass

### Primary Type Priority (when corners differ)

`Wall > Water > Grass > Ground`

---

## Data Structures

### BiomeTileSet

- ScriptableObject
- 16 `Tile` references (mask 0–15)
- One per biome

### BiomeTileRegistry

- ScriptableObject
- `BiomeTileSet[]` (length 4 now, 16 later)
- Maps biome index → tileset
- No separate database

### Runtime Lookup

`registry.GetBiomeSet(biomeIndex).GetTile(mask)`

---

## Slicing & Automation

- **Input**: 4×4 sprite sheet
- **Process**: Slice into 16 sprites → create/fill BiomeTileSet with mask 0–15
- **Convention**: (col, row) → index = col + row * 4
- Editor tool for automation

---

## Implementation Notes

- DualGridTilemap: mask-based lookup, no 1296-tile dictionary
- DungeonGeneration: optional dualGridTilemap + biomeTileRegistry + currentBiome (default 0)
- Biome selection placeholder until pre-WFC biome system exists
