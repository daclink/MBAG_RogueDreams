namespace DataSchemas.PackedItem
{
    using UnityEngine;
    using Tables;

    [CreateAssetMenu(menuName = "Items/Packed Item Asset")]
    public class PackedItemAsset : ScriptableObject
    {
        // editor fields
        [Header("Readable fields")]
        public ItemType itemType;
        public StatFlags statFlags;
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
        
        // Allows object authorship within the allowable range
        [Header("Stats (0-255)")]
        [Range(0, 255)] public int health;
        [Range(0, 255)] public int power;
        [Range(0, 255)] public int armor;
        [Range(0, 255)] public int agility;
        [Range(0, 255)] public int vigor;
        [Range(0, 255)] public int fortune;
        [Range(0, 255)] public int range;
        [Range(0, 255)] public int status;

        // Allocation for data to be packed
        [Header("Raw words (packed)")]
        public uint word0;
        public uint word1;
        public uint word2;

        /// <summary>Runtime view of the packed data. Use this for inventory, tooltips, or saving.</summary>
        public PackedItemData ToPackedItemData() => new PackedItemData(word0, word1, word2);

        // Encodes values from the inspector into allocated space
        public void PackFromFields()
        {
            word0 = PackedItemSchema.PackWord0(itemType, statFlags, spriteKey, textKey);
            word1 = PackedItemSchema.PackWord1((byte)health, (byte)power, (byte)armor, (byte)agility);
            word2 = PackedItemSchema.PackWord2((byte)vigor, (byte)fortune, (byte)range, (byte)status);
        }

        // unpacks encoded data for inspection
        public void UnpackToFields()
        {
            var data = new PackedItemData(word0, word1, word2);
            itemType = data.ItemType;
            statFlags = data.StatFlags;
            spriteKey = data.SpriteKey;
            textKey = data.TextKey;
            health = data.Health;
            power = data.Power;
            armor = data.Armor;
            agility = data.Agility;
            vigor = data.Vigor;
            fortune = data.Fortune;
            range = data.Range;
            status = data.Status;
        }

        // Randomizes stats if flags are enabled and packs them
        public void RandomizeStatsAndPack()
        {
            if ((statFlags & StatFlags.Health) != 0) health = Random.Range(0, 256);
            if ((statFlags & StatFlags.Power) != 0) power = Random.Range(0, 256);
            if ((statFlags & StatFlags.Armor) != 0) armor = Random.Range(0, 256);
            if ((statFlags & StatFlags.Agility) != 0) agility = Random.Range(0, 256);
            if ((statFlags & StatFlags.Vigor) != 0) vigor = Random.Range(0, 256);
            if ((statFlags & StatFlags.Fortune) != 0) fortune = Random.Range(0, 256);
            if ((statFlags & StatFlags.Range) != 0) range = Random.Range(0, 256);
            if ((statFlags & StatFlags.Status) != 0) status = Random.Range(0, 256);
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