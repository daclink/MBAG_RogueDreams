using DataSchemas.PackedItem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Always-visible row of the first N items in <see cref="PlayerInventory"/> (default 4).
/// </summary>
[DisallowMultipleComponent]
public class InventoryHotbarUI : MonoBehaviour
{
    public const int DefaultSlotCount = 4;

    [Header("Target")]
    [SerializeField] private PlayerInventory _inventory;

    [Tooltip("If null, uses the first Canvas in the scene; creates a dedicated canvas if none.")]
    [SerializeField] private Canvas _canvas;

    [Header("Layout")]
    [Min(1)]
    [SerializeField] private int _slotCount = DefaultSlotCount;
    [SerializeField] private Vector2 _cellSize = new Vector2(56f, 56f);
    [SerializeField] private float _bottomOffset = 24f;
    [SerializeField] private float _slotSpacing = 8f;

    [Header("Style")]
    [SerializeField] private Color _emptySlotColor = new Color(0.12f, 0.12f, 0.14f, 0.9f);
    [SerializeField] private Color _filledSlotColor = new Color(0.2f, 0.2f, 0.24f, 0.95f);
    [SerializeField] private int _sortOrder = 40;

    Image[] _icons;
    Image[] _backgrounds;
    bool _built;

    void Awake()
    {
        if (_canvas == null)
        {
            _canvas = Object.FindFirstObjectByType<Canvas>();
            if (_canvas == null)
            {
                var go = new GameObject("Canvas_Hotbar");
                _canvas = go.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = _sortOrder;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
            }
        }

        if (_canvas != null && _canvas.sortingOrder < _sortOrder)
            _canvas.sortingOrder = _sortOrder;

        Build();
    }

    void Start()
    {
        if (_inventory == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                _inventory = p.GetComponent<PlayerInventory>() ?? p.GetComponentInParent<PlayerInventory>() ??
                    p.GetComponentInChildren<PlayerInventory>(true);
        }

        if (_inventory != null)
            _inventory.InventoryChanged += OnInventoryChanged;

        Refresh();
    }

    void OnDestroy()
    {
        if (_inventory != null)
            _inventory.InventoryChanged -= OnInventoryChanged;
    }

    void OnInventoryChanged() => Refresh();

    void Build()
    {
        if (_built) return;
        _built = true;
        int n = Mathf.Max(1, _slotCount);
        _icons = new Image[n];
        _backgrounds = new Image[n];

        var root = new GameObject("HotbarRoot", typeof(RectTransform));
        root.transform.SetParent(_canvas.transform, false);
        var rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var bar = new GameObject("Hotbar", typeof(RectTransform));
        bar.transform.SetParent(root.transform, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0f);
        barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0f, _bottomOffset);
        float width = n * _cellSize.x + Mathf.Max(0, n - 1) * _slotSpacing + 32f;
        barRt.sizeDelta = new Vector2(width, _cellSize.y + 24f);

        var barBg = bar.AddComponent<Image>();
        barBg.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);
        barBg.raycastTarget = false;

        var h = bar.AddComponent<HorizontalLayoutGroup>();
        h.spacing = _slotSpacing;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false;
        h.childControlHeight = false;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.padding = new RectOffset(16, 16, 8, 8);

        for (int i = 0; i < n; i++)
        {
            var cell = new GameObject($"Slot_{i}", typeof(RectTransform));
            cell.transform.SetParent(bar.transform, false);
            var le = cell.AddComponent<LayoutElement>();
            le.minWidth = _cellSize.x;
            le.minHeight = _cellSize.y;
            le.preferredWidth = _cellSize.x;
            le.preferredHeight = _cellSize.y;

            var bg = cell.AddComponent<Image>();
            bg.color = _emptySlotColor;
            bg.raycastTarget = false;
            _backgrounds[i] = bg;

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cell.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            const float inset = 6f;
            iconRt.sizeDelta = new Vector2(_cellSize.x - inset, _cellSize.y - inset);
            var icon = iconGo.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            _icons[i] = icon;
        }
    }

    void Refresh()
    {
        if (!_built || _icons == null) return;
        var table = ItemTableBootstrap.Instance != null ? ItemTableBootstrap.Instance.Table : null;
        var spriteTable = table != null ? table.SpriteTable : null;
        int cap = _inventory != null ? _inventory.Count : 0;
        for (int i = 0; i < _icons.Length; i++)
        {
            if (i < cap && _inventory != null)
            {
                var r = _inventory.Slots[i];
                _backgrounds[i].color = _filledSlotColor;
                Sprite s = null;
                if (spriteTable != null) s = spriteTable.Get(r.Type, r.BiomeFlags, r.Key);
                _icons[i].sprite = s;
                _icons[i].color = s != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            }
            else
            {
                _backgrounds[i].color = _emptySlotColor;
                _icons[i].sprite = null;
                _icons[i].color = Color.white;
            }
        }
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
