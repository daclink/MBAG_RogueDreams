# Room Tree → DualGrid Pipeline (with Canonical Keys)

This doc describes the entire **Room Tree demo pipeline** end‑to‑end and how it feeds into the **DualGrid** canonical tile system, including how keys, orbits, and rotations/reflections work.

---

## 1. High-level flow

At a very high level:

1. **RoomTreeDungeonComponent** (MonoBehaviour)
   - Creates a **RoomTreeDungeonGenerator** with `gridSize` and `randomSeed`.
   - Asks it to generate rooms and corridors.
   - Builds Unity tilemaps from the generator’s data.
   - Optionally builds a **base tilemap** for DualGrid.
   - Optionally wires a **DualGridTilemap** using the BiomeTileRegistry.

2. **RoomTreeDungeonGenerator**
   - Builds a **spanning tree** over a `gridSize × gridSize` grid of rooms.
   - Assigns special rooms (Start, End, Item).
   - Chooses **door positions per edge** (door start index on each wall).
   - Runs **RoomWFCTilemap** for each room to get a 10×10 `TileType[,]`:
     - perimeter walls
     - doors
     - a 1-tile-wide persistent path that connects doors inside the room
     - an “organic” interior shape carved around the path
     - WFC to fill terrain types in the interior
   - Fills a **single global corridor tilemap** that connects doors between neighboring rooms.

3. **RoomTreeDungeonComponent** builds Unity tilemaps:
   - One **Tilemap per room**, using each room’s 10×10 `TileType[,]`.
   - One **corridor Tilemap** (global).
   - One **base Tilemap** for DualGrid input (rooms + corridors, and optionally “void as wall”).

4. **DualGridTilemap** (if configured)
   - Reads the base tilemap cell types via WFC placeholder tiles.
   - Reduces them to 4 DualGrid types (Ground, Wall, Water, Grass).
   - Builds an 8‑bit **corner key** per dual cell.
   - Maps the key into a canonical index + rotation + reflection using **D₄ orbits**.
   - Uses a **BiomeTileSet** (via **BiomeTileRegistry**) to pick the canonical sprite and transform and renders the final floor/wall tiles.

So the full chain is:

> RoomTree spanning tree → per‑room WFC → per‑room + corridor tilemaps → single base tilemap → DualGrid keys/orbits → canonical tiles.

---

## 2. Room Tree generation details

### 2.1 Spanning tree & grid

Classes:

- `RoomTreeSpanningTreeBuilder`
- `RoomTreeDungeonGenerator`
- `RoomTreeNode`

Key facts:

- Grid is `gridSize × gridSize` (default 4×4).
- For each grid coordinate `(gx, gy)` we create a `RoomTreeNode` with:
  - `GridPosition = (gx, gy)`
  - `Depth` (set by BFS from the root)
  - `RoomType` (Normal, Start, End, Item)
  - `TileData` (`TileType[10,10]`) – filled later by `RoomWFCTilemap`
  - `Neighbors[0..3]` for N/E/S/W links
  - `DoorStarts[0..3]` – door offsets per side

The spanning tree builder:

- Chooses a random **root** room.
- Grows a **spanning tree** over the grid using 4‑directional neighbors.
- Prefers branching (bias toward nodes that already have edges).
- Assigns `Depth` to all nodes via BFS.

### 2.2 Special rooms & door positions

`RoomTreeDungeonGenerator`:

- Marks:
  - `RoomType.Start` = root node.
  - `RoomType.End` = deepest node.
  - Some `RoomType.Item` rooms at selected depths (2, 4, 8, …) with diminishing probability.

- **Door starts per edge:**
  - For each **tree edge** A–B, we:
    - Choose a random `start` index along the shared wall (`RoomTreeLayout.RandomDoorStart()`).
    - Write it into both nodes:
      - `room.DoorStarts[dirFromA] = start;`
      - `neighbor.DoorStarts[oppositeDir] = start;`
  - This guarantees that the doors on both sides of a corridor are aligned.

### 2.3 RoomWFCTilemap – organic room with persistent path

