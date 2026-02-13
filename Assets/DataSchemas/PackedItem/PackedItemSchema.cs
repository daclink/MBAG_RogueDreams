using System;
namespace DataSchemas.PackedItem
{

    [Flags]
    /// <summary> 
    /// ItemType is an enum with a set of named constants for each type of object.
    /// Labeled with a Flags attribute so that it is treated as a bitfield.
    /// The stored value is 8-bits long (byte), 4 of which are in use.
    /// Each value is one bit; combine with bitwise OR and test with bitwise AND.
    /// Stored in bits 0-7 of block0.
    /// </summary>
    public enum ItemType : byte
    {
        None       = 0,
        Weapon     = 1 << 0,
        Armor      = 1 << 1,
        Consumable = 1 << 2,
        KeyItem    = 1 << 3
    }
    
    [Flags]
    /// <summary> 
    /// AspectFlags is an enum with a set of named constants for filtering stat bonuses.
    /// It is labeled with a Flags attribute so that it is treated as a bitfield.
    /// The stored value is 8-bits long (byte), 7 of which are in use.
    /// Each value is one bit; combine with bitwise OR and test with bitwise AND.
    /// Stored in bits 8-15 of block0.
    /// </summary>
    public enum AspectFlags : byte
    {
        None    = 0,
        Health  = 1 << 0,
        Power   = 1 << 1,
        Armor   = 1 << 2,
        Agility = 1 << 3,
        Vigor   = 1 << 4,
        Fortune = 1 << 5,
        Range   = 1 << 6,
        Rarity  = 1 << 7
    }

    [Flags]
    /// <summary> 
    /// StatusFlags is an enum with a set of named constants for filtering status conditions.
    /// It is labeled with a Flags attribute so that it is treated as a bitfield.
    /// The stored value is 16-bits long (short), 8 of which are in use.
    /// Each value is one bit; combine with bitwise OR and test with bitwise AND.
    /// Stored in bits 16-31 of block0.
    /// </summary>
    public enum StatusFlags : ushort
    {
        None       = 0,
        Burn       = 1 << 0,
        Poison     = 1 << 1,
        Paralysis  = 1 << 2,
        Slow       = 1 << 3,
        Chilled    = 1 << 4,
        Haste      = 1 << 5,
        Resistance = 1 << 6,
        Invisible  = 1 << 7
    }

    // PackedItemSchema is a static helper theat defines the layout of three packed blocks.
    // The class provides method for packing and unpacking these blocks.
    // block0 contains metadata for the packed item.
    // block1 and block2 contain numerical data fornthe item's stats.
    public static class PackedItemSchema
    {
        // Constant shift amounts to be reused across functions to land at the correct right bit
        private const int SecondByte  = 8;
        private const int ThirdByte   = 16;
        private const int FourthByte  = 24;
        private const int FifthByte   = 32;
        private const int SixthByte   = 40;
        private const int SeventhByte = 48;
        private const int EighthByte  = 56;

        // Low 8 bits set. To be used with bitwise AND
        private const uint ByteMask = 0xFFu;
        private const uint ShortMask = 0xFFFFu;

        /// <summary>
        /// Packblock0 takes item metadata (type, aspectFlags, statusFlags, spriteKey, iconKey, textkey, descKey) and packs them into a ulong.
        /// </summary>
        public static ulong PackBlock0(ItemType type, AspectFlags aspectFlags, StatusFlags statusFlags, byte spriteKey, byte iconKey, byte nameKey, byte descKey)
        {
            return (ulong)type
                | ((ulong)aspectFlags << SecondByte)
                | ((ulong)statusFlags << ThirdByte)
                | ((ulong)spriteKey   << FifthByte)
                | ((ulong)iconKey     << SixthByte)
                | ((ulong)nameKey     << SeventhByte)
                | ((ulong)descKey     << EighthByte);
        }

        /*
        * The following methods each shift their target byte into the low eight bits.
        * Status flags use 16-bits instead and are dealt with accordingly.
        * Then they use a mask to return only the information in those bits.
        * Casts are performed as needed.
        */
        public static ItemType GetItemType(ulong block0)       => (ItemType)(block0 & ByteMask);
        public static AspectFlags GetAspectFlags(ulong block0) => (AspectFlags)((block0 >> SecondByte) & ByteMask);
        public static StatusFlags GetStatusFlags(ulong block0) => (StatusFlags)((block0 >> ThirdByte) & ShortMask);
        public static byte GetSpriteKey(ulong block0)          => (byte)((block0 >> FifthByte) & ByteMask);
        public static byte GetIconKey(ulong block0)            => (byte)((block0 >> SixthByte) & ByteMask);
        public static byte GetTextKey(ulong block0)            => (byte)((block0 >> SeventhByte) & ByteMask);
        public static byte GetDescKey(ulong block0)            => (byte)((block0 >> EighthByte) & ByteMask);

