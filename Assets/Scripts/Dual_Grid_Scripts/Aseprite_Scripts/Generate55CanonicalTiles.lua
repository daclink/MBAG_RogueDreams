--[[
  Generate 55 Canonical Tiles from 4 Base Tiles (rotation + reflection)
  MBAG RogueDreams DualGrid System

  USAGE:
  1. Open an Aseprite file with exactly 4 frames (one per tile type)
  2. Frame order: 1=Ground, 2=Wall, 3=Water, 4=Grass
  3. Run: File > Scripts > Generate55CanonicalTiles
  4. A new sprite with 55 tiles will open.

  INPUT: 4 full-size tiles (64x64 default). Each tile is one frame.
  OUTPUT: 55 tiles, one per D4 orbit (4 rotations × reflection).

  Type indices (match DualGridTileType.cs):
    Ground=0, Wall=1, Water=2, Grass=3
--]]

-- ============ Configuration ============
local TILE_SIZE = 64
local HALF = TILE_SIZE / 2

-- ============ Canonical Key Generation (D4: rotation + reflection) ============
-- Key format: bl | (br<<2) | (tl<<4) | (tr<<6)
-- Rotate 90° CW: new_bl=br, new_br=tr, new_tr=tl, new_tl=bl
-- Reflect H (L-R): bl<->br, tl<->tr
local function rotate90(key)
  local bl = key & 3
  local br = (key >> 2) & 3
  local tl = (key >> 4) & 3
  local tr = (key >> 6) & 3
  return br | (tr << 2) | (bl << 4) | (tl << 6)
end

local function reflect(key)
  local bl = key & 3
  local br = (key >> 2) & 3
  local tl = (key >> 4) & 3
  local tr = (key >> 6) & 3
  return br | (bl << 2) | (tr << 4) | (tl << 6)
end

local function orbit8(key)
  local seen = {}
  local x = key
  for _ = 1, 4 do
    seen[x] = true
    x = rotate90(x)
  end
  x = reflect(key)
  for _ = 1, 4 do
    seen[x] = true
    x = rotate90(x)
  end
  return seen
end

local function isCanonical(key)
  local orb = orbit8(key)
  local min = key
  for k, _ in pairs(orb) do
    if k < min then min = k end
  end
  return key == min
end

local function distinctCornerTypes(key)
  local bl = key & 3
  local br = (key >> 2) & 3
  local tl = (key >> 4) & 3
  local tr = (key >> 6) & 3
  local seen = {}
  seen[bl] = true; seen[br] = true; seen[tl] = true; seen[tr] = true
  local n = 0
  for _ in pairs(seen) do n = n + 1 end
  return n
end

-- Build list of 55 canonical keys
local canonicalKeys = {}
for key = 0, 255 do
  if isCanonical(key) then
    canonicalKeys[#canonicalKeys + 1] = key
  end
end

table.sort(canonicalKeys, function(a, b)
  local da, db = distinctCornerTypes(a), distinctCornerTypes(b)
  if da ~= db then return da < db end
  return a < b
end)

-- ============ Quadrant rects (Aseprite: y=0 at top) ============
local QUAD = {
  bl = { x = 0, y = HALF, w = HALF, h = HALF },
  br = { x = HALF, y = HALF, w = HALF, h = HALF },
  tl = { x = 0, y = 0, w = HALF, h = HALF },
  tr = { x = HALF, y = 0, w = HALF, h = HALF },
}

local function getCorner(key, corner)
  if corner == "bl" then return (key) & 3 end
  if corner == "br" then return (key >> 2) & 3 end
  if corner == "tl" then return (key >> 4) & 3 end
  if corner == "tr" then return (key >> 6) & 3 end
  return 0
end

local function buildTile(sources, key, colorMode)
  local out = Image(sources[1])
  out:clear()
  for _, corner in ipairs({ "bl", "br", "tl", "tr" }) do
    local typeIdx = getCorner(key, corner)
    local srcImg = sources[typeIdx + 1]
    if not srcImg then
      app.alert("Missing source for type " .. tostring(typeIdx))
      return nil
    end
    local r = QUAD[corner]
    local srcRect = Rectangle(r.x, r.y, r.w, r.h)
    local srcQuad = Image(srcImg, srcRect)
    if srcQuad then
      out:drawImage(srcQuad, Point(r.x, r.y))
    end
  end
  return out
end

-- ============ Main ============
local function main()
  local src = app.sprite
  if not src then
    app.alert("No sprite open. Open a 4-frame sprite (Ground, Wall, Water, Grass) first.")
    return
  end

  if #src.frames < 4 then
    app.alert("Sprite must have at least 4 frames.\nFrame 1=Ground, 2=Wall, 3=Water, 4=Grass")
    return
  end

  if src.width < TILE_SIZE or src.height < TILE_SIZE then
    app.alert("Each frame must be at least " .. TILE_SIZE .. "x" .. TILE_SIZE .. " pixels.")
    return
  end

  app.transaction("Generate 55 Canonical Tiles", function()
    local layer = src.layers[1]
    if not layer then
      app.alert("Sprite has no layers.")
      return
    end

    local sources = {}
    for i = 1, 4 do
      for _, c in ipairs(src.cels) do
        if c.layer == layer and c.frame.frameNumber == i then
          sources[i] = Image(c.image)
          break
        end
      end
      if not sources[i] then
        app.alert("Missing cel for frame " .. tostring(i))
        return
      end
    end

    local colorMode = src.colorMode
    local outSprite = Sprite(TILE_SIZE, TILE_SIZE, colorMode)
    local base = (src.filename and src.filename ~= "") and src.filename:gsub("%.aseprite$", ""):gsub("%.ase$", "") or "canonical_tiles"
    outSprite.filename = base .. "_55canonical.aseprite"

    if colorMode == ColorMode.INDEXED and src.palettes[1] then
      outSprite:setPalette(src.palettes[1])
    end

    local outLayer = outSprite.layers[1]

    local function getCel(spr, lay, frameNum)
      for _, c in ipairs(spr.cels) do
        if c.layer == lay and c.frame.frameNumber == frameNum then
          return c
        end
      end
      return nil
    end

    local img0 = buildTile(sources, canonicalKeys[1], colorMode)
    if not img0 then return end

    local cel0 = getCel(outSprite, outLayer, 1)
    if cel0 then cel0.image = img0 end

    for i = 2, 55 do
      outSprite:newEmptyFrame(i)
      local img = buildTile(sources, canonicalKeys[i], colorMode)
      if not img then return end
      outSprite:newCel(outLayer, outSprite.frames[i], img, Point(0, 0))
    end

    app.sprite = outSprite
    app.alert("Created 55 canonical tiles (rotation + reflection). Export as 11×5 sheet.")
  end)
end

main()