`RoomWFCTilemap` runs per room and returns a 10×10 `TileType[,]`.

Constants:

- `RoomSize = 10`
- Outer walls at indices 0 and 9 (perimeter ring).
- `PathWidth = RoomTreeLayout.PathWidth` (currently 1).

Generation steps:

1. **Initialize superposition**
   - For each cell `(x, y)` in 10×10, set possible tile types to:
     - {Grass, Dirt} (and Water if allowed).
   - **Path is not in the random superposition**; it is carved deterministically.

2. **PrePlaceStaticTiles()**
   - Place perimeter walls at:
     - (x, 0), (x, 9) for all x
     - (0, y), (9, y) for all y
   - Place door tiles in walls at the positions from `DoorStarts`:
     - North: (start .. start+PathWidth‑1, 9)
     - South: (start .. start+PathWidth‑1, 0)
     - East:  (9, start .. start+PathWidth‑1)
     - West:  (0, start .. start+PathWidth‑1)
   - Carve a **persistent 1‑tile‑wide path** that connects all door entries inside the room:
     - Compute interior “entry” positions (just inside the walls).
     - Choose a **hub** near the center (clamped to interior).
     - For each entry, carve an L‑shaped Manhattan path entry → hub.
     - Each carved cell is set to `TileType.Path` and marked pre‑placed.

3. **Organic interior shape**
   - Run a **BFS from all path cells** to compute their **distance** to the nearest path.
   - For each interior cell `(x, y)`:
     - If it’s **farther** than some threshold (e.g. 2 tiles) from any path:
       - Pre‑place it as `TileType.Wall`.
   - Result: walls “hug” the path; the walkable interior is a blob around the path rather than a fixed 8×8 rectangle.

4. **PropagateAllPrePlacedConstraints()**
   - For each pre‑placed cell (wall, door, path):
     - Call `WFCCore.Propagate` starting from that cell.
   - This enforces adjacency rules around the carved shape before any random collapse.

5. **WFC collapse**
   - While there are cells with >1 possibility:
     - Find a **lowest‑entropy** cell (using `WFCCore.FindLowestEntropyCell`).
     - Collapse it to a specific type using `WFCCore.CollapseCell` and weights.
     - Propagate constraints to neighbors.

6. **FinalizeTilemap**
   - Any cell whose superposition has been reduced to 1 possibility but not yet written to `collapsedTilemap` is finalized (filled with that type).
   - Conflicts or 0‑possibility cells fall back to safe defaults (e.g. Grass).

So each room ends up with:

- Walls on the perimeter + extra interior walls (organic shape).
- A single continuous 1‑wide path that connects all doors.
- Grass/Dirt/Water interior consistent with adjacency rules.

### 2.4 Corridor generation

`RoomTreeCorridorGenerator` builds a single global `TileType[,]` for corridors:

- For each tree edge A–B:
  - Use their **shared doorStart** and direction.
  - Carve a straight 1‑wide path from A’s door to B’s door across the gap.
- The corridor tilemap cells are set to `TileType.Path`.

---

## 3. Building Unity Tilemaps for Room Tree

`RoomTreeDungeonComponent` turns the generator’s data into Unity Tilemaps:

1. **One tilemap per room**

   - For each `RoomTreeNode`:
     - Create a new GameObject with `Tilemap + TilemapRenderer`.
     - Use `SetTilesBlock` over a 10×10 **BoundsInt** starting at `node.WorldPosition`.
     - Fill it with tiles from `GetTileAsset(node.TileData[x, y])`.

2. **Corridor tilemap**

   - Create one **CorridorTilemap** under the same Grid.
   - Use `SetTilesBlock` over the dungeon bounds computed from `RoomTreeCorridorGenerator.GetDungeonBounds(gridSize)`.
   - Set each cell to the `pathTile` if the corridor map has `TileType.Path`, otherwise null.

