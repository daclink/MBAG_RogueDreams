using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using DataSchemas.PackedItem;
using Tables;

namespace DataSchemas.PackedItem.Editor
{
    public class ItemAuthoringWindow : EditorWindow
    {
        [MenuItem("Tools/Item Authoring")]
        [MenuItem("Window/Item Authoring")]
        public static void Open() => GetWindow<ItemAuthoringWindow>("Item Authoring");

        [SerializeField] SpriteTable2D _spriteTable;
        [SerializeField] TextTable2D _textTable;
        [SerializeField] string _itemsPath = "";
        [SerializeField] string _spritesPath = "";
        [SerializeField] string _textsPath = "";
        [SerializeField] Vector2 _scrollPosition;

        PackedItemTable _table;
        (ItemType type, int partition, byte key)? _selected;
        ItemType? _addModeForType;

        string ItemsPath => !string.IsNullOrEmpty(_itemsPath) ? _itemsPath : Path.Combine(Application.persistentDataPath, PackedItemStorage.DefaultFileName);
        string SpritesPath => !string.IsNullOrEmpty(_spritesPath) ? _spritesPath : Path.Combine(Application.persistentDataPath, SpriteTableStorage.DefaultFileName);
        string TextsPath => !string.IsNullOrEmpty(_textsPath) ? _textsPath : Path.Combine(Application.persistentDataPath, TextTableStorage.DefaultFileName);

        void OnEnable()
        {
            if (_spriteTable != null && _textTable != null && _table == null)
                _table = new PackedItemTable(_spriteTable, _textTable);
        }

        bool HasRequiredTables => _spriteTable != null && _textTable != null;

