using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using OurGame.Core;

public class InventoryUIController : SingletonMono<InventoryUIController>
{
    private readonly List<InventorySlotView> inventorySlotViews = new List<InventorySlotView>();
    private readonly List<InventorySlotView> hotbarSlotViews = new List<InventorySlotView>();

    private InventorySystem inventorySystem;
    private Font uiFont;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform inventoryOverlay;
    private RectTransform inventoryPanel;
    private RectTransform hotbarRoot;
    private RectTransform cursorRoot;
    private InventorySlotView cursorSlotView;
    private Text selectedItemText;
    private bool built;

    protected override void Awake()
    {
        base.Awake();
        EnsureEventSystem();
        BuildUiIfNeeded();
    }

    void Start()
    {
        InitializeFromSystem(InventorySystem.Instance);
    }

    void Update()
    {
        if (!built || inventorySystem == null)
            return;

        UpdateCursorStack();
    }

    public void InitializeFromSystem(InventorySystem system)
    {
        BuildUiIfNeeded();

        if (inventorySystem == system)
        {
            RefreshAll();
            return;
        }

        Unsubscribe();
        inventorySystem = system;
        Subscribe();
        RefreshAll();
    }

    public void HandleSlotClick(
        InventorySection section,
        int index,
        PointerEventData.InputButton button
    )
    {
        if (inventorySystem == null)
            return;

        if (!inventorySystem.IsInventoryOpen)
        {
            if (section == InventorySection.Hotbar && button == PointerEventData.InputButton.Left)
                inventorySystem.SelectHotbarSlot(index);

            return;
        }

        if (button == PointerEventData.InputButton.Left)
            inventorySystem.HandleSlotLeftClick(section, index);
        else if (button == PointerEventData.InputButton.Right)
            inventorySystem.HandleSlotRightClick(section, index);
    }

    private void Subscribe()
    {
        if (inventorySystem == null)
            return;

        inventorySystem.OnInventoryChanged += RefreshAll;
        inventorySystem.OnInventoryToggled += HandleInventoryToggled;
        inventorySystem.OnSelectedHotbarChanged += HandleHotbarSelectionChanged;
        inventorySystem.OnHeldItemChanged += HandleHeldItemChanged;
    }

    private void Unsubscribe()
    {
        if (inventorySystem == null)
            return;

        inventorySystem.OnInventoryChanged -= RefreshAll;
        inventorySystem.OnInventoryToggled -= HandleInventoryToggled;
        inventorySystem.OnSelectedHotbarChanged -= HandleHotbarSelectionChanged;
        inventorySystem.OnHeldItemChanged -= HandleHeldItemChanged;
    }

    private void BuildUiIfNeeded()
    {
        if (built)
            return;

        built = true;
        uiFont = ResolveFont();

        GameObject canvasObject = new GameObject(
            "InventoryCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        BuildInventoryOverlay();
        BuildHotbar();
        BuildCursorView();
    }

    private void BuildInventoryOverlay()
    {
        inventoryOverlay = CreatePanel(
            "InventoryOverlay",
            canvasRect,
            new Color(0f, 0f, 0f, 0.58f),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        inventoryPanel = CreatePanel(
            "InventoryPanel",
            inventoryOverlay,
            new Color(0.11f, 0.13f, 0.17f, 0.97f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-280f, -220f),
            new Vector2(280f, 220f)
        );

        CreateText(
            "InventoryTitle",
            inventoryPanel,
            "Inventario",
            28,
            TextAnchor.UpperCenter,
            new Color(0.96f, 0.96f, 0.96f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-220f, -46f),
            new Vector2(220f, -8f)
        );

        CreateText(
            "InventoryHint",
            inventoryPanel,
            "Left click: sposta/scambia   Right click: dividi o piazza 1",
            16,
            TextAnchor.UpperCenter,
            new Color(0.71f, 0.77f, 0.86f, 0.95f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-240f, -76f),
            new Vector2(240f, -44f)
        );

        GameObject gridObject = new GameObject(
            "InventoryGrid",
            typeof(RectTransform),
            typeof(GridLayoutGroup)
        );
        gridObject.transform.SetParent(inventoryPanel, false);

        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.offsetMin = new Vector2(-216f, -142f);
        gridRect.offsetMax = new Vector2(216f, 122f);

        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96f, 96f);
        grid.spacing = new Vector2(16f, 16f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 12; i++)
        {
            InventorySlotView slotView = CreateSlotView(
                gridRect,
                InventorySection.MainInventory,
                i,
                string.Empty
            );
            inventorySlotViews.Add(slotView);
        }

        inventoryOverlay.gameObject.SetActive(false);
    }

    private void BuildHotbar()
    {
        hotbarRoot = CreatePanel(
            "HotbarRoot",
            canvasRect,
            new Color(0f, 0f, 0f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-220f, 26f),
            new Vector2(220f, 156f)
        );
        hotbarRoot.GetComponent<Image>().raycastTarget = false;

        selectedItemText = CreateText(
            "SelectedItem",
            hotbarRoot,
            string.Empty,
            18,
            TextAnchor.MiddleCenter,
            new Color(0.98f, 0.98f, 0.98f, 0.95f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-200f, -30f),
            new Vector2(200f, 0f)
        );

        GameObject rowObject = new GameObject(
            "HotbarRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup)
        );
        rowObject.transform.SetParent(hotbarRoot, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.offsetMin = new Vector2(-208f, 0f);
        rowRect.offsetMax = new Vector2(208f, 96f);

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = false;
        layout.childControlWidth = false;

        for (int i = 0; i < 4; i++)
        {
            InventorySlotView slotView = CreateSlotView(
                rowRect,
                InventorySection.Hotbar,
                i,
                (i + 1).ToString()
            );
            hotbarSlotViews.Add(slotView);
        }
    }

    private void BuildCursorView()
    {
        cursorRoot = CreatePanel(
            "CursorStack",
            canvasRect,
            new Color(0f, 0f, 0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, -96f),
            new Vector2(84f, -12f)
        );
        cursorRoot.GetComponent<Image>().raycastTarget = false;

        CanvasGroup canvasGroup = cursorRoot.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        cursorSlotView = CreateSlotView(
            cursorRoot,
            InventorySection.MainInventory,
            -1,
            string.Empty
        );
        cursorSlotView.SetShortcutVisible(false);
        cursorSlotView.SetRaycastTarget(false);
        cursorRoot.gameObject.SetActive(false);
    }

    private InventorySlotView CreateSlotView(
        Transform parent,
        InventorySection section,
        int index,
        string shortcut
    )
    {
        GameObject slotObject = new GameObject("Slot", typeof(RectTransform));
        slotObject.transform.SetParent(parent, false);

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 96f);
        LayoutElement layoutElement = slotObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 96f;
        layoutElement.preferredHeight = 96f;

        InventorySlotView slotView = slotObject.AddComponent<InventorySlotView>();
        slotView.Initialize(this, section, index, uiFont, shortcut);
        return slotView;
    }

    private RectTransform CreatePanel(
        string name,
        Transform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax
    )
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;

        return rect;
    }