3. **Base tilemap for DualGrid (optional)**

   - Create a single **RoomTreeBaseTilemap** under the Grid.
   - For each cell `(x, y)` in the dungeon bounds:
     - Sample rooms:
       - If the cell falls inside any room’s 10×10 block, use that room’s `TileType`.
     - Overlay corridors:
       - If `CorridorTilemap[x, y] == TileType.Path`, override with `Path`.
     - Optionally treat “outside rooms” as `Wall` in this base tilemap so the void renders as wall in DualGrid.
   - Write all tiles in one `SetTilesBlock` call.

This base tilemap is what `DualGridTilemap` uses as its **inputTilemap**.

---

## 4. DualGrid corner keys and canonical tiles

Once the Room Tree base tilemap exists, the **DualGrid** system takes over. This part is shared between the WFC demo and the Room Tree demo.

### 4.1 From base tile to 4 DualGrid types

The Room Tree base tilemap uses `TileType` values:

- Empty, Grass, Dirt, Path, Water, Wall

DualGrid only uses 4 types:

- **Ground (0)** ← Empty, Dirt, Path
- **Wall (1)**   ← Wall
- **Water (2)**  ← Water
- **Grass (3)**  ← Grass

`DualGridTileTypeMapping.FromTileType()` performs this mapping.

### 4.2 Dual grid vs base grid

The base tilemap is on an integer grid `(x, y)`. The dual grid is the grid of **junctions** where four base cells meet. For each dual cell at integer coordinate `coords` in the DualGridTilemap, we sample:

- `bl =` base at `coords + (0, 0)`  (bottom-left)
- `br =` base at `coords + (1, 0)`  (bottom-right)
- `tl =` base at `coords + (0, 1)`  (top-left)
- `tr =` base at `coords + (1, 1)`  (top-right)

This ordering is the same in both:

- `DualGridTilemap.NEIGHBOURS`
- `Generate55CanonicalTiles.lua`’s QUAD layout.

### 4.3 Building the 8-bit key

Each corner is one of {Ground, Wall, Water, Grass} ⇒ values {0,1,2,3}. We pack the four corners into an 8‑bit integer:

```csharp
key = bl | (br << 2) | (tl << 4) | (tr << 6);
```

So:

- bits 0–1: bottom-left
- bits 2–3: bottom-right
- bits 4–5: top-left
- bits 6–7: top-right

There are 256 possible keys (0–255).

#### 4.3.1 How we unpack and rotate a key

When we need to manipulate a key (for `Rotate90` or `Reflect`), we always:

1. **Unpack** the 8 bits into four 2‑bit fields:

   ```csharp
   int bl =  key        & 0b11; // bits 0–1
   int br = (key >> 2) & 0b11; // bits 2–3
   int tl = (key >> 4) & 0b11; // bits 4–5
   int tr = (key >> 6) & 0b11; // bits 6–7
   ```

   Here `0b11` (decimal 3) is just a **mask** saying “keep only the lowest 2 bits.”

2. **Permute** those four corner values according to the geometric operation:

   - For a 90° clockwise rotation of the square:
     - new bl′ = old br
     - new br′ = old tr
     - new tr′ = old tl
     - new tl′ = old bl

3. **Pack** them back into an 8‑bit key:

   ```csharp
   // Rotate90 implementation
   return br | (tr << 2) | (bl << 4) | (tl << 6);
   ```

So `Rotate90` is not a raw “rotate these 8 bits” operation. It is:

> “unpack 4 × 2‑bit corners → reorder them as if we rotated the tile → pack back into 8 bits.”

### 4.4 D₄ orbits and 55 canonical keys

Many of those 256 keys are the same “shape” rotated or reflected. We consider the dihedral group \(D_4\) (symmetries of the square), generated by:

- `Rotate90(key)` — 90° clockwise rotation of corners.
- `Reflect(key)` — horizontal (left–right) reflection.

From any given key `k`, you can generate its **orbit**:

- `k, Rotate90(k), Rotate90^2(k), Rotate90^3(k)`
- `Reflect(k), Rotate90(Reflect(k)), Rotate90^2(Reflect(k)), Rotate90^3(Reflect(k))`

The orbit size is at most 8 (some patterns have smaller orbits).

