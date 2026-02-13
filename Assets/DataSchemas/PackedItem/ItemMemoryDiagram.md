This document describes how a single item is encoded into two 64‑bit blocks.

- **Metadata block (Block0)**: type, which aspects/stats are present, status flags, and lookup keys into external tables (sprite, icon, name, description).
- **Stats block (Block1)**: up to eight signed stat values, each stored in one byte (Health, Power, Armor, etc.).

###  Metadata Block (64 bits)

| **Bits** | **Byte index** | **Field** |
|---------:|---------------:|----------|
| 63–56    | 7              | Desc     |
| 55–48    | 6              | Name     |
| 47–40    | 5              | Icon     |
| 39–32    | 4              | Sprite   |
| 31–24    | 3              |          |
| 23–16    | 2              | Status   |
| 15–8     | 1              | Aspect   |
| 7–0      | 0              | Type     |

```text
Bits:
    63       56 55      48 47      40 39      32 31      24 23      16 15       8 7        0
    +__________+__________+__________+__________+__________+__________+__________+_________+
    |   Desc   |  Name    |  Icon    |  Sprite  |       Status        |  Aspect  |  Type   |
    |__________+__________+__________+__________+__________+__________+__________+_________|
```
Each field in this block is an 8‑ or 16‑bit identifier:

- **Type**: which kind of item this is (`ItemType` enum).
- **Aspect**: which numeric stats are actually used for this item (`AspectFlags` bitfield).
- **Status**: which status effects this item can apply or confer (`StatusFlags` bitfield, 16 bits total).
- **Sprite/Icon/Name/Desc**: byte keys into separate lookup tables (e.g., for UI sprites and localized text).


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