        void OnGUI()
        {
            EditorGUILayout.Space(4);
            DrawSetup();
            if (!HasRequiredTables)
            {
                EditorGUILayout.HelpBox("Assign both Sprite Table 2D and Text Table 2D to use the authoring tools.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8);
            DrawLoadSave();
            EditorGUILayout.Space(8);

            if (_table == null) _table = new PackedItemTable(_spriteTable, _textTable);
            _table.TextTable = _textTable;
            DrawDetails();
            DrawAddForm();
            GUILayout.FlexibleSpace();
            DrawGrid();
        }

        void DrawSetup()
        {
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            _spriteTable = (SpriteTable2D)EditorGUILayout.ObjectField("Sprite Table 2D", _spriteTable, typeof(SpriteTable2D), false);
            _textTable = (TextTable2D)EditorGUILayout.ObjectField("Text Table 2D", _textTable, typeof(TextTable2D), false);
            _itemsPath = EditorGUILayout.TextField("Items path", _itemsPath);
            _spritesPath = EditorGUILayout.TextField("Sprites path", _spritesPath);
            _textsPath = EditorGUILayout.TextField("Texts path", _textsPath);
            if (GUILayout.Button("Use persistentDataPath + default names"))
            {
                _itemsPath = Path.Combine(Application.persistentDataPath, PackedItemStorage.DefaultFileName);
                _spritesPath = Path.Combine(Application.persistentDataPath, SpriteTableStorage.DefaultFileName);
                _textsPath = Path.Combine(Application.persistentDataPath, TextTableStorage.DefaultFileName);
            }
        }

        void DrawLoadSave()
        {
            EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                try
                {
                    _table.LoadFromFiles(ItemsPath, SpritesPath, TextsPath);
                    _selected = null;
                    _addModeForType = null;
                    _editPopulated = false;
                    Debug.Log($"Loaded from {ItemsPath}, {SpritesPath}, {TextsPath}");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Load Failed", ex.Message, "OK");
                }
            }
            if (GUILayout.Button("Save"))
            {
                try
                {
                    _table.SaveToFile(ItemsPath);
                    _spriteTable.SaveFromFile(SpritesPath);
                    _textTable.SaveFromFile(TextsPath);
                    Debug.Log($"Saved to {ItemsPath}, {SpritesPath}, {TextsPath}");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Save Failed", ex.Message, "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawGrid()
        {
            EditorGUILayout.LabelField("Items by Type", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            var types = new[] { ItemType.Weapon, ItemType.Armor, ItemType.Consumable, ItemType.KeyItem };
            foreach (ItemType type in types)
            {
                DrawRow(type);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawRow(ItemType type)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(80));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(type.ToString(), EditorStyles.boldLabel, GUILayout.Width(100));
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                _addModeForType = _addModeForType == type ? null : type;
                _selected = null;
                _editPopulated = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            for (int p = 0; p < PackedItemTableCore.BiomePartitionCount; p++)
            {
                EditorGUILayout.BeginVertical(GUILayout.MinWidth(100));
                EditorGUILayout.LabelField(PartitionLabel(p), EditorStyles.miniLabel, GUILayout.Height(16));
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.MinHeight(52));

                int baseIdx = ((byte)type) * PackedItemTableCore.KeysPerType + p * PackedItemTableCore.SlotsPerBiome;
                for (byte k = 0; k < PackedItemTableCore.SlotsPerBiome; k++)
                {
                    if (!_table.HasEntryAt(baseIdx + k)) continue;
                    var data = _table.Get(type, ToBiomeFlags(p), k);
                    if (!data.HasValue) continue;

                    Sprite preview = _spriteTable.Get(type, data.Value.BiomeFlags, k);
                    var content = preview != null && AssetPreview.GetAssetPreview(preview) != null
                        ? new GUIContent(AssetPreview.GetAssetPreview(preview), $"{type} {PartitionLabel(p)} k{k}")
                        : new GUIContent(k.ToString(), $"{type} {PartitionLabel(p)} k{k}");

                    bool isSelected = _selected.HasValue && _selected.Value.type == type && _selected.Value.partition == p && _selected.Value.key == k;
                    if (isSelected)
                    {
                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
                        if (GUILayout.Button(content, GUILayout.Width(44), GUILayout.Height(44)))
                        {
                            _selected = (type, p, k);
                            _addModeForType = null;
                            _editPopulated = false;
                        }
                        GUI.backgroundColor = prev;
                    }
                    else if (GUILayout.Button(content, GUILayout.Width(44), GUILayout.Height(44)))
                    {
                        _selected = (type, p, k);
                        _addModeForType = null;
                        _editPopulated = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>Partition maps 1:1 to biome byte value. Extend BiomeFlags enum as needed.</summary>
        static BiomeFlags ToBiomeFlags(int partition)
        {
            if (partition < 0 || partition >= PackedItemTableCore.BiomePartitionCount) return BiomeFlags.None;
            return (BiomeFlags)partition;
        }

        static (string name, string desc) ParseNameDesc(string text)
        {
            if (string.IsNullOrEmpty(text)) return ("", "");
            int i = text.IndexOf('|');
            return i >= 0 ? (text.Substring(0, i), text.Substring(i + 1)) : (text, "");
        }

        static string ConcatenateNameDesc(string name, string desc)
        {
            return string.IsNullOrEmpty(desc) ? (name ?? "") : (name ?? "") + "|" + (desc ?? "");
        }

        static string PartitionLabel(int partition)
        {
            if (partition < 0 || partition >= PackedItemTableCore.BiomePartitionCount) return "?";
            var f = (BiomeFlags)partition;
            var name = f.ToString();
            return name != partition.ToString() ? name : $"Biome {partition}";
        }

        void DrawDetails()
        {
            if (!_selected.HasValue) return;

            var (type, partition, key) = _selected.Value;
            var biomeFlags = ToBiomeFlags(partition);
            var data = _table.Get(type, biomeFlags, key);
            if (!data.HasValue) { _selected = null; _editPopulated = false; return; }

            var d = data.Value;
            if (!_editPopulated)
            {
                _editTexture2D = null;
                var (n, desc) = ParseNameDesc(_textTable.Get(type, biomeFlags, key));
                _editName = n;
                _editDescription = desc;
                _editBiomeFlags = d.BiomeFlags;
                _editAspectFlags = d.AspectFlags;
                _editStatusFlags = d.StatusFlags;
                _editRarityFlags = d.RarityFlags;
                _editHealth = d.Health;
                _editPower = d.Power;
                _editArmor = d.Armor;
                _editAgility = d.Agility;
                _editVigor = d.Vigor;
                _editFortune = d.Fortune;
                _editRange = d.Range;
                _editRarity = d.Rarity;
                _editPopulated = true;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Edit Item", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Type", type.ToString());
            EditorGUILayout.LabelField("Key", key.ToString());

            Sprite iconPreview = _spriteTable.Get(type, biomeFlags, key);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Icon (frame 0)", iconPreview, typeof(Sprite), false);
            EditorGUI.EndDisabledGroup();
            _editTexture2D = (Texture2D)EditorGUILayout.ObjectField("Spritesheet (Texture2D)", _editTexture2D, typeof(Texture2D), false);
            if (_editTexture2D != null)
                EditorGUILayout.HelpBox("Assign a Texture2D to replace. Sliced at 64×64. Max 16 frames (icon + 15 animation). Enable Read/Write.", MessageType.None);
            _editName = EditorGUILayout.TextField("Name", _editName ?? "");
            _editDescription = EditorGUILayout.TextField("Description", _editDescription ?? "");
            _editBiomeFlags = (BiomeFlags)EditorGUILayout.EnumFlagsField("Biome Flags", _editBiomeFlags ?? d.BiomeFlags);
            _editAspectFlags = (AspectFlags)EditorGUILayout.EnumFlagsField("Aspect Flags", _editAspectFlags ?? d.AspectFlags);
            _editStatusFlags = (StatusFlags)EditorGUILayout.EnumFlagsField("Status Flags", _editStatusFlags ?? d.StatusFlags);
            _editRarityFlags = (RarityFlags)EditorGUILayout.EnumFlagsField("Rarity Flags", _editRarityFlags ?? d.RarityFlags);

            var editAspect = _editAspectFlags ?? d.AspectFlags;
            _editHealth ??= d.Health;
            _editPower ??= d.Power;
            _editArmor ??= d.Armor;
            _editAgility ??= d.Agility;
            _editVigor ??= d.Vigor;
            _editFortune ??= d.Fortune;
            _editRange ??= d.Range;
            _editRarity ??= d.Rarity;

            if ((editAspect & AspectFlags.Health) != 0) _editHealth = EditorGUILayout.IntSlider("Health", _editHealth.Value, -128, 127); else _editHealth = 0;
            if ((editAspect & AspectFlags.Power) != 0) _editPower = EditorGUILayout.IntSlider("Power", _editPower.Value, -128, 127); else _editPower = 0;
            if ((editAspect & AspectFlags.Armor) != 0) _editArmor = EditorGUILayout.IntSlider("Armor", _editArmor.Value, -128, 127); else _editArmor = 0;
            if ((editAspect & AspectFlags.Agility) != 0) _editAgility = EditorGUILayout.IntSlider("Agility", _editAgility.Value, -128, 127); else _editAgility = 0;
            if ((editAspect & AspectFlags.Vigor) != 0) _editVigor = EditorGUILayout.IntSlider("Vigor", _editVigor.Value, -128, 127); else _editVigor = 0;
            if ((editAspect & AspectFlags.Fortune) != 0) _editFortune = EditorGUILayout.IntSlider("Fortune", _editFortune.Value, -128, 127); else _editFortune = 0;
            if ((editAspect & AspectFlags.Range) != 0) _editRange = EditorGUILayout.IntSlider("Range", _editRange.Value, -128, 127); else _editRange = 0;
            if ((editAspect & AspectFlags.Rarity) != 0) _editRarity = EditorGUILayout.IntSlider("Rarity", _editRarity.Value, -128, 127); else _editRarity = 0;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                Sprite[] editSpritesheet = _editTexture2D != null
                    ? SpriteSheetUtility.SliceTexture(_editTexture2D)
                    : _spriteTable.GetSpritesheet(type, biomeFlags, key);
                if (editSpritesheet == null || editSpritesheet.Length == 0)
                {
                    EditorUtility.DisplayDialog("Missing Spritesheet", "Assign a Texture2D spritesheet or ensure the item has one.", "OK");
                }
                else if (string.IsNullOrWhiteSpace(_editName))
                {
                    EditorUtility.DisplayDialog("Missing Name", "A name is required.", "OK");
                }
                else
                {
                    var (success, newKeyIfMoved) = _table.Update(type, biomeFlags, key, editSpritesheet,
                        _editBiomeFlags ?? d.BiomeFlags, editAspect, _editStatusFlags ?? d.StatusFlags, _editRarityFlags ?? d.RarityFlags,
                        (sbyte)((editAspect & AspectFlags.Health) != 0 ? _editHealth ?? 0 : 0),
                        (sbyte)((editAspect & AspectFlags.Power) != 0 ? _editPower ?? 0 : 0),
                        (sbyte)((editAspect & AspectFlags.Armor) != 0 ? _editArmor ?? 0 : 0),
                        (sbyte)((editAspect & AspectFlags.Agility) != 0 ? _editAgility ?? 0 : 0),
                        (sbyte)((editAspect & AspectFlags.Vigor) != 0 ? _editVigor ?? 0 : 0),
                        (sbyte)((editAspect & AspectFlags.Fortune) != 0 ? _editFortune ?? 0 : 0),
                        (sbyte)((editAspect & AspectFlags.Range) != 0 ? _editRange ?? 0 : 0),
                        (sbyte)((editAspect & AspectFlags.Rarity) != 0 ? _editRarity ?? 0 : 0),
                        ConcatenateNameDesc(_editName, _editDescription));
                    if (success)
                    {
                        EditorUtility.SetDirty(_spriteTable);
                        EditorUtility.SetDirty(_textTable);
                        _editPopulated = false;
                        if (newKeyIfMoved.HasValue)
                        {
                            int newPartition = PackedItemTableCore.GetPartitionIndex(_editBiomeFlags ?? d.BiomeFlags);
                            _selected = (type, newPartition, newKeyIfMoved.Value);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Partition Full",
                            "The target partition has no free slots. Delete an existing item in that partition before moving this one, or choose a different biome.", "OK");
                    }
                }
            }
            if (GUILayout.Button("Delete", GUILayout.Height(24)))
            {
                _table.Remove(type, biomeFlags, key);
                EditorUtility.SetDirty(_spriteTable);
                EditorUtility.SetDirty(_textTable);
                _selected = null;
                _editPopulated = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        Texture2D _editTexture2D;
        string _editName;
        string _editDescription;
        BiomeFlags? _editBiomeFlags;
        AspectFlags? _editAspectFlags;
        StatusFlags? _editStatusFlags;
        RarityFlags? _editRarityFlags;
        int? _editHealth, _editPower, _editArmor, _editAgility, _editVigor, _editFortune, _editRange, _editRarity;
        bool _editPopulated;

        void DrawAddForm()
        {
            if (!_addModeForType.HasValue) return;

            ItemType type = _addModeForType.Value;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Add {type}", EditorStyles.boldLabel);

            _addTexture2D = (Texture2D)EditorGUILayout.ObjectField("Spritesheet (Texture2D)", _addTexture2D, typeof(Texture2D), false);
            if (_addTexture2D != null)
                EditorGUILayout.HelpBox("Sliced at 64×64. Max 16 frames (icon + 15 animation). Enable Read/Write.", MessageType.None);
            _addName = EditorGUILayout.TextField("Name", _addName ?? "");
            _addDescription = EditorGUILayout.TextField("Description", _addDescription ?? "");
            _addBiomeFlags = (BiomeFlags)EditorGUILayout.EnumFlagsField("Biome Flags", _addBiomeFlags ?? BiomeFlags.Forest);
            _addAspectFlags = (AspectFlags)EditorGUILayout.EnumFlagsField("Aspect Flags", _addAspectFlags ?? AspectFlags.None);
            _addStatusFlags = (StatusFlags)EditorGUILayout.EnumFlagsField("Status Flags", _addStatusFlags ?? StatusFlags.None);
            _addRarityFlags = (RarityFlags)EditorGUILayout.EnumFlagsField("Rarity Flags", _addRarityFlags ?? RarityFlags.None);

            var addAspect = _addAspectFlags ?? AspectFlags.None;
            _addHealth ??= 0;
            _addPower ??= 0;
            _addArmor ??= 0;
            _addAgility ??= 0;
            _addVigor ??= 0;
            _addFortune ??= 0;
            _addRange ??= 0;
            _addRarity ??= 0;

            if ((addAspect & AspectFlags.Health) != 0) _addHealth = EditorGUILayout.IntSlider("Health", _addHealth.Value, -128, 127); else _addHealth = 0;
            if ((addAspect & AspectFlags.Power) != 0) _addPower = EditorGUILayout.IntSlider("Power", _addPower.Value, -128, 127); else _addPower = 0;
            if ((addAspect & AspectFlags.Armor) != 0) _addArmor = EditorGUILayout.IntSlider("Armor", _addArmor.Value, -128, 127); else _addArmor = 0;
            if ((addAspect & AspectFlags.Agility) != 0) _addAgility = EditorGUILayout.IntSlider("Agility", _addAgility.Value, -128, 127); else _addAgility = 0;
            if ((addAspect & AspectFlags.Vigor) != 0) _addVigor = EditorGUILayout.IntSlider("Vigor", _addVigor.Value, -128, 127); else _addVigor = 0;
            if ((addAspect & AspectFlags.Fortune) != 0) _addFortune = EditorGUILayout.IntSlider("Fortune", _addFortune.Value, -128, 127); else _addFortune = 0;
            if ((addAspect & AspectFlags.Range) != 0) _addRange = EditorGUILayout.IntSlider("Range", _addRange.Value, -128, 127); else _addRange = 0;
            if ((addAspect & AspectFlags.Rarity) != 0) _addRarity = EditorGUILayout.IntSlider("Rarity", _addRarity.Value, -128, 127); else _addRarity = 0;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                Sprite[] addSpritesheet = _addTexture2D != null ? SpriteSheetUtility.SliceTexture(_addTexture2D) : null;
                if (addSpritesheet == null || addSpritesheet.Length == 0)
                {
                    EditorUtility.DisplayDialog("Missing Spritesheet", "Assign a Texture2D spritesheet. Sliced at 64×64.", "OK");
                }
                else if (string.IsNullOrWhiteSpace(_addName))
                {
                    EditorUtility.DisplayDialog("Missing Name", "A name is required.", "OK");
                }
                else
                {
                    int key = _table.Add(type, _addBiomeFlags.Value, addSpritesheet,
                        addAspect, _addStatusFlags.Value, _addRarityFlags.Value,
                        (sbyte)((addAspect & AspectFlags.Health) != 0 ? _addHealth.Value : 0),
                        (sbyte)((addAspect & AspectFlags.Power) != 0 ? _addPower.Value : 0),
                        (sbyte)((addAspect & AspectFlags.Armor) != 0 ? _addArmor.Value : 0),
                        (sbyte)((addAspect & AspectFlags.Agility) != 0 ? _addAgility.Value : 0),
                        (sbyte)((addAspect & AspectFlags.Vigor) != 0 ? _addVigor.Value : 0),
                        (sbyte)((addAspect & AspectFlags.Fortune) != 0 ? _addFortune.Value : 0),
                        (sbyte)((addAspect & AspectFlags.Range) != 0 ? _addRange.Value : 0),
                        (sbyte)((addAspect & AspectFlags.Rarity) != 0 ? _addRarity.Value : 0),
                        ConcatenateNameDesc(_addName, _addDescription));
                    if (key >= 0)
                    {
                        EditorUtility.SetDirty(_spriteTable);
                        EditorUtility.SetDirty(_textTable);
                        _addModeForType = null;
                        ResetAddForm();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Partition Full",
                            "This partition has no free slots. Delete an existing item in this partition before adding a new one, or choose a different biome.", "OK");
                    }
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                _addModeForType = null;
                ResetAddForm();
            }
            EditorGUILayout.EndHorizontal();
        }

        Texture2D _addTexture2D;
        string _addName;
        string _addDescription;
        AspectFlags? _addAspectFlags;
        StatusFlags? _addStatusFlags;
        RarityFlags? _addRarityFlags;
        BiomeFlags? _addBiomeFlags;
        int? _addHealth, _addPower, _addArmor, _addAgility, _addVigor, _addFortune, _addRange, _addRarity;

        void ResetAddForm()
        {
            _addTexture2D = null;
            _addName = null;
            _addDescription = null;
            _addAspectFlags = AspectFlags.None;
            _addStatusFlags = StatusFlags.None;
            _addRarityFlags = RarityFlags.None;
            _addBiomeFlags = BiomeFlags.Forest;
            _addHealth = _addPower = _addArmor = _addAgility = _addVigor = _addFortune = _addRange = _addRarity = 0;
        }
    }
}
