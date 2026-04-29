--[[
  Generate Canonical Tiles from N Base Tiles (rotation + reflection, D4)
  MBAG RogueDreams DualGrid System

  USAGE:
  1. Open an Aseprite file with at least TYPE_COUNT frames (one per tile type 0..TYPE_COUNT-1)
  2. Run: File > Scripts > Generate55CanonicalTiles
  3. A new sprite with all canonical tiles will open.

  INPUT: TYPE_COUNT full-size tiles (64x64 default). Each tile is one frame.
  OUTPUT: One tile per D4 orbit (4 rotations × reflection).

  Recommended TYPE_COUNT values: 2, 3, or 4.
  For TYPE_COUNT = 4 and frame order Ground=0, Wall=1, Water=2, Grass=3,
  the output matches the original 55 canonical tiles.
--]]

-- ============ Configuration ============
local TILE_SIZE = 64
local HALF = TILE_SIZE / 2
-- Number of distinct tile types (2, 3, or 4).
local TYPE_COUNT = 4

-- ============ Canonical Key Generation (D4: rotation + reflection) ============
-- We treat the 4 corners as digits in base TYPE_COUNT:
-- corners = [bl, br, tl, tr], each in 0..TYPE_COUNT-1.
-- Key = bl + br*TYPE_COUNT + tl*TYPE_COUNT^2 + tr*TYPE_COUNT^3.

local function getCornerDigit(key, index)
  -- index: 0=bl, 1=br, 2=tl, 3=tr
  local base = TYPE_COUNT
  for _ = 1, index do
    key = math.floor(key / base)
  end
  return key % base
end

local function keyFromCorners(bl, br, tl, tr)
  local base = TYPE_COUNT
  return bl + br * base + tl * base * base + tr * base * base * base
end

-- Rotate 90° CW: new_bl=br, new_br=tr, new_tr=tl, new_tl=bl
local function rotate90(key)
  local bl = getCornerDigit(key, 0)
  local br = getCornerDigit(key, 1)
  local tl = getCornerDigit(key, 2)
  local tr = getCornerDigit(key, 3)
  return keyFromCorners(br, tr, bl, tl)
end

-- Reflect H (L-R): bl<->br, tl<->tr
local function reflect(key)
  local bl = getCornerDigit(key, 0)
  local br = getCornerDigit(key, 1)
  local tl = getCornerDigit(key, 2)
  local tr = getCornerDigit(key, 3)
  return keyFromCorners(br, bl, tr, tl)
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
  local seen = {}
  local bl = getCornerDigit(key, 0)
  local br = getCornerDigit(key, 1)
  local tl = getCornerDigit(key, 2)
  local tr = getCornerDigit(key, 3)
  seen[bl] = true; seen[br] = true; seen[tl] = true; seen[tr] = true
  local n = 0
  for _ in pairs(seen) do n = n + 1 end
  return n
end

-- Global canonical keys (rebuilt each run in main, after TYPE_COUNT is chosen)
local canonicalKeys = {}

-- ============ Quadrant rects (Aseprite: y=0 at top) ============
local QUAD = {
  bl = { x = 0, y = HALF, w = HALF, h = HALF },
  br = { x = HALF, y = HALF, w = HALF, h = HALF },
  tl = { x = 0, y = 0, w = HALF, h = HALF },
  tr = { x = HALF, y = 0, w = HALF, h = HALF },
}

local function getCorner(key, corner)
  if corner == "bl" then return getCornerDigit(key, 0) end
  if corner == "br" then return getCornerDigit(key, 1) end
  if corner == "tl" then return getCornerDigit(key, 2) end
  if corner == "tr" then return getCornerDigit(key, 3) end
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
    app.alert("No sprite open. Open a sprite with TYPE_COUNT frames (one per tile type) first.")
    return
  end

  -- Ask user for TYPE_COUNT (2, 3, or 4). Defaults to current TYPE_COUNT value.
  local dlg = Dialog{ title = "Generate Canonical Tiles (D4)" }
  dlg:number{
    id = "typeCount",
    label = "Tile types",
    text = tostring(TYPE_COUNT),
    min = 2,
    max = 4
  }
  dlg:button{ id = "ok", text = "OK" }
  dlg:button{ id = "cancel", text = "Cancel" }
  dlg:show()

  local data = dlg.data
  if not data or not data.typeCount then
    return
  end

  TYPE_COUNT = math.floor(tonumber(data.typeCount) or TYPE_COUNT)

  if TYPE_COUNT < 2 or TYPE_COUNT > 4 then
    app.alert("TYPE_COUNT must be 2, 3, or 4.")
    return
  end

  if #src.frames < TYPE_COUNT then
    app.alert("Sprite must have at least " .. TYPE_COUNT .. " frames (one per tile type).")
    return
  end

  -- Rebuild canonical keys for this TYPE_COUNT
  canonicalKeys = {}
  local KEY_COUNT = TYPE_COUNT ^ 4
  for key = 0, KEY_COUNT - 1 do
    if isCanonical(key) then
      canonicalKeys[#canonicalKeys + 1] = key
    end
  end

  table.sort(canonicalKeys, function(a, b)
    local da, db = distinctCornerTypes(a), distinctCornerTypes(b)
    if da ~= db then return da < db end
    return a < b
  end)

  if src.width < TILE_SIZE or src.height < TILE_SIZE then
    app.alert("Each frame must be at least " .. TILE_SIZE .. "x" .. TILE_SIZE .. " pixels.")
    return
  end

  app.transaction("Generate Canonical Tiles (D4)", function()
    local layer = src.layers[1]
    if not layer then
      app.alert("Sprite has no layers.")
      return
    end

    local sources = {}
    for i = 1, TYPE_COUNT do
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
    outSprite.filename = base .. "_canonical_D4.aseprite"

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

    local count = #canonicalKeys
    if count == 0 then
      app.alert("No canonical keys found for TYPE_COUNT=" .. tostring(TYPE_COUNT))
      return
    end

    local img0 = buildTile(sources, canonicalKeys[1], colorMode)
    if not img0 then return end

    local cel0 = getCel(outSprite, outLayer, 1)
    if cel0 then cel0.image = img0 end

    for i = 2, count do
      outSprite:newEmptyFrame(i)
      local img = buildTile(sources, canonicalKeys[i], colorMode)
      if not img then return end
      outSprite:newCel(outLayer, outSprite.frames[i], img, Point(0, 0))
    end

    app.sprite = outSprite
    app.alert("Created " .. tostring(count) .. " canonical tiles (rotation + reflection, D4).")
  end)
end

main()
