using System;
namespace DataSchemas.PackedItem
{
    /// <summary> 
    /// ItemType is an enum with a set of named constants for each type of object.
    /// Labeled with a Flags attribute so that it is treated as a bitfield.
    /// The stored value is 8-bits long (byte), 4 of which are in use.
    /// Each value is one bit; combine with bitwise OR and test with bitwise AND.
    /// Stored in bits 0-7 of word0.
    /// </summary>
    [Flags]
    public enum ItemType : byte
    {
        None       = 0,
        Weapon     = 1 << 0,
        Armor      = 1 << 1,
        Consumable = 1 << 2,
        KeyItem    = 1 << 3
    }
    /// <summary> 
    /// StatFlags is an enum with a set of named constants for filtering stat bonuses.
    /// It is labeled with a Flags attribute so that it is treated as a bitfield.
    /// The stored value is 8-bits long (byte), all of which are in use.
    /// Each value is one bit; combine with bitwise OR and test with bitwise AND.
    /// Stored in bits 8-15 of word0.
    /// </summary>
    [Flags]
    public enum StatFlags : byte
    {
        None    = 0,
        Health  = 1 << 0,
        Power   = 1 << 1,
        Armor   = 1 << 2,
        Agility = 1 << 3,
        Vigor   = 1 << 4,
        Fortune = 1 << 5,
        Range   = 1 << 6,
        Status  = 1 << 7
    }

    /// <summary>
    /// PackedItemSchema is a static helper that defines the layout of three packed words.
    /// The class provides methods for packing and unpacking these words.
    /// Word0 contains metadata for the packed item.
    /// Word1 and word2 contain numerical data for the item's stats.
    /// </summary>
    public static class PackedItemSchema
    {
        // Constant shift amounts to be reused across functions to land at the correct right bit
        private const int SecondByte = 8;
        private const int ThirdByte  = 16;
        private const int FourthByte = 24;

        // Low 8-bits set. To be used with bitwise AND
        private const uint ByteMask = 0xFFu;

        /* Word 0 layout (32-bits):
        *
        * bits 0-7 (byte)   : Type 
        * bits 8-15 (byte)  : Stats 
        * bits 16-23 (byte) : SpriteKey 
        * bits 24-31 (byte) : TextKey
        *
        *  31              24 23             16 15             8 7               0
        *  *--------+--------+-----------------+----------------+----------------+
        *  |     TextKey     |    SpriteKey    |    StatFlags   |    ItemType    |
        *  *--------+--------+--------+--------+----------------+----------------+
        */

        /// <summary>
        /// PackWord0 takes four bytes (type, statFlags, spriteKey, textKey) and packs them into an uint.
        /// </summary>
        public static uint PackWord0(ItemType type, StatFlags statFlags, byte spriteKey, byte textKey)
        {
            return (uint)type
                | ((uint)statFlags << SecondByte)
                | ((uint)spriteKey << ThirdByte)
                | ((uint)textKey   << FourthByte);
        }

        /*
        * The following methods each shift their target byte into the low eight bits.
        * Then they use a mask to return only the information in those bits.
        * Casts are performed as needed.
        */
        public static ItemType GetItemType(uint word0)   => (ItemType)(word0 & ByteMask);
        public static StatFlags GetStatFlags(uint word0) => (StatFlags)((word0 >> SecondByte) & ByteMask);
        public static byte GetSpriteKey(uint word0)      => (byte)((word0 >> ThirdByte) & ByteMask);
        public static byte GetTextKey(uint word0)        => (byte)((word0 >> FourthByte) & ByteMask);

        /* Word 1 layout:
        *
        * bits 0-7 (byte)   : Health 
        * bits 8-15 (byte)  : Power 
        * bits 16-23 (byte) : Armor 
        * bits 24-31 (byte) : Agility
        *
        *  31              24 23             16 15             8 7               0
        *  *--------+--------+-----------------+----------------+----------------+
        *  |     Agility     |      Armor      |      Power     |     Health     |
        *  *--------+--------+--------+--------+----------------+----------------+
        */

        /// <summary>
        /// PackWord1 takes the numerical values of the first four StatFlags and packs them into the uint word1. 
        /// </summary>
        public static uint PackWord1(byte health, byte power, byte armor, byte agility)
        {
            return health
                | ((uint)power   << SecondByte)
                | ((uint)armor   << ThirdByte)
                | ((uint)agility << FourthByte);
        }

