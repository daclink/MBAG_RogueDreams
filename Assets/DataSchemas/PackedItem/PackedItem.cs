namespace DataSchemas.PackedItem
{
    using Tables;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Items/Packed Item Asset")]
    public class PackedItemAsset : ScriptableObject
    {
        // editor fields
        [Header("Readable fields")]
        public ItemType itemType;
        public AspectFlags aspectFlags;
        public StatusFlags statusFlags;
        [Range(0, 255)] public byte spriteKey;
        [Range(0, 255)] public byte textKey;

        [Header("Tables (add text & image, then use Add to tables & set keys)")]
        [Tooltip("Assign the text lookup table used by this project.")]
        public TextLookupTable textTable;
        [Tooltip("Assign the sprite lookup table used by this project.")]
        public SpriteLookupTable spriteTable;
        [Tooltip("Type display text here, then click Add to tables & set keys.")]
        public string displayTextToAdd;
        [Tooltip("Assign a sprite here, then click Add to tables & set keys.")]
        public Sprite spriteToAdd;

        [Header("Preview")]
        [Tooltip("Sprite resolved from spriteKey via the sprite table when unpacking.")]
        public Sprite spritePreview;

        // Allows object authorship within the allowable range
        [Header("Stats (-128 to 127)")]
        [Range(-128, 127)] public int health;
        [Range(-128, 127)] public int power;
        [Range(-128, 127)] public int armor;
        [Range(-128, 127)] public int agility;
        [Range(-128, 127)] public int vigor;
        [Range(-128, 127)] public int fortune;
        [Range(-128, 127)] public int range;
        [Range(-128, 127)] public int rarity;

        // Allocation for data to be packed
        [Header("Raw blocks (packed)")]
        public ulong block0;
        public ulong block1;
        public ulong block2;

        /// <summary>Runtime view of the packed data. Use this for inventory, tooltips, or saving.</summary>
        public PackedItemData ToPackedItemData() => new PackedItemData(block0, block1);

        // Encodes values from the inspector into allocated space
        public void PackFromFields()
        {
            // For now, reuse spriteKey for both sprite/icon and textKey for both name/desc
            block0 = PackedItemSchema.PackBlock0(
                itemType,
                aspectFlags,
                statusFlags,
                spriteKey,
                spriteKey,
                textKey,
                textKey);

            // Only flagged stats contribute to block1; unflagged stats are packed as zero
            sbyte h = (aspectFlags & AspectFlags.Health)  != 0 ? (sbyte)health  : (sbyte)0;
            sbyte p = (aspectFlags & AspectFlags.Power)   != 0 ? (sbyte)power   : (sbyte)0;
            sbyte a = (aspectFlags & AspectFlags.Armor)   != 0 ? (sbyte)armor   : (sbyte)0;
            sbyte ag = (aspectFlags & AspectFlags.Agility) != 0 ? (sbyte)agility : (sbyte)0;
            sbyte v  = (aspectFlags & AspectFlags.Vigor)   != 0 ? (sbyte)vigor   : (sbyte)0;
            sbyte f  = (aspectFlags & AspectFlags.Fortune) != 0 ? (sbyte)fortune : (sbyte)0;
            sbyte r  = (aspectFlags & AspectFlags.Range)   != 0 ? (sbyte)range   : (sbyte)0;
            sbyte s  = (aspectFlags & AspectFlags.Rarity)  != 0 ? (sbyte)rarity  : (sbyte)0;

            block1 = PackedItemSchema.PackBlock1(h, p, a, ag, v, f, r, s);
            
        }

        // unpacks encoded data for inspection
        public void UnpackToFields()
        {
            var data = new PackedItemData(block0, block1);

            itemType    = data.ItemType;
            aspectFlags = data.AspectFlags;
            statusFlags = data.StatusFlags;

            spriteKey = data.SpriteKey;
            textKey   = data.TextKey;

            // Update preview sprite based on resolved key
            spritePreview = spriteTable != null ? spriteTable.Get(spriteKey) : null;

            health  = data.Health;
            power   = data.Power;
            armor   = data.Armor;
            agility = data.Agility;
            vigor   = data.Vigor;
            fortune = data.Fortune;
            range   = data.Range;
            rarity  = data.Rarity;
        }

        // Randomizes stats if aspect flags are enabled and packs them
        public void RandomizeStatsAndPack()
        {
            // Unity's Random.Range for ints is min-inclusive, max-exclusive
            if ((aspectFlags & AspectFlags.Health)  != 0) health  = Random.Range(-128, 128);
            if ((aspectFlags & AspectFlags.Power)   != 0) power   = Random.Range(-128, 128);
            if ((aspectFlags & AspectFlags.Armor)   != 0) armor   = Random.Range(-128, 128);
            if ((aspectFlags & AspectFlags.Agility) != 0) agility = Random.Range(-128, 128);
            if ((aspectFlags & AspectFlags.Vigor)   != 0) vigor   = Random.Range(-128, 128);
            if ((aspectFlags & AspectFlags.Fortune) != 0) fortune = Random.Range(-128, 128);
            if ((aspectFlags & AspectFlags.Range)   != 0) range   = Random.Range(-128, 128);
            if ((aspectFlags & AspectFlags.Rarity)  != 0) rarity  = Random.Range(-128, 128);

            PackFromFields();
        }

        /// <summary>Adds displayTextToAdd and spriteToAdd to their tables (or reuses existing keys), sets spriteKey and textKey, then packs. Call from editor.</summary>
        public bool AddDisplayToTablesAndSetKeys()
        {
            bool changed = false;
            if (textTable != null && !string.IsNullOrEmpty(displayTextToAdd))
            {
                int key = textTable.GetKeyOf(displayTextToAdd);
                if (key < 0) key = textTable.Add(displayTextToAdd);
                if (key >= 0 && key <= 255) { textKey = (byte)key; changed = true; }
            }
            if (spriteTable != null && spriteToAdd != null)
            {
                int key = spriteTable.GetKeyOf(spriteToAdd);
                if (key < 0) key = spriteTable.Add(spriteToAdd);
                if (key >= 0 && key <= 255) { spriteKey = (byte)key; changed = true; }
            }
            if (changed) PackFromFields();
            return changed;
        }
    }
}