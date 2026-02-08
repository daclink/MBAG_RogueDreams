using System;
namespace DataSchemas.PackedItem
{
// has 8 bits of space, uses 4
[Flags]
public enum PackedItemType : byte
{
    None = 0,
    Weapon = 1 << 0,
    Armor = 1 << 1,
    Consumable = 1 << 2,
    KeyItem = 1 << 3
}
// has 16 bits of space, uses 8
[Flags]
public enum PackedStatFlags : ushort
{
    None = 0,
    Health = 1 << 0,
    Power = 1 << 1,
    Armor = 1 << 2,
    Agility = 1 << 3,
    Vigor = 1 << 4,
    Fortune = 1 << 5,
    Range = 1 << 6,
    Status = 1 << 7
}
// redundant to be used for a separate purpose, uses all 8 bits of space
[Flags]
public enum PackedRuntimeFlags : byte
{
    None = 0,
    Health = 1 << 0,
    Power = 1 << 1,
    Armor = 1 << 2,
    Agility = 1 << 3,
    Vigor = 1 << 4,
    Fortune = 1 << 5,
    Range = 1 << 6,
    Status = 1 << 7
}

public static class PackedItemSchema
{
    // To replace magic numbers
    private const int ItemTypeShift = 0;
    private const int StatFlagsShift = 8;
    private const int RuntimeFlagsShift = 24;

    private const uint ByteMask = 0xFFu;
    private const uint UShortMask = 0xFFFFu;

    // Word 0 layout:
    // bits 0-7   : PackedItemType (byte)
    // bits 8-23  : PackedStatFlags (ushort)
    // bits 24-31 : PackedRuntimeFlags (byte)
    
    /*
     * 31           24 23            8 7             0
    +--------------+----------------+---------------+
    | RuntimeFlags |   StatFlags    |   ItemType    |
    +--------------+----------------+---------------+

     */
    public static uint PackWord0(PackedItemType type, PackedStatFlags statFlags, PackedRuntimeFlags runtimeFlags)
    {
        return (uint)type
               | ((uint)statFlags << 8)
               | ((uint)runtimeFlags << 24);
    }
    // bitwise & to get first 8 bits
    // 0xFFu == 255 == 00000000 00000000 00000000 11111111
    public static PackedItemType GetItemType(uint word0)
    {
        return (PackedItemType)(word0 & 0xFFu);
    }
    
    // bitwise & to get middle 16 bits
    // 0xFFFFu == 65,535 == 00000000 00000000 11111111 11111111
    public static PackedStatFlags GetStatFlags(uint word0)
    {
        return (PackedStatFlags)((word0 >> 8) & 0xFFFFu);
    }

    public static PackedRuntimeFlags GetRuntimeFlags(uint word0)
    {
        return (PackedRuntimeFlags)((word0 >> 24) & 0xFFu);
    }

    // Word 1 layout:
    // bits 0-7   : Health (byte)
    // bits 8-15  : Power (byte)
    // bits 16-23 : Armor (byte)
    // bits 24-31 : Agility (byte)
    public static uint PackWord1(byte health, byte power, byte armor, byte agility)
    {
        return (uint)health
               | ((uint)power << 8)
               | ((uint)armor << 16)
               | ((uint)agility << 24);
    }

    public static byte GetHealth(uint word1) => (byte)(word1 & 0xFFu);
    public static byte GetPower(uint word1) => (byte)((word1 >> 8) & 0xFFu);
    public static byte GetArmor(uint word1) => (byte)((word1 >> 16) & 0xFFu);
    public static byte GetAgility(uint word1) => (byte)((word1 >> 24) & 0xFFu);

    // Word 2 layout:
    // bits 0-7   : Vigor (byte)
    // bits 8-15  : Fortune (byte)
    // bits 16-23 : Range (byte)
    // bits 24-31 : Status (byte)
    public static uint PackWord2(byte vigor, byte fortune, byte range, byte status)
    {
        return (uint)vigor
               | ((uint)fortune << 8)
               | ((uint)range << 16)
               | ((uint)status << 24);
    }

    public static byte GetVigor(uint word2) => (byte)(word2 & 0xFFu);
    public static byte GetFortune(uint word2) => (byte)((word2 >> 8) & 0xFFu);
    public static byte GetRange(uint word2) => (byte)((word2 >> 16) & 0xFFu);
    public static byte GetStatus(uint word2) => (byte)((word2 >> 24) & 0xFFu);
}

public readonly struct PackedItemData
{
    public readonly uint Word0;
    public readonly uint Word1;
    public readonly uint Word2;

    public PackedItemData(uint word0, uint word1, uint word2)
    {
        Word0 = word0;
        Word1 = word1;
        Word2 = word2;
    }

    public PackedItemType ItemType => PackedItemSchema.GetItemType(Word0);
    public PackedRuntimeFlags RuntimeFlags => PackedItemSchema.GetRuntimeFlags(Word0);
    public PackedStatFlags StatFlags => PackedItemSchema.GetStatFlags(Word0);

    public byte Health => (StatFlags & PackedStatFlags.Health) != 0 ? PackedItemSchema.GetHealth(Word1) : (byte)0;
    public byte Power => (StatFlags & PackedStatFlags.Power) != 0 ? PackedItemSchema.GetPower(Word1) : (byte)0;
    public byte Armor => (StatFlags & PackedStatFlags.Armor) != 0 ? PackedItemSchema.GetArmor(Word1) : (byte)0;
    public byte Agility => (StatFlags & PackedStatFlags.Agility) != 0 ? PackedItemSchema.GetAgility(Word1) : (byte)0;
    public byte Vigor => (StatFlags & PackedStatFlags.Vigor) != 0 ? PackedItemSchema.GetVigor(Word2) : (byte)0;
    public byte Fortune => (StatFlags & PackedStatFlags.Fortune) != 0 ? PackedItemSchema.GetFortune(Word2) : (byte)0;
    public byte Range => (StatFlags & PackedStatFlags.Range) != 0 ? PackedItemSchema.GetRange(Word2) : (byte)0;
    public byte Status => (StatFlags & PackedStatFlags.Status) != 0 ? PackedItemSchema.GetStatus(Word2) : (byte)0;
    
}
}