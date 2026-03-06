using System;
using System.IO;
using UnityEngine;
using Tables;

namespace DataSchemas.PackedItem
{
    /// <summary>
    /// Loads packed item data at game start. Place in scene and assign table assets.
    /// Access PackedItemTable via Instance.Table after Awake.
    /// </summary>
    public class ItemTableBootstrap : MonoBehaviour
    {
        [SerializeField] SpriteTable2D _spriteTable;
        [SerializeField] TextTable2D _textTable;
        [SerializeField] string _itemsPath = "";
        [SerializeField] string _spritesPath = "";
        [SerializeField] string _textsPath = "";

        static ItemTableBootstrap _instance;
        PackedItemTable _table;

        /// <summary>Bootstrap instance. May be null before Awake.</summary>
        public static ItemTableBootstrap Instance => _instance;

        /// <summary>Packed item table. Valid after Awake.</summary>
        public PackedItemTable Table => _table;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("ItemTableBootstrap: Multiple instances; keeping first.");
                return;
            }
            _instance = this;

            if (_spriteTable == null)
            {
                Debug.LogError("ItemTableBootstrap: SpriteTable2D not assigned.");
                return;
            }

            _table = new PackedItemTable(_spriteTable, _textTable);

            string itemsPath = GetPath(_itemsPath, PackedItemStorage.DefaultFileName);
            string spritesPath = GetPath(_spritesPath, SpriteTableStorage.DefaultFileName);
            string textsPath = GetPath(_textsPath, TextTableStorage.DefaultFileName);

            try
            {
                _table.LoadFromFiles(itemsPath, spritesPath, textsPath);
                Debug.Log($"ItemTableBootstrap: Loaded from {itemsPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ItemTableBootstrap: Load failed. {ex.Message}");
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        static string GetPath(string custom, string defaultFileName)
        {
            if (!string.IsNullOrWhiteSpace(custom))
                return custom;
            return Path.Combine(Application.persistentDataPath, defaultFileName);
        }
    }
}
