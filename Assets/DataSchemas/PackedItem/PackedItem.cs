namespace DataSchemas.PackedItem
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "Items/Packed Item Asset")]
    public class PackedItemAsset : ScriptableObject
    {
        // editor fields
        [Header("Readable fields")]
        public PackedItemType itemType;
        public PackedStatFlags statFlags;
        public PackedRuntimeFlags runtimeFlags;

        // Allows object authorship within allowable range
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
            word0 = PackedItemSchema.PackWord0(itemType, statFlags, runtimeFlags);
            word1 = PackedItemSchema.PackWord1((byte)health, (byte)power, (byte)armor, (byte)agility);
            word2 = PackedItemSchema.PackWord2((byte)vigor, (byte)fortune, (byte)range, (byte)status);
        }

        // unpacks encoded data for inspection
        public void UnpackToFields()
        {
            var data = new PackedItemData(word0, word1, word2);
            itemType = data.ItemType;
            statFlags = data.StatFlags;
            runtimeFlags = data.RuntimeFlags;
            health = data.Health;
            power = data.Power;
            armor = data.Armor;
            agility = data.Agility;
            vigor = data.Vigor;
            fortune = data.Fortune;
            range = data.Range;
            status = data.Status;
        }

        // Randomizes stats if flags are anabled and packs them
        public void RandomizeStatsAndPack()
        {
            if ((statFlags & PackedStatFlags.Health) != 0) health = Random.Range(0, 256);
            if ((statFlags & PackedStatFlags.Power) != 0) power = Random.Range(0, 256);
            if ((statFlags & PackedStatFlags.Armor) != 0) armor = Random.Range(0, 256);
            if ((statFlags & PackedStatFlags.Agility) != 0) agility = Random.Range(0, 256);
            if ((statFlags & PackedStatFlags.Vigor) != 0) vigor = Random.Range(0, 256);
            if ((statFlags & PackedStatFlags.Fortune) != 0) fortune = Random.Range(0, 256);
            if ((statFlags & PackedStatFlags.Range) != 0) range = Random.Range(0, 256);
            if ((statFlags & PackedStatFlags.Status) != 0) status = Random.Range(0, 256);
            PackFromFields();
        }
    }
}