        // Block 1
        // PackBlock1 takes the numerical values of the aspectFlags and packs them into the ulong block1. 
        // Signed bytes are passed to the function, then cast as bytes and then ulong so as to avoid sign extension.
        // 8-bits remain unclaimed
        public static ulong PackBlock1(sbyte health, sbyte power, sbyte armor, sbyte agility, sbyte vigor, sbyte fortune, sbyte range, sbyte rarity)
        {
            return (ulong)(byte)health
                | ((ulong)(byte)power   << SecondByte)
                | ((ulong)(byte)armor   << ThirdByte)
                | ((ulong)(byte)agility << FourthByte)
                | ((ulong)(byte)vigor   << FifthByte)
                | ((ulong)(byte)fortune << SixthByte)
                | ((ulong)(byte)range   << SeventhByte)
                | ((ulong)(byte)rarity  << EighthByte);
        }

        // block1-specific getters
        public static sbyte GetHealth(ulong block1)  => (sbyte)(block1 & ByteMask);
        public static sbyte GetPower(ulong block1)   => (sbyte)((block1 >> SecondByte)  & ByteMask);
        public static sbyte GetArmor(ulong block1)   => (sbyte)((block1 >> ThirdByte)   & ByteMask);
        public static sbyte GetAgility(ulong block1) => (sbyte)((block1 >> FourthByte)  & ByteMask);
        public static sbyte GetVigor(ulong block1)   => (sbyte)((block1 >> FifthByte)   & ByteMask);
        public static sbyte GetFortune(ulong block1) => (sbyte)((block1 >> SixthByte)   & ByteMask);
        public static sbyte GetRange(ulong block1)   => (sbyte)((block1 >> SeventhByte) & ByteMask);
        public static sbyte GetRarity(ulong block1)   => (sbyte)((block1 >> EighthByte) & ByteMask);
    }

    /// <summary>
    /// PackedItemData is a readonly struct that holds three blocks and exposes as decoded fields, providing a runtime view.
    /// </summary>
    public readonly struct PackedItemData
    {
        // Blocks as they were stored in asset form or save data
        public ulong Block0 { get; }
        public ulong Block1 { get; }
        private readonly AspectFlags _aspectFlags;
        private readonly StatusFlags _statusFlags;

        // Constructor that takes three blocks and assigns them to the readonly fields as well as the flag cache
        // Only the constructor can set a field. Everything else is read only
        public PackedItemData(ulong block0, ulong block1)
        {
            Block0 = block0;
            Block1 = block1;
            _aspectFlags = PackedItemSchema.GetAspectFlags(block0);
            _statusFlags = PackedItemSchema.GetStatusFlags(block0);
        }

        // Properties derived from block0. aspectFlags uses cached version from constructor all others compute when called.
        public ItemType ItemType       => PackedItemSchema.GetItemType(Block0);
        public AspectFlags AspectFlags => _aspectFlags;
        public StatusFlags StatusFlags => _statusFlags;
        public byte SpriteKey          => PackedItemSchema.GetSpriteKey(Block0);
        public byte IconKey            => PackedItemSchema.GetIconKey(Block0);
        public byte TextKey            => PackedItemSchema.GetTextKey(Block0);
        public byte DescKey            => PackedItemSchema.GetDescKey(Block0);

        // Properties derived from block1. Values for unset flags default to zero
        public sbyte Health  => (_aspectFlags & AspectFlags.Health)  != 0 ? PackedItemSchema.GetHealth(Block1)  : (sbyte)0;
        public sbyte Power   => (_aspectFlags & AspectFlags.Power)   != 0 ? PackedItemSchema.GetPower(Block1)   : (sbyte)0;
        public sbyte Armor   => (_aspectFlags & AspectFlags.Armor)   != 0 ? PackedItemSchema.GetArmor(Block1)   : (sbyte)0;
        public sbyte Agility => (_aspectFlags & AspectFlags.Agility) != 0 ? PackedItemSchema.GetAgility(Block1) : (sbyte)0;
        public sbyte Vigor   => (_aspectFlags & AspectFlags.Vigor)   != 0 ? PackedItemSchema.GetVigor(Block1)   : (sbyte)0;
        public sbyte Fortune => (_aspectFlags & AspectFlags.Fortune) != 0 ? PackedItemSchema.GetFortune(Block1) : (sbyte)0;
        public sbyte Range   => (_aspectFlags & AspectFlags.Range)   != 0 ? PackedItemSchema.GetRange(Block1)   : (sbyte)0;
        public sbyte Rarity  => (_aspectFlags & AspectFlags.Rarity)  != 0 ? PackedItemSchema.GetRarity(Block1)  : (sbyte)0;
        
    }
}