We define the **canonical key** for an orbit as the **minimum integer** in that orbit. Running this over all 256 keys yields **55 distinct canonical keys**.

Both the Unity code (`DualGridCanonicalKeys`) and the Aseprite script (`Generate55CanonicalTiles.lua`) do the same:

1. Loop `key = 0..255`.
2. Keep `key` if `key == min(orbit(key))`.
3. Sort canonical keys by:
   - number of distinct corner types (fewer distinct types first),
   - numeric key value.

This sorted list is the **canonical order** (length 55).

### 4.5 Canonical tiles in Aseprite

The Aseprite script generates **55 frames**:

- Frame `i` corresponds to the `i`‑th canonical key in the canonical order.
- For each canonical key:
  - It looks at each corner (bl, br, tl, tr) as 0–3 (Ground, Wall, Water, Grass).
  - It copies the corresponding quadrant from the 4 base tiles into the correct position.
- No rotation/reflection is applied in Aseprite: each frame is in its **canonical orientation**.

In Unity, each frame `i` is imported into a **BiomeTileSet** as tile index `i` (0-based or 1-based depending on your indexing; the code uses 0–54).

### 4.6 From key to (canonicalIndex, rotation, reflected)

At runtime, for each dual cell we have a key `k`. We want:

- Which canonical tile to use.
- How to rotate/reflect it to display `k`.

`DualGridCanonicalKeys.GetCanonicalIndexRotationAndReflection(int key)` does this:

1. Finds the **canonical key** `min` in the orbit of `k`.
2. Finds the **index** `idx` of `min` in the canonical key list (0–54).
3. Determines whether `k` matches `min` via:
   - some number of rotations, or
   - a reflection plus some number of rotations.

It returns:

- `canonicalIndex = idx`
- `rotation` — 0–3, meaning **how many 90° CW rotations** on the **key** map it to `min`.
- `reflected` — `true` if `Reflect(key)` and its rotations are involved, `false` if only rotations.

Mathematically:

- If there exists `r` so that `Rotate90^r(key) = min`, then `reflected = false`, `rotation = r`.
- Else if there exists `r` so that `Rotate90^r(Reflect(key)) = min`, then `reflected = true`, `rotation = r`.

### 4.7 Applying the transform in Unity

We don’t rotate keys; we rotate sprites. For each dual cell:

1. `DualGridTilemap` computes key `k` and gets `(canonicalIndex, rotation, reflected)`.
2. It fetches the **canonical tile** from `BiomeTileSet`:

   ```csharp
   Tile tile = tileSet.GetTile(canonicalIndex);
   ```

3. It builds a transform matrix:

   ```csharp
   private static Matrix4x4 BuildTileTransform(int rotation, bool reflected)
   {
       Quaternion rot = Quaternion.Euler(0f, 0f, rotation * 90f); // CCW in Unity
       Vector3 scale = new Vector3(reflected ? -1f : 1f, 1f, 1f);
       return Matrix4x4.Scale(scale) * Matrix4x4.Rotate(rot);
   }
   ```

   - The matrix order `Scale * Rotate` ensures that, when applied to vertex coordinates, we effectively get the correct composition of reflection + rotation corresponding to the key’s relation to its canonical.
   - The sign of the rotation angle is chosen so that when key rotations are CW, sprite rotations are the inverse (CCW).

4. It writes the tile into the floor tilemap and applies the transform:

   ```csharp
   floorTilemap.SetTile(cellCoords, tile);
   floorTilemap.SetTransformMatrix(cellCoords, BuildTileTransform(rotation, reflected));
   ```

Thus, every dual cell’s 2×2 neighborhood is rendered as the appropriate canonical tile, rotated and reflected into place. This is what makes walls, water edges, and grass/dirt transitions line up nicely even though the Room Tree and WFC systems only reason in terms of **simple `TileType` grids**.

---

## 5. How it all fits together

Putting the pieces in order for the Room Tree demo:

1. **Spanning tree** decides which rooms exist and how they’re connected.
2. **RoomWFCTilemap**:
   - carves walls, doors, and a persistent path,
   - shapes the room interior around the path,
   - fills in terrain types with WFC.
