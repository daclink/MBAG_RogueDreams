# Room Tree demo scene — current state

This document describes the **intended layout and behavior** of the **Room Tree demo** you can build from the editor, and the runtime systems that scene is meant to exercise.

**Menu path:** `Tools` → `WFC` → **Create Room Tree Demo Scene**  
Use **File → Save As** to persist (for example under `Assets/Scenes/Room_Tree/`).

---

## What this scene is

A self-contained 2D demo built around a **`RoomTreeDungeon`** root:

- Procedural **4×4 room-tree** dungeons
- **Neighborhood-based streaming** (default **3 hops** from the player’s room for loaded cells)
- **Packed-item pickups** in rooms
- **Per-room enemy spawning** with **room-local pathfinding** toward the player, and **random idle wander** in streamed rooms that are not the player’s current room
- A **Player** with **inventory**, **equipment**, **hotbar**, **equipped-item visuals**, and **thrown weapons** using `ThrownProjectile2D`

Hand-authored scenes that match this setup need the same components and references (see [`Assets/Scripts/README.md`](../Assets/Scripts/README.md) and [`RoomTreeDungeon_Spec.md`](../Assets/Scripts/RoomTree/RoomTreeDungeon_Spec.md)).

---

## Scene graph (as created by the menu)

| Object / area | Role |
|---------------|------|
| **RoomTreeDungeon** | `RoomTreeDungeonComponent` with WFC tile assets assigned; child **DualGridTilemap**; `BiomeTileRegistry` wired; **`streamedNeighborHops` = 3** (typical). |
| **RoomTreeEnemyPathfindingSystem** (on root) | References dungeon + **Melee_Enemy** prefab; **2 enemies per room** (when spawned); `BaseEnemy` disabled at runtime, **`NPC`** pathfinding; optional move-speed override. |
| **ItemTableBootstrap** | `SpriteTable2D` + `TextTable2D`; data under `Assets/GameData/Items/` as configured. |
| **RoomTreePackedItemSpawner** | 0–1 packed pickup per room (defaults from tool) using the **PackedItemPickup** prefab. |
| **Player** | **Player_body** at dungeon center if the asset path resolves; `PlayerInventory`, `PlayerEquipment`, `EquippedItemVisual`, `WeaponThrowController`; **Inventory** / **Hotbar** UI wired to the player. |
| **EventSystem** | Created if missing, with **InputSystemUIInputModule** for UI. |
| **Main camera** | Positioned for the 4×4 play area; **URP 2D Pixel Perfect** (e.g. 64 PPU) when the setup helper runs. |

---

## Runtime notes (typical)

- Thrown weapons: motion uses **`Rigidbody2D.MovePosition` in `FixedUpdate`** for reliable 2D triggers; the equipped weapon is not consumed on a single throw.
- Enemies: `BaseEnemy` may be disabled; **lethal** damage still runs **`HandleDead`** from **`TakeDamage`** so destruction works when `Update` does not drive state.

---

## How to verify (manual)

1. Run **Tools → WFC → Create Room Tree Demo Scene** (or open a saved room-tree scene).
2. Enter **Play**; confirm generation, camera follow, and zoom if supported.
3. **Tab** (inventory) / hotbar; pick up a **weapon**; **LMB** to throw; confirm enemies can take damage and despawn at 0 health.
4. Move between rooms; confirm streaming, chase in the current room, and wander in other streamed rooms.

For pathfinding and walk-mask details, see `RoomLocalPathfinding_Plan.md` under **Pathfinding_Scripts** and the **RoomTree** spec.

---

## PR / review

When opening a pull request, use [`.github/pull_request_template.md`](../.github/pull_request_template.md) and point to this file if the change affects the room-tree demo only.

Suggested PR title example: `docs: room tree demo — scene behavior`
