This document describes how a single item is encoded into two 64‑bit blocks.

- **Metadata block (Block0)**: type, aspect/status flags, 8-bit lookup keys (SpriteKey, TextKey) for 2D tables, plus RarityFlags and BiomeFlags.
- **Stats block (Block1)**: up to eight signed stat values, each stored in one byte (Health, Power, Armor, etc.).

###  Metadata Block (64 bits)

| **Bits** | **Byte index** | **Field**     |
|---------:|---------------:|---------------|
| 63–56    | 7              | TextKey       |
| 55–48    | 6              | SpriteKey     |
| 47–40    | 5              | RarityFlags   |
| 39–32    | 4              | BiomeFlags    |
| 31–16    | 2–3            | StatusFlags   |
| 15–8     | 1              | AspectFlags   |
| 7–0      | 0              | ItemType      |

```text
Bits:
    63       56 55      48 47      40 39      32 31                 16 15       8 7        0
    +__________+__________+__________+__________+_____________________+__________+_________+
    | TextKey  |SpriteKey |  Rarity  |BiomeFlags|   StatusFlags (16)  |  Aspect  |  Type   |
    |__________+__________+__________+__________+_____________________+__________+_________|
```
Each field in this block:

- **ItemType** (8b): kind of item (`ItemType` enum, Flags).
- **AspectFlags** (8b): which numeric stats are present (`AspectFlags` bitfield).
- **StatusFlags** (16b): status effects this item can apply (`StatusFlags` bitfield).
- **BiomeFlags** (8b): biomes where this item can appear (`BiomeFlags` bitfield).
- **RarityFlags** (8b): rarity tiers or filtering (`RarityFlags` bitfield).
- **SpriteKey** (8b): key into sprite table [type][spriteKey]; icon = spriteKey + stride.
- **TextKey** (8b): key into text table [type][textKey]; desc = textKey + stride.


### Item Stats Block (64 bits)

| **Bits** | **Byte index** | **Field** |
|---------:|---------------:|-----------|
| 63–56    | 7              | Rarity    |
| 55–48    | 6              | Range     |
| 47–40    | 5              | Fortune   |
| 39–32    | 4              | Vigor     |
| 31–24    | 3              | Agility   |
| 23–16    | 2              | Armor     |
| 15–8     | 1              | Power     |
| 7–0      | 0              | Health    |

```text
Bits:
    63       56 55      48 47      40 39      32 31      24 23      16 15       8 7        0
    +__________+__________+__________+__________+__________+__________+__________+_________+
    |  Rarity  |  Range   |  Fortune |  Vigor   |  Agility  |  Armor  |   Power  |  Health |
    |__________+__________+__________+__________+__________+__________+__________+_________|
```
Each byte in this block is a signed stat (`sbyte`, -128..127):

- If an aspect flag is **set** for a stat (e.g. `AspectFlags.Power`), the corresponding byte in this block is meaningful.
- If an aspect flag is **not set**, that byte is ignored and treated as 0 at runtime.
- This keeps all numeric stats tightly packed in one 64‑bit value while still letting you enable/disable them via `AspectFlags`.