3. **RoomTreeCorridorGenerator** connects doors with 1‑wide corridors.
4. **RoomTreeDungeonComponent** builds:
   - per‑room Tilemaps,
   - corridor Tilemap,
   - base Tilemap for DualGrid.
5. **DualGridTilemap**:
   - reads the base Tilemap using WFC placeholder tiles,
   - computes 8‑bit keys from 2×2 corners,
   - maps keys into D₄ canonical tiles + transforms,
   - renders final floor/wall canonical tiles using the BiomeTileRegistry/BiomeTileSet.

This separation lets you:

- change WFC rules or room shapes without touching DualGrid,
- change canonical art or biome palettes without touching WFC or Room Tree logic,
- and reason about correctness in two layers:
  - **grid-level** (Room Tree + WFC),
  - **render-level** (DualGrid keys/orbits/canonical tiles).

# DualGrid + WFC: How Tile Picking Works

This document explains how the **WFC base grid** feeds the **DualGrid** system, and how the DualGrid system chooses a tile **(canonical sprite + rotation + reflection)** so borders/corners line up.

## High-level overview

### The problem we’re solving

WFC (or the Room Tree generator) produces a **base grid** where each cell is a single type (grass, wall, water, etc.). Drawing a single “per-cell” tile often produces ugly seams at **boundaries** (wall vs floor) and incorrect **corners**.

DualGrid fixes that by rendering tiles on the **dual grid**: each DualGrid tile represents the **junction of four adjacent base cells**. That lets the renderer choose a tile based on the 2×2 neighborhood, producing clean edges and corners.

### The key idea

For every junction, we look at the **four corner types** and pack them into an **8-bit key** (0–255). In theory that could require 256 unique art tiles, but most keys are equivalent under **rotation** and **horizontal reflection**, so we only need **55 canonical tiles**.

At runtime we:

- Compute the 8-bit key for the junction.
- Map that key to:
  - **canonicalIndex** (0–54) into the 55-tile spritesheet
  - **rotation** (0–3) in 90° increments
  - **reflected** (bool) for a horizontal flip
- Place the canonical tile and apply the transform.

In one sentence:

**Base grid (WFC) → 4-corner key → canonical tile index + rotation/reflection → draw transformed sprite.**

## Detailed explanation

### 1) Base grid vs Dual grid

- **Base grid**: the grid WFC produces (cells at integer coordinates (x, y)).
- **Dual grid**: the grid of junctions where four base cells meet.

For a DualGrid position `coords` (a tile location on the DualGrid tilemap), the four base-cell samples are:

- **bl** (bottom-left): `coords + (0, 0)`
- **br** (bottom-right): `coords + (1, 0)`
- **tl** (top-left): `coords + (0, 1)`
- **tr** (top-right): `coords + (1, 1)`

This ordering matches both:

- `Assets/Scripts/Dual_Grid_Scripts/DualGridTilemap.cs` (`NEIGHBOURS`)
- `Assets/Scripts/Dual_Grid_Scripts/Aseprite_Scripts/Generate55CanonicalTiles.lua`

### 2) Reducing WFC TileTypes to 4 DualGrid types

WFC uses a richer set of tile types (e.g. Empty, Grass, Dirt, Path, Water, Wall). DualGrid uses a 4-type mask:

- **Ground (0)**: Empty, Dirt, Path
- **Wall (1)**
- **Water (2)**
- **Grass (3)**

This mapping is in:

- `Assets/Scripts/Shared/DualGridTileType.cs` (`DualGridTileTypeMapping.FromTileType`)

### 3) Building the 8-bit key

Each of the four corners is a value 0–3, so each fits in 2 bits. The key is packed like this:

```
key = bl | (br << 2) | (tl << 4) | (tr << 6)
```

So:

- bits 0–1: bl
- bits 2–3: br
- bits 4–5: tl
- bits 6–7: tr

This matches the Lua script and `DualGridCanonicalKeys.BuildKey(...)`.

### 4) Canonical keys and why there are 55

