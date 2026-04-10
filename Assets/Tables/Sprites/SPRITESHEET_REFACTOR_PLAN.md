# Spritesheet Refactor Plan

Refactor SpriteTable2D from single sprites to spritesheets. Frame 0 = icon. All frames serialized as pixel data.

---

## 1. SpriteTableSerialization

**Current:** `SpriteTableEntry(type, partition, key, pixels)` — pixels = single 64×64 RGBA = 16,384 bytes per entry.

**New:** Add frame count. Per-entry format: `[type][partition][key][frameCount][pixels]` where pixels = `frameCount × 64 × 64 × 4`.

- Add `FormatV2` for spritesheet support (keep V1 for potential migration; we'll use V2 only).
- `SpriteTableEntry`: add `int FrameCount` (or `byte` for 0–255 frames).
- `BytesPerFrame` = 64×64×4 = 16,384 bytes per frame.
- `BytesPerEntry` = `frameCount × BytesPerFrame`.
- Max frame count: 64 (1 byte) or 256 (2 bytes). Recommend 1 byte (0–255).
- Serialize: `[1B version][4B width][4B height][4B count][per entry: 1B type, 1B partition, 1B key, 1B frameCount, frameCount*BytesPerFrame pixels]`.

---

## 2. SpritePixelConversion

**Add:**

- `SpriteToPixels(Sprite[] sprites, int frameWidth, int frameHeight)` → `byte[]` (concatenate all frames).
- `PixelsToSprites(byte[] pixels, int frameWidth, int frameHeight, int frameCount)` → `Sprite[]` (create one Texture2D per frame, or one Texture2D with all frames and slice).

**Note:** `PixelsToSprite` creates a new Texture2D per frame. For spritesheets we'd create N textures (one per frame) or one large texture. Simpler: one Texture2D per frame, each 64×64. Same as current behavior but repeated.

---

## 3. SpriteTable2D

**Storage:** `Sprite[] _sprites` → `Sprite[][] _spritesheets` (or `List<Sprite>[]`). Each slot = `Sprite[]` or `null`.

**API changes:**

| Method | Current | New |
|--------|---------|-----|
| `Get(type, biome, key)` | Returns `Sprite` | Returns `Sprite` (frame 0) — same signature |
| `GetFrame(type, biome, key, frameIndex)` | — | **New** — returns `Sprite` at frame |
| `GetSpritesheet(type, biome, key)` | — | **New** — returns `Sprite[]` |
| `SetAt(type, biome, key, Sprite sprite)` | Single sprite | `SetAt(type, biome, key, Sprite[] frames)` |
| `ClearAt` | — | Unchanged |
| `GetKeyOf` | Compares sprite | Compare frame 0 of spritesheet |
| `GetUsedSlotCount` | Count non-null | Count non-null (slot has spritesheet) |
| `SaveFromFile` | One sprite → pixels | All frames → concatenated pixels |
| `LoadFromFile` | Pixels → one sprite | Pixels → Sprite[] |

**EnsureCapacity:** `_spritesheets` array of `Sprite[][]`; each element can be `null` or `Sprite[]`.

---

## 4. PackedItemTable

**Add:** `Add(..., Sprite[] spritesheet, ...)` — replace `Sprite sprite`.

**Update:** `Update(..., Sprite[] spritesheet, ...)` — replace `Sprite sprite`.

**Overload:** `Add(..., Sprite sprite, ...)` → `Add(..., new[] { sprite }, ...)` for single-frame convenience.

---

## 5. ItemAuthoringWindow

**Sprite picker:** Currently `ObjectField` for single `Sprite`.

**Options:**

- **A) Texture2D + slice:** User assigns Texture2D. Slice by fixed 64×64 grid. `Texture2D.width/64` × `Texture2D.height/64` = frame count. Use `Sprite.Create(texture, rect, pivot)` per frame.
- **B) Sprite[] from sub-assets:** User assigns Texture2D. Use `AssetDatabase.LoadAllAssetsAtPath` + filter for Sprite to get Unity’s pre-sliced sprites.
- **C) Multi-object field:** Custom list for Sprite[]. More work.

**Recommendation:** A (Texture2D + slice). Simple, no asset dependencies. User imports spritesheet; we slice at 64×64.

**Edit form:** Show Texture2D field or sprite array. For edit, show frame 0 as preview; allow reassigning spritesheet.

**Grid preview:** `Get(type, biome, key)` still returns frame 0 — no change.

---

## 6. SpriteTableStorage

- No change if it just passes entries to serialization.
- Serialization format changes; `SpriteTableStorage` stays generic.

---

## 7. Files to Modify

| File | Changes |
|------|---------|
| `SpriteTableSerialization.cs` | Add FormatV2, frameCount, variable-length pixels |
| `SpritePixelConversion.cs` | Add `SpriteToPixels(Sprite[])`, `PixelsToSprites(byte[], frameCount)` |
| `SpriteTable2D.cs` | `Sprite[][]` storage, `SetAt(Sprite[])`, `GetFrame`, `GetSpritesheet` |
| `PackedItemTable.cs` | `Add(..., Sprite[] spritesheet, ...)`, `Update(..., Sprite[] spritesheet, ...)` |
| `ItemAuthoringWindow.cs` | Texture2D picker, slice to Sprite[], pass to Add/Update |

---

## 8. Optional: Single Frame Backwards

- Single-frame items: `Sprite[]` with length 1.
- `Get()` returns `frames[0]`.
- Serialization: `frameCount = 1`, same format.

---

## 9. Order of Implementation

1. **SpritePixelConversion** — add multi-frame convert.
2. **SpriteTableSerialization** — format V2, frameCount.
3. **SpriteTable2D** — storage + API.
4. **PackedItemTable** — Add/Update signatures.
5. **ItemAuthoringWindow** — Texture2D picker + slice logic.
