--[[
  Generate Canonical Hex Tiles from 6 Base Tiles (D6: rotation + reflection)
  MBAG RogueDreams Hex Dual-Grid System

  USAGE:
  1. Open an Aseprite file with exactly 6 frames (one per tile type 0..5)
  2. Run: File > Scripts > GenerateHexCanonicalTiles
  3. A new sprite with all canonical tiles will open.

  INPUT: 6 full-size flat-top hex tiles. Frame order: type 0, 1, 2, 3, 4, 5.
  OUTPUT: One tile per D6 orbit (6 rotations × 2 for reflection = 12).

  Corner numbering (flat-top, clockwise from top-left):
       0 -------- 1
      /            \
     5              2
      \            /
       4 -------- 3
--]]

-- ============ Configuration ============
-- Default hex size (flat-top). Script uses the open sprite's dimensions if >= these.
local HEX_WIDTH = 64
local HEX_HEIGHT = 56   -- flat-top: height often sqrt(3)/2 * width; 56 ≈ 64*0.87

-- ============ Key encoding: 6 corners × 6 types (0..5) = base-6 key, 0..46655 ============
local function getCorner(key, i)
  -- i in 0..5; key = c0 + c1*6 + c2*36 + c3*216 + c4*1296 + c5*7776
  for _ = 1, i do key = math.floor(key / 6) end
  return key % 6
end

local function keyFromCorners(c0, c1, c2, c3, c4, c5)
  return c0 + c1*6 + c2*36 + c3*216 + c4*1296 + c5*7776
end

-- Rotate 60° CW: (c0,c1,c2,c3,c4,c5) -> (c5,c0,c1,c2,c3,c4)
local function rotate60(key)
  local c = {}
  for i = 0, 5 do c[i] = getCorner(key, i) end
  return keyFromCorners(c[5], c[0], c[1], c[2], c[3], c[4])
end

-- Reflect (vertical axis): (c0,c1,c2,c3,c4,c5) -> (c1,c0,c5,c4,c3,c2)
local function reflect(key)
  local c = {}
  for i = 0, 5 do c[i] = getCorner(key, i) end
  return keyFromCorners(c[1], c[0], c[5], c[4], c[3], c[2])
end

local function orbit12(key)
  local seen = {}
  local x = key
  for _ = 1, 6 do
    seen[x] = true
    x = rotate60(x)
  end
  x = reflect(key)
  for _ = 1, 6 do
    seen[x] = true
    x = rotate60(x)
  end
  return seen
end

local function isCanonical(key)
  local orb = orbit12(key)
  local min = key
  for k, _ in pairs(orb) do
    if k < min then min = k end
  end
  return key == min
end

local function distinctCornerTypes(key)
  local seen = {}
  for i = 0, 5 do seen[getCorner(key, i)] = true end
  local n = 0
  for _ in pairs(seen) do n = n + 1 end
  return n
end

-- Build list of canonical keys (0 .. 6^6-1)
local canonicalKeys = {}
for key = 0, 46655 do
  if isCanonical(key) then
    canonicalKeys[#canonicalKeys + 1] = key
  end
end

table.sort(canonicalKeys, function(a, b)
  local da, db = distinctCornerTypes(a), distinctCornerTypes(b)
  if da ~= db then return da < db end
  return a < b
end)

local CANONICAL_COUNT = #canonicalKeys

-- ============ Flat-top hex geometry (y down) ============
-- Corners: 0 top-left, 1 top-right, 2 right, 3 bottom-right, 4 bottom-left, 5 left
local function hexCorners(w, h)
  local cx, cy = w / 2, h / 2
  return {
    { x = w/4,   y = 0 },    -- 0 top-left
    { x = 3*w/4, y = 0 },    -- 1 top-right
    { x = w,     y = h/2 },   -- 2 right
    { x = 3*w/4, y = h },     -- 3 bottom-right
    { x = w/4,   y = h },     -- 4 bottom-left
    { x = 0,     y = h/2 },   -- 5 left
  }, cx, cy
end

-- Point in triangle (same side of each edge as the opposite vertex)
local function pointInTriangle(px, py, ax, ay, bx, by, cx, cy)
  local function sign(ox, oy, px, py, qx, qy)
    return (px - ox) * (qy - oy) - (py - oy) * (qx - ox)
  end
  local s1 = sign(ax, ay, bx, by, px, py)
  local s2 = sign(ax, ay, bx, by, cx, cy)
  if s1 * s2 < 0 then return false end
  s1 = sign(bx, by, cx, cy, px, py)
  s2 = sign(bx, by, cx, cy, ax, ay)
  if s1 * s2 < 0 then return false end
  s1 = sign(cx, cy, ax, ay, px, py)
  s2 = sign(cx, cy, ax, ay, bx, by)
  if s1 * s2 < 0 then return false end
  return true
end

-- Which wedge (0..5) contains (px, py)? Wedge i = triangle (center, corner_i, corner_{i+1})
local function getWedge(px, py, corners, cx, cy)
  for i = 0, 5 do
    local j = (i + 1) % 6
    local a, b, c = corners[i + 1], corners[j + 1], { x = cx, y = cy }
    if pointInTriangle(px, py, cx, cy, a.x, a.y, b.x, b.y) then
      return i
    end
  end
  return nil
end

local function buildHexTile(sources, key, w, h, colorMode)
  local corners, cx, cy = hexCorners(w, h)
  -- Clone first source for size and color mode, then clear
  local out = Image(sources[1])
  out:clear()

  for pixel in out:pixels() do
    local x, y = pixel.x, pixel.y
    local wedge = getWedge(x, y, corners, cx, cy)
    if wedge then
      local typeIdx = getCorner(key, wedge)
      local src = sources[typeIdx + 1]
      if src then
        pixel(src:getPixel(x, y))
      end
    end
  end
  return out
end

-- ============ Main ============
local function main()
  local src = app.sprite
  if not src then
    app.alert("No sprite open. Open a 6-frame sprite (one frame per tile type 0..5) first.")
    return
  end

  if #src.frames < 6 then
    app.alert("Sprite must have at least 6 frames (one per tile type 0..5).")
    return
  end

  local w, h = src.width, src.height
  if w < HEX_WIDTH or h < HEX_HEIGHT then
    app.alert("Each frame should be at least " .. HEX_WIDTH .. "x" .. HEX_HEIGHT .. " for a clear hex.")
  end

  app.transaction("Generate Hex Canonical Tiles", function()
    local layer = src.layers[1]
    if not layer then
      app.alert("Sprite has no layers.")
      return
    end

    local sources = {}
    for i = 1, 6 do
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
    local outSprite = Sprite(w, h, colorMode)
    local base = (src.filename and src.filename ~= "") and src.filename:gsub("%.aseprite$", ""):gsub("%.ase$", "") or "hex_canonical_tiles"
    outSprite.filename = base .. "_hex_canonical.aseprite"

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

    local img0 = buildHexTile(sources, canonicalKeys[1], w, h, colorMode)
    if not img0 then return end

    local cel0 = getCel(outSprite, outLayer, 1)
    if cel0 then cel0.image = img0 end

    for i = 2, CANONICAL_COUNT do
      outSprite:newEmptyFrame(i)
      local img = buildHexTile(sources, canonicalKeys[i], w, h, colorMode)
      if not img then return end
      outSprite:newCel(outLayer, outSprite.frames[i], img, Point(0, 0))
    end

    app.sprite = outSprite
    app.alert("Created " .. tostring(CANONICAL_COUNT) .. " canonical hex tiles (D6).")
  end)
end

main()