        // word1-specific getters

        public static byte GetHealth(uint word1)  => (byte)(word1 & ByteMask);
        public static byte GetPower(uint word1)   => (byte)((word1 >> SecondByte) & ByteMask);
        public static byte GetArmor(uint word1)   => (byte)((word1 >> ThirdByte) & ByteMask);
        public static byte GetAgility(uint word1) => (byte)((word1 >> FourthByte) & ByteMask);

        /* Word 2 layout:
        *
        * bits 0-7 (byte)   : Vigor 
        * bits 8-15 (byte)  : Fortune 
        * bits 16-23 (byte) : Range 
        * bits 24-31 (byte) : Status
        *
        *  31              24 23             16 15             8 7               0
        *  *--------+--------+-----------------+----------------+----------------+
        *  |      Status     |      Range      |     Fortune    |      Vigor     |
        *  *--------+--------+--------+--------+----------------+----------------+
        */

        /// <summary>
        /// PackWord2 takes the numerical values of the last four StatFlags and packs them into the uint word2. 
        /// </summary>
        public static uint PackWord2(byte vigor, byte fortune, byte range, byte status)
        {
            return vigor
                | ((uint)fortune << SecondByte)
                | ((uint)range   << ThirdByte)
                | ((uint)status  << FourthByte);
        }

        // word2-specific getters
        public static byte GetVigor(uint word2)   => (byte)(word2 & ByteMask);
        public static byte GetFortune(uint word2) => (byte)((word2 >> SecondByte) & ByteMask);
        public static byte GetRange(uint word2)   => (byte)((word2 >> ThirdByte) & ByteMask);
        public static byte GetStatus(uint word2)  => (byte)((word2 >> FourthByte) & ByteMask);
    }

    /// <summary>
    /// PackedItemData is a readonly struct that holds three words and exposes as decoded fields, providing a runtime view.
    /// </summary>
    public readonly struct PackedItemData
    {
        // words as they were stored in asset form or save data
        private readonly uint word0;
        private readonly uint word1;
        private readonly uint word2;
        private readonly StatFlags statFlags;

        // Constructor that takes three words and assigns them to the readonly fields as well as the flag cache
        // Only the constructor can set a field. Everything else is read-only
        public PackedItemData(uint packed0, uint packed1, uint packed2)
        {
            word0 = packed0;
            word1 = packed1;
            word2 = packed2;
            statFlags = PackedItemSchema.GetStatFlags(word0);
        }

        // Properties derived from word0. StatFlags uses the cached version from the constructor all others compute when called.
        public ItemType ItemType   => PackedItemSchema.GetItemType(word0);
        public StatFlags StatFlags => statFlags;
        public byte SpriteKey      => PackedItemSchema.GetSpriteKey(word0);
        public byte TextKey        => PackedItemSchema.GetTextKey(word0);

        // Properties derived from word1 and word2. Values for unset flags default to zero
        public byte Health  => (statFlags & StatFlags.Health)  != 0 ? PackedItemSchema.GetHealth(word1)  : (byte)0;
        public byte Power   => (statFlags & StatFlags.Power)   != 0 ? PackedItemSchema.GetPower(word1)   : (byte)0;
        public byte Armor   => (statFlags & StatFlags.Armor)   != 0 ? PackedItemSchema.GetArmor(word1)   : (byte)0;
        public byte Agility => (statFlags & StatFlags.Agility) != 0 ? PackedItemSchema.GetAgility(word1) : (byte)0;
        public byte Vigor   => (statFlags & StatFlags.Vigor)   != 0 ? PackedItemSchema.GetVigor(word2)   : (byte)0;
        public byte Fortune => (statFlags & StatFlags.Fortune) != 0 ? PackedItemSchema.GetFortune(word2) : (byte)0;
        public byte Range   => (statFlags & StatFlags.Range)   != 0 ? PackedItemSchema.GetRange(word2)   : (byte)0;
        public byte Status  => (statFlags & StatFlags.Status)  != 0 ? PackedItemSchema.GetStatus(word2)  : (byte)0;
        
    }
}