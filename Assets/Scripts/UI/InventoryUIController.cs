using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using OurGame.Core;
using OurGame.Systems;
using OurGame.UI;

public class InventoryUIController : SingletonMono<InventoryUIController>
{
    [Header("Scene References")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform inventoryOverlay;
    [SerializeField] private RectTransform hotbarRoot;
    [SerializeField] private RectTransform cursorRoot;
    [SerializeField] private Transform inventoryGridRoot;
    [SerializeField] private Transform hotbarRowRoot;
    [SerializeField] private Text selectedItemText;
    [SerializeField] private InventorySlotView cursorSlotView;
    [SerializeField] private Vector2 cursorDragOffset = Vector2.zero;

    private readonly List<InventorySlotView> inventorySlotViews = new List<InventorySlotView>();
    private readonly List<InventorySlotView> hotbarSlotViews = new List<InventorySlotView>();

    private InventorySystem inventorySystem;
    private bool referencesBound;
    private bool dragInProgress;
    private InventorySection dragOriginSection;
    private int dragOriginIndex = -1;
    private bool hotbarVisible = true;

    void Reset()
    {
        AutoAssignSceneReferences();
    }

    void OnValidate()
    {
        AutoAssignSceneReferences();
    }

    protected override void Awake()
    {
        base.Awake();
        EnsureEventSystem();
        AutoAssignSceneReferences();
        CacheSceneReferences();

        if (Application.isPlaying && inventoryOverlay != null)
            inventoryOverlay.gameObject.SetActive(false);

        if (Application.isPlaying && cursorRoot != null)
            cursorRoot.gameObject.SetActive(false);
    }

    void Start()
    {
        InitializeFromSystem(InventorySystem.Instance);
    }

    void Update()
    {
        if (!referencesBound || inventorySystem == null)
            return;

        UpdateCursorStack();
    }

    public void InitializeFromSystem(InventorySystem system)
    {
        CacheSceneReferences();
        if (!referencesBound)
            return;

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

    public void SetHotbarVisible(bool visible)
    {
        hotbarVisible = visible;

        CacheSceneReferences();
        if (hotbarRoot != null)
            hotbarRoot.gameObject.SetActive(visible);
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
        {
            if (!inventorySystem.CursorSlot.IsEmpty)
                inventorySystem.HandleSlotLeftClick(section, index);
        }
        else if (button == PointerEventData.InputButton.Right)
            inventorySystem.HandleSlotRightClick(section, index);
    }

    public void BeginSlotDrag(InventorySection section, int index, PointerEventData eventData)
    {
        if (inventorySystem == null || !inventorySystem.IsInventoryOpen)
            return;

        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!inventorySystem.CursorSlot.IsEmpty)
            return;

        InventorySlotData slot = GetSlot(section, index);
        if (slot == null || slot.IsEmpty)
            return;

        dragOriginSection = section;
        dragOriginIndex = index;
        dragInProgress = true;

        inventorySystem.HandleSlotLeftClick(section, index);
        UpdateCursorStack();
    }

    public void EndSlotDrag(PointerEventData eventData)
    {
        if (!dragInProgress || inventorySystem == null)
            return;

        InventorySlotView targetSlot = GetTargetSlot(eventData);
        if (targetSlot != null && targetSlot.SlotIndex >= 0)
            inventorySystem.HandleSlotLeftClick(targetSlot.Section, targetSlot.SlotIndex);
        else
            inventorySystem.HandleSlotLeftClick(dragOriginSection, dragOriginIndex);

        dragInProgress = false;
        dragOriginIndex = -1;
        UpdateCursorStack();
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

    private void CacheSceneReferences()
    {
        AutoAssignSceneReferences();

        if (referencesBound)
            return;

        if (cursorSlotView == null && cursorRoot != null)
            cursorSlotView = cursorRoot.GetComponentInChildren<InventorySlotView>(true);

        if (inventoryGridRoot != null && inventorySlotViews.Count == 0)
        {
            InventorySlotView[] views = inventoryGridRoot.GetComponentsInChildren<InventorySlotView>(true);
            ConfigureSlots(views, inventorySlotViews, InventorySection.MainInventory, false);
        }

        if (hotbarRowRoot != null && hotbarSlotViews.Count == 0)
        {
            InventorySlotView[] views = hotbarRowRoot.GetComponentsInChildren<InventorySlotView>(true);
            ConfigureSlots(views, hotbarSlotViews, InventorySection.Hotbar, true);
        }

        if (cursorSlotView != null)
        {
            cursorSlotView.Configure(this, InventorySection.MainInventory, -1, string.Empty);
            cursorSlotView.SetCursorGhostMode(true);
            cursorSlotView.SetShortcutVisible(false);
            cursorSlotView.SetRaycastTarget(false);
        }

        referencesBound = canvasRect != null
            && inventoryOverlay != null
            && hotbarRoot != null
            && cursorRoot != null
            && selectedItemText != null
            && cursorSlotView != null
            && inventorySlotViews.Count > 0
            && hotbarSlotViews.Count > 0;

        if (!referencesBound)
        {
            Debug.LogWarning(
                "InventoryUIController could not bind all scene references. Verify the UI hierarchy in the active scene."
            );
            return;
        }

        cursorRoot.gameObject.SetActive(false);
    }

    private void AutoAssignSceneReferences()
    {
        canvasRect ??= FindRectByPathOrName("InventoryCanvas");
        inventoryOverlay ??= FindRectByPathOrName("InventoryCanvas/InventoryOverlay", "InventoryOverlay");
        hotbarRoot ??= FindRectByPathOrName("InventoryCanvas/HotbarRoot", "HotbarRoot");
        cursorRoot ??= FindRectByPathOrName("InventoryCanvas/CursorStack", "CursorStack");
        inventoryGridRoot ??= FindTransformByPathOrName(
            "InventoryCanvas/InventoryOverlay/InventoryPanel/InventoryGrid",
            "InventoryGrid"
        );
        hotbarRowRoot ??= FindTransformByPathOrName(
            "InventoryCanvas/HotbarRoot/HotbarPanel/HotbarRow",
            "HotbarRow"
        );
        selectedItemText ??= FindTextByPathOrName(
            "InventoryCanvas/HotbarRoot/HotbarPanel/SelectedItem",
            "SelectedItem"
        );
        cursorSlotView ??= FindComponentByPathOrName<InventorySlotView>(
            "InventoryCanvas/CursorStack/Slot",
            "CursorStack"
        );
    }

    private void ConfigureSlots(
        InventorySlotView[] views,
        List<InventorySlotView> target,
        InventorySection section,
        bool showShortcut
    )
    {
        target.Clear();

        for (int i = 0; i < views.Length; i++)
        {
            if (views[i] == null)
                continue;

            views[i].Configure(this, section, i, showShortcut ? (i + 1).ToString() : string.Empty);
            views[i].SetShortcutVisible(showShortcut);
            target.Add(views[i]);
        }
    }

    private RectTransform FindRectByPathOrName(string relativePath, string fallbackName = null)
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found as RectTransform;

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return FindDescendantByName<RectTransform>(transform, fallbackName);

        return null;
    }

    private Transform FindTransformByPathOrName(string relativePath, string fallbackName = null)
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found;

        return !string.IsNullOrWhiteSpace(fallbackName)
            ? FindDescendantByName<Transform>(transform, fallbackName)
            : null;
    }

