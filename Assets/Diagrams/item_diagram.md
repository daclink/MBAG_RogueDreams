# PackedItemTable Entity-Relationship Diagram (ERD)

Mermaid natively supports Entity-Relationship Diagrams (ERDs)! These are fantastic for mapping out how different systems and classes communicate with each other and what fields they consist of.

Here is an ERD showing how your `PackedItemTable` manages its sub-systems and data structs:

```mermaid
%%{init: {'theme': 'dark'}}%%
erDiagram
    PackedItemTable ||--o{ PackedItemData : "Stores Array Of"
    PackedItemTable ||--|| PackedItemTableCore : "Delegates Logic To"
    PackedItemTable ||--|| SpriteTable2D : "Syncs Sprites To"
    PackedItemTable ||--o| TextTable2D : "Syncs Text To"
    
    PackedItemTableCore ||--o{ Stack : "Owns 2D Array Of (Free Lists)"

    PackedItemTable {
        PackedItemData[] _items
        PackedItemTableCore _core
        SpriteTable2D _spriteTable
        TextTable2D _textTable
    }

    PackedItemData {
        ulong Block0 "Holds ItemType, BiomeFlags, SpriteKeys"
        ulong Block1 "Holds Health, Power, Armor, Stats"
    }

    PackedItemTableCore {
        Stack[] _freeLists "256x32 Capacity"
    }
```

### Relationship Notation Key:
* `||--||` : One-to-One 
* `||--o|` : One-to-Zero-or-One (Optional)
* `||--o{` : One-to-Zero-or-Many (Array / List / Collections)