    private Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        TextAnchor alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax
    )
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;

        return text;
    }

    private void EnsureEventSystem()
    {
        EventSystem existingSystem = FindFirstObjectByType<EventSystem>();
        if (existingSystem != null)
        {
            if (existingSystem.GetComponent<InputSystemUIInputModule>() == null)
                existingSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            return;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
        DontDestroyOnLoad(eventSystemObject);
    }

    private Font ResolveFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private void RefreshAll()
    {
        if (!built || inventorySystem == null)
            return;

        RefreshSlots(
            inventorySlotViews,
            inventorySystem.MainSlots,
            InventorySection.MainInventory
        );
        RefreshSlots(hotbarSlotViews, inventorySystem.HotbarSlots, InventorySection.Hotbar);
        HandleInventoryToggled(inventorySystem.IsInventoryOpen);
        HandleHeldItemChanged(inventorySystem.GetSelectedItem());
        UpdateCursorStack();
    }

    private void RefreshSlots(
        List<InventorySlotView> views,
        IReadOnlyList<InventorySlotData> slots,
        InventorySection section
    )
    {
        int count = Mathf.Min(views.Count, slots.Count);
        for (int i = 0; i < count; i++)
        {
            bool selected = section == InventorySection.Hotbar
                && i == inventorySystem.SelectedHotbarIndex;
            views[i].Refresh(slots[i], selected);
        }
    }

    private void HandleInventoryToggled(bool open)
    {
        if (inventoryOverlay != null)
            inventoryOverlay.gameObject.SetActive(open);

        UpdateCursorStack();
    }

    private void HandleHotbarSelectionChanged(int selectedIndex)
    {
        RefreshSlots(hotbarSlotViews, inventorySystem.HotbarSlots, InventorySection.Hotbar);
        HandleHeldItemChanged(inventorySystem.GetSelectedItem());
    }

    private void HandleHeldItemChanged(InventoryItemDefinition item)
    {
        if (selectedItemText == null)
            return;

        selectedItemText.text = item == null ? "Mano vuota" : item.DisplayName;
    }

    private void UpdateCursorStack()
    {
        if (cursorRoot == null || cursorSlotView == null || inventorySystem == null)
            return;

        bool showCursorStack = inventorySystem.IsInventoryOpen && !inventorySystem.CursorSlot.IsEmpty;
        cursorRoot.gameObject.SetActive(showCursorStack);

        if (!showCursorStack)
            return;

        Vector2 mousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mousePosition,
            null,
            out Vector2 localPoint
        );

        cursorRoot.anchoredPosition = localPoint + new Vector2(48f, -48f);
        cursorSlotView.Refresh(inventorySystem.CursorSlot, false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