    private Text FindTextByPathOrName(string relativePath, string fallbackName = null)
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found.GetComponent<Text>();

        return !string.IsNullOrWhiteSpace(fallbackName)
            ? FindDescendantByName<Text>(transform, fallbackName)
            : null;
    }

    private T FindComponentByPathOrName<T>(string relativePath, string fallbackName = null) where T : Component
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found.GetComponent<T>();

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            Transform fallbackRoot = FindDescendantByName<Transform>(transform, fallbackName);
            if (fallbackRoot != null)
                return fallbackRoot.GetComponentInChildren<T>(true);
        }

        return null;
    }

    private T FindDescendantByName<T>(Transform root, string objectName) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
            {
                T component = child.GetComponent<T>();
                if (component != null)
                    return component;
            }

            T nested = FindDescendantByName<T>(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
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

    private void RefreshAll()
    {
        if (!referencesBound || inventorySystem == null)
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

        if (hotbarRoot != null)
            hotbarRoot.gameObject.SetActive(hotbarVisible);
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

        if (open && referencesBound && inventorySystem != null)
        {
            RefreshSlots(
                inventorySlotViews,
                inventorySystem.MainSlots,
                InventorySection.MainInventory
            );
            RefreshSlots(hotbarSlotViews, inventorySystem.HotbarSlots, InventorySection.Hotbar);
            HandleHeldItemChanged(inventorySystem.GetSelectedItem());
            Canvas.ForceUpdateCanvases();
        }

        UpdateCursorStack();
    }

    private void HandleHotbarSelectionChanged(int selectedIndex)
    {
        if (!referencesBound || inventorySystem == null)
            return;

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
        if (canvasRect == null || cursorRoot == null || cursorSlotView == null || inventorySystem == null)
            return;

        bool showCursorStack = inventorySystem.IsInventoryOpen && !inventorySystem.CursorSlot.IsEmpty;
        cursorRoot.gameObject.SetActive(showCursorStack);

        if (!showCursorStack)
            return;

        Vector2 mousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        cursorRoot.position = mousePosition + cursorDragOffset;
        cursorSlotView.Refresh(inventorySystem.CursorSlot, false);
    }

    private InventorySlotData GetSlot(InventorySection section, int index)
    {
        if (inventorySystem == null)
            return null;

        IReadOnlyList<InventorySlotData> slots = section == InventorySection.Hotbar
            ? inventorySystem.HotbarSlots
            : inventorySystem.MainSlots;

        if (index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }

    private InventorySlotView GetTargetSlot(PointerEventData eventData)
    {
        if (eventData == null)
            return null;

        GameObject raycastObject = eventData.pointerCurrentRaycast.gameObject;
        if (raycastObject == null)
            raycastObject = eventData.pointerEnter;

        return raycastObject != null
            ? raycastObject.GetComponentInParent<InventorySlotView>()
            : null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
