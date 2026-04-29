using System.Collections;
using DataSchemas.PackedItem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Toggle with Tab: shows a grid of item icons from the player's <see cref="PlayerInventory"/>,
/// using <see cref="ItemTableBootstrap"/> to resolve sprites. Click a weapon or armor to equip (via
/// <see cref="PlayerEquipment"/>).
/// Add to any active GameObject in play scenes (e.g. under a Canvas, or a canvas will be created).
/// </summary>
[DisallowMultipleComponent]
public class InventoryScreenUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("If null, uses Player-tagged object with PlayerInventory.")]
    [SerializeField] private PlayerInventory _inventory;

    [Tooltip("If null, uses Player-tagged object with PlayerEquipment.")]
    [SerializeField] private PlayerEquipment _equipment;

    [Tooltip("If null, uses the first Canvas in the scene; creates an overlay canvas if none.")]
    [SerializeField] private Canvas _canvas;

    [Header("Input")]
    [SerializeField] private Key _toggleKey = Key.Tab;

    [Header("Layout")]
    [SerializeField] private Vector2 _cellSize = new Vector2(72f, 72f);
    [SerializeField] private Vector2 _spacing = new Vector2(8f, 8f);
    [Min(1)]
    [SerializeField] private int _columns = 6;

    [Header("Style")]
    [SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color _slotBackground = new Color(0.2f, 0.2f, 0.24f, 0.95f);

    [Header("Debug")]
    [SerializeField] private int _sortOrder = 200;

    GameObject _screenRoot;
    RectTransform _gridContent;
    bool _visible;

    void Awake()
    {
        if (_canvas == null)
        {
            _canvas = Object.FindFirstObjectByType<Canvas>();
            if (_canvas == null)
            {
                var canvasGo = new GameObject("Canvas_Inventory");
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = _sortOrder;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }
        }
        else if (_canvas.sortingOrder < _sortOrder)
        {
            _canvas.sortingOrder = _sortOrder;
        }

        BuildUi();
    }

    void Start()
    {
        if (_inventory == null || _equipment == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                if (_inventory == null)
                    _inventory = p.GetComponent<PlayerInventory>() ?? p.GetComponentInParent<PlayerInventory>() ??
                        p.GetComponentInChildren<PlayerInventory>(true);
                if (_equipment == null)
                    _equipment = p.GetComponent<PlayerEquipment>() ?? p.GetComponentInParent<PlayerEquipment>() ??
                        p.GetComponentInChildren<PlayerEquipment>(true);
            }
        }

        if (_inventory != null)
            _inventory.InventoryChanged += OnInventoryChanged;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        if (_inventory != null)
            _inventory.InventoryChanged -= OnInventoryChanged;
    }

    void OnInventoryChanged()
    {
        if (_visible) ScheduleRefresh();
    }

    void OnSlotClicked(int slotIndex)
    {
        if (_equipment == null) return;
        if (_equipment.TryEquipFromInventoryIndex(slotIndex))
            ScheduleRefresh();
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;
        if (!k[_toggleKey].wasPressedThisFrame) return;

        _visible = !_visible;
        if (_screenRoot != null)
        {
            _screenRoot.SetActive(_visible);
            if (_visible)
            {
                _screenRoot.transform.SetAsLastSibling();
                ScheduleRefresh();
            }
        }
    }

    void BuildUi()
    {
        _screenRoot = new GameObject("InventoryScreen");
        _screenRoot.transform.SetParent(_canvas.transform, false);
        var rootRt = _screenRoot.AddComponent<RectTransform>();
        StretchFull(rootRt);
        _screenRoot.SetActive(false);

        var dim = new GameObject("Dim");
        dim.transform.SetParent(_screenRoot.transform, false);
        var dimRt = dim.AddComponent<RectTransform>();
        StretchFull(dimRt);
        var dimImg = dim.AddComponent<Image>();
        dimImg.color = _dimColor;
        dimImg.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(_screenRoot.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(700f, 480f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);
        panelImg.raycastTarget = true;

        var gridGo = new GameObject("GridContent", typeof(RectTransform));
        _gridContent = gridGo.GetComponent<RectTransform>();
        _gridContent.SetParent(panel.transform, false);
        _gridContent.anchorMin = new Vector2(0f, 0f);
        _gridContent.anchorMax = new Vector2(1f, 1f);
        _gridContent.pivot = new Vector2(0.5f, 0.5f);
        const float pad = 16f;
        _gridContent.offsetMin = new Vector2(pad, pad);
        _gridContent.offsetMax = new Vector2(-pad, -pad);

        var grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = _cellSize;
        grid.spacing = _spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = _columns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
    }

    void ScheduleRefresh()
    {
        StopAllCoroutines();
        StartCoroutine(RefreshWhenGridClear());
    }

    IEnumerator RefreshWhenGridClear()
    {
        if (_gridContent == null) yield break;
        int before = _gridContent.childCount;
        for (int i = _gridContent.childCount - 1; i >= 0; i--)
            Object.Destroy(_gridContent.GetChild(i).gameObject);
        if (before > 0)
            yield return null;
        if (!_visible || _gridContent == null) yield break;
        if (_inventory == null || _inventory.Count == 0) yield break;

        var table = ItemTableBootstrap.Instance != null ? ItemTableBootstrap.Instance.Table : null;
        var spriteTable = table != null ? table.SpriteTable : null;

        for (int i = 0; i < _inventory.Count; i++)
        {
            var reference = _inventory.Slots[i];
            var cell = new GameObject($"Item_{i}", typeof(RectTransform));
            cell.transform.SetParent(_gridContent, false);
            var bg = cell.AddComponent<Image>();
            bg.color = _slotBackground;
            bg.raycastTarget = true;

            var button = cell.AddComponent<Button>();
            button.targetGraphic = bg;
            int slot = i;
            button.onClick.AddListener(() => OnSlotClicked(slot));

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cell.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            const float inset = 8f;
            iconRt.sizeDelta = new Vector2(_cellSize.x - inset, _cellSize.y - inset);
            var icon = iconGo.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            if (spriteTable != null)
            {
                var s = spriteTable.Get(reference.Type, reference.BiomeFlags, reference.Key);
                if (s != null) icon.sprite = s;
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