There are 256 possible keys, but many are the same “shape” rotated or mirrored.

We define two symmetry operations on keys:

- **Rotate 90° CW**: `Rotate90(key)`
- **Reflect horizontally (left–right flip)**: `Reflect(key)`

Those two operations generate an 8-element orbit (at most) for a key:

- 4 rotations of the key
- 4 rotations of the reflected key

We define a **canonical key** as the **minimum integer key** in that orbit. There are exactly **55** canonical keys (for 4 corner types), so we only need **55 canonical sprites**.

Both the Unity code and Aseprite script generate the canonical keys the same way:

- generate all 0–255 keys
- keep those that are canonical (min of orbit)
- sort by:
  1. number of distinct corner types
  2. key numeric value

This “canonical order” is important because it’s how:

- Aseprite writes out frames 1–55
- Unity indexes those frames via `canonicalIndex` (0–54)

### 5) Aseprite: how the 55 sprites are authored

`Generate55CanonicalTiles.lua` produces 55 frames where each frame corresponds to a canonical key.

For each canonical key, it:

- splits a tile into quadrants (bl, br, tl, tr)
- draws the appropriate base-type image into each quadrant

Important: **Aseprite does not apply rotation or reflection.** It simply outputs each canonical tile in its canonical orientation.

### 6) Unity: key → (canonicalIndex, rotation, reflected)

In Unity:

- `DualGridTilemap` computes the key for each dual-grid position.
- `DualGridCanonicalKeys.GetCanonicalIndexRotationAndReflection(key)` returns:
  - `canonicalIndex`: which of the 55 canonical tiles
  - `rotation`: 0–3 steps
  - `reflected`: whether the key is in the reflected half of the orbit

The key point: the function finds the orbit minimum `min`, and also returns the `(rotation, reflected)` needed to relate `key` to `min` based on the same symmetry definitions used by Lua.

### 7) Unity: applying the transform correctly

Unity must take the canonical sprite (the one for `min`) and transform it so that it visually matches the original `key`.

The `rotation` returned by `GetCanonicalIndexRotationAndReflection` corresponds to how many **90° CW key-rotations** are needed to reduce `key` down to `min`.

But for display we want the inverse: starting from canonical (`min`) and getting back to `key`.

Because `Rotate90` on a key corresponds to rotating the sprite 90° CW, the inverse rotation is 90° **CCW**. So in Unity we apply:

- rotation angle \(= +rotation * 90°\) (CCW in Unity’s Z rotation)
- optional horizontal reflection with scaleX = -1

Matrix order matters because reflection and rotation do not commute. The implementation uses:

```
Matrix = Scale * Rotate
```

so the point is rotated first, then reflected (equivalently matching the chosen convention and the way canonicalization is computed).

The correct implementation lives in:

- `Assets/Scripts/Dual_Grid_Scripts/DualGridTilemap.cs` (`BuildTileTransform`)

### 8) Where DualGrid reads from WFC

WFC and Room Tree both ultimately write a **Tilemap** that DualGrid reads as `inputTilemap`.

For the regular WFC demo:

- `DungeonGeneration.BuildUnityTilemap(...)` uses `SetTilesBlock` with `block[x + y * width]`.

For the Room Tree demo:

- The code builds a single `_baseTilemap` (used as DualGrid input) and also uses `SetTilesBlock`.

So any placement issues that remain (after fixing transforms) typically come from:

- wrong placeholder-tile mapping (tile references not matching the WFC tiles)
- wrong canonical index order (Aseprite export not matching Unity canonical ordering)
- incorrect rotation/reflection transform math (the most common culprit)

## Practical debugging tips

- Turn on `Log Dual Grid Diagnostics` in `DualGridTilemap` to see:
  - per-cell samples: key → canonicalIndex/rotation/reflection
  - a summary of whether any canonicals are missing (fallback)
- When the summary shows “no gaps” but visuals are wrong, the issue is almost always:
  - rotation sign (CW vs CCW)
  - transform order (Scale/Rotate order)
  - placeholder tile references not matching the base tilemap’s tile assets

