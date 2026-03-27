using System;
using System.Collections.Generic;
using OurGame.Core;
using OurGame.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class PocketInventoryUIController : SingletonMono<PocketInventoryUIController>
{
    [Serializable]
    private class PocketPanelBinding
    {
        public string pocketName;
        public RectTransform panelRoot;
        public Text selectedItemText;
        [NonSerialized] public readonly List<InventorySlotView> slotViews = new List<InventorySlotView>();
    }

    private const string HotbarBindingName = "__Hotbar__";

    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private RectTransform inventoryOverlay;
    [SerializeField] private RectTransform hotbarRoot;
    [SerializeField] private RectTransform cursorStackRoot;
    [SerializeField] private Transform hotbarRowRoot;
    [SerializeField] private Text hotbarSelectedItemText;
    [SerializeField] private List<PocketPanelBinding> pocketPanels = new List<PocketPanelBinding>();

    private readonly Dictionary<string, PocketPanelBinding> panelByPocket =
        new Dictionary<string, PocketPanelBinding>();
    private readonly List<InventorySlotView> hotbarSlotViews = new List<InventorySlotView>();

    private BackpackInventorySystem inventorySystem;
    private InventorySlotView cursorSlotView;
    private bool referencesBound;
    private bool isDragging;
    private string dragSourceContainerName;
    private int dragSourceIndex = -1;
    private string pendingDropContainerName;
    private int pendingDropIndex = -1;

    void OnValidate()
    {
        referencesBound = false;
        AutoAssignReferences();
#if UNITY_EDITOR
        EnsurePocketPanelsExistInEditor();
#endif
        CacheReferences();
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        EnsurePocketPanelsExistInEditor();
#endif
    }

    void OnDisable()
    {
        if (Application.isPlaying)
            Debug.LogWarning("PocketInventoryUIController was disabled while the game is running. Pocket inventory UI will stop responding.");
    }

    protected override void Awake()
    {
        base.Awake();
        AutoAssignReferences();
        CacheReferences();

        if (!Application.isPlaying)
            return;

        EnsureEventSystem();
        HideAllUIs();
    }

    void Start()
    {
        if (!Application.isPlaying)
            return;

        InitializeFromSystem(BackpackInventorySystem.Instance);
    }

    void Update()
    {
        if (!isDragging)
            return;

        Vector2 pointerPosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        UpdateDragPosition(pointerPosition);
    }

    public void InitializeFromSystem(BackpackInventorySystem system)
    {
        CacheReferences();

        if (system == null)
        {
            Debug.LogWarning("PocketInventoryUIController could not initialize because BackpackInventorySystem is missing.");
            return;
        }

        if (!referencesBound)
        {
            Debug.LogWarning(
                "PocketInventoryUIController could not bind all scene references. Verify InventoryOverlay, HotbarRoot, CursorStackRoot and pocket panels in the active scene."
            );
        }

        Unsubscribe();
        inventorySystem = system;
        Subscribe();
        HideAllUIs();
        RefreshHotbar();
        RefreshVisiblePocket();
    }

    public void ShowPocketUI(string pocketName)
    {
        CacheReferences();
        if (!referencesBound)
        {
            Debug.LogWarning($"PocketInventoryUIController cannot show pocket '{pocketName}' because required scene references are missing.");
            return;
        }

        HideAllPanels();
        RefreshHotbar();

        if (!panelByPocket.TryGetValue(pocketName, out PocketPanelBinding panel) || panel.panelRoot == null)
        {
            Debug.LogWarning($"PocketInventoryUIController cannot find UI panel for pocket '{pocketName}'.");
            return;
        }

        if (inventoryOverlay != null)
            inventoryOverlay.gameObject.SetActive(true);

        panel.panelRoot.gameObject.SetActive(true);
        RefreshPanel(pocketName);
    }

    public void HideAllUIs()
    {
        HideAllPanels();
        ResetDragState();

        if (inventoryOverlay != null)
            inventoryOverlay.gameObject.SetActive(false);

        RefreshHotbar();
    }

    public void HandleSlotClick(string pocketName, int slotIndex)
    {
        if (inventorySystem == null)
            return;

        if (pocketName == HotbarBindingName)
        {
            inventorySystem.SelectHotbarSlot(slotIndex);
            return;
        }
    }

    public void BeginSlotDrag(string containerName, int slotIndex, Vector2 pointerPosition)
    {
        if (inventorySystem == null || !inventorySystem.IsInventoryOpen || isDragging)
            return;

        InventorySlotData slot = inventorySystem.GetSlot(containerName, slotIndex);
        if (slot == null || slot.IsEmpty)
            return;

        EnsureCursorSlotView();
        if (cursorSlotView == null || cursorStackRoot == null)
            return;

        dragSourceContainerName = containerName;
        dragSourceIndex = slotIndex;
        pendingDropContainerName = null;
        pendingDropIndex = -1;
        isDragging = true;

        cursorStackRoot.gameObject.SetActive(true);
        cursorStackRoot.SetAsLastSibling();
        cursorSlotView.Refresh(slot, false);
        cursorSlotView.SetCursorGhostMode(true);
        cursorSlotView.SetRaycastTarget(false);
        cursorSlotView.SetShortcutVisible(false);
        RefreshVisiblePocket();
        RefreshHotbar();
        UpdateDragPosition(pointerPosition);
    }

    public void UpdateDragPosition(Vector2 pointerPosition)
    {
        if (!isDragging || cursorStackRoot == null || canvasRoot == null)
            return;

        cursorStackRoot.position = pointerPosition;
    }

    public void RegisterDropTarget(string containerName, int slotIndex)
    {
        if (!isDragging)
            return;

        pendingDropContainerName = containerName;
        pendingDropIndex = slotIndex;
    }

    public void EndSlotDrag()
    {
        if (!isDragging)
            return;

        bool moved = !string.IsNullOrWhiteSpace(pendingDropContainerName) &&
                     inventorySystem != null &&
                     inventorySystem.TryMoveSlot(
                         dragSourceContainerName,
                         dragSourceIndex,
                         pendingDropContainerName,
                         pendingDropIndex
                     );

        ResetDragState();
        RefreshVisiblePocket();
        RefreshHotbar();
    }

    public void HandleOverlayBackgroundClick()
    {
        if (inventorySystem == null || !inventorySystem.IsInventoryOpen)
            return;

        inventorySystem.CloseInventory();
    }

    private void Subscribe()
    {
        if (inventorySystem == null)
            return;

        inventorySystem.OnPocketOpened += HandlePocketOpened;
        inventorySystem.OnPocketClosed += HandlePocketClosed;
        inventorySystem.OnInventoryChanged += HandleInventoryChanged;
    }

    private void Unsubscribe()
    {
        if (inventorySystem == null)
            return;

        inventorySystem.OnPocketOpened -= HandlePocketOpened;
        inventorySystem.OnPocketClosed -= HandlePocketClosed;
        inventorySystem.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandlePocketOpened(string pocketName)
    {
        ShowPocketUI(pocketName);
    }

    private void HandlePocketClosed()
    {
        HideAllUIs();
    }

    private void HandleInventoryChanged(string pocketName)
    {
        if (string.IsNullOrWhiteSpace(pocketName))
        {
            RefreshHotbar();
            RefreshVisiblePocket();
            return;
        }

        if (pocketName == HotbarBindingName)
        {
            RefreshHotbar();
            return;
        }

        RefreshPanel(pocketName);
    }

    private void RefreshVisiblePocket()
    {
        if (inventorySystem == null || !inventorySystem.IsInventoryOpen)
        {
            HideAllUIs();
            return;
        }

        ShowPocketUI(inventorySystem.CurrentOpenPocket);
    }

    private void RefreshPanel(string pocketName)
    {
        if (inventorySystem == null)
            return;

        if (!panelByPocket.TryGetValue(pocketName, out PocketPanelBinding panel))
            return;

        Pocket pocket = inventorySystem.GetPocket(pocketName);
        if (pocket == null)
            return;

        for (int i = 0; i < panel.slotViews.Count; i++)
        {
            InventorySlotData slot = i < pocket.Slots.Count ? pocket.Slots[i] : null;
            if (IsDraggedSourceSlot(pocketName, i))
                slot = null;
            panel.slotViews[i].Refresh(slot, false);
        }

        if (panel.selectedItemText != null)
            panel.selectedItemText.text = string.Empty;

        RefreshHotbar();
    }

    private void CacheReferences()
    {
        AutoAssignReferences();
        panelByPocket.Clear();
        EnsureOverlayDismissRegion();
        EnsureCursorSlotView();

        for (int i = 0; i < pocketPanels.Count; i++)
        {
            PocketPanelBinding binding = pocketPanels[i];
            if (binding == null || string.IsNullOrWhiteSpace(binding.pocketName) || binding.panelRoot == null)
                continue;

            binding.slotViews.Clear();
            InventorySlotView[] slotViews = binding.panelRoot.GetComponentsInChildren<InventorySlotView>(true);
            for (int slotIndex = 0; slotIndex < slotViews.Length; slotIndex++)
            {
                slotViews[slotIndex].Configure(this, binding.pocketName, slotIndex);
                slotViews[slotIndex].SetShortcutVisible(false);
                binding.slotViews.Add(slotViews[slotIndex]);
            }

            panelByPocket[binding.pocketName] = binding;
        }

        hotbarSlotViews.Clear();
        if (hotbarRowRoot != null)
        {
            InventorySlotView[] slotViews = hotbarRowRoot.GetComponentsInChildren<InventorySlotView>(true);
            for (int slotIndex = 0; slotIndex < slotViews.Length; slotIndex++)
            {
                slotViews[slotIndex].Configure(this, HotbarBindingName, slotIndex);
                slotViews[slotIndex].SetShortcutVisible(true);
                slotViews[slotIndex].SetShortcutLabel((slotIndex + 1).ToString());
                hotbarSlotViews.Add(slotViews[slotIndex]);
            }
        }

        if (cursorSlotView != null)
        {
            cursorSlotView.Configure(this, HotbarBindingName, -1);
            cursorSlotView.SetCursorGhostMode(true);
            cursorSlotView.SetRaycastTarget(false);
            cursorSlotView.SetShortcutVisible(false);
        }

        referencesBound = canvasRoot != null && inventoryOverlay != null;
    }

    private void AutoAssignReferences()
    {
        canvasRoot ??= FindRectByName("InventoryCanvas");
        inventoryOverlay ??= FindRectByName("InventoryOverlay");
        hotbarRoot ??= FindRectByName("HotbarRoot");
        cursorStackRoot ??= FindRectByName("CursorStack");
        hotbarRowRoot ??= FindTransformByName("HotbarRow");
        hotbarSelectedItemText ??= FindTextByName(hotbarRoot, "SelectedItem");

        if (pocketPanels.Count == 0)
        {
            pocketPanels.Add(CreateBinding(PocketNames.LeftPocket, "InventoryLeftPocket"));
            pocketPanels.Add(CreateBinding(PocketNames.RightPocket, "InventoryRightPocket"));
            pocketPanels.Add(CreateBinding(PocketNames.CentralPocket, "InventoryCentralPocket"));
            pocketPanels.Add(CreateBinding(PocketNames.UpperPocket, "InventoryUpperPocket"));
            pocketPanels.Add(CreateBinding(PocketNames.BottomPocket, "InventoryBottomPocket"));
        }

        for (int i = 0; i < pocketPanels.Count; i++)
        {
            PocketPanelBinding binding = pocketPanels[i];
            if (binding == null || binding.panelRoot != null)
                continue;

            binding.panelRoot = FindRectByName($"Inventory{binding.pocketName}");
            if (binding.selectedItemText == null && binding.panelRoot != null)
                binding.selectedItemText = FindTextByName(binding.panelRoot, "SelectedItem");
        }
    }

    private string GetFriendlyPocketLabel(string pocketName)
    {
        return pocketName switch
        {
            PocketNames.LeftPocket => "Left pocket",
            PocketNames.RightPocket => "Right pocket",
            PocketNames.CentralPocket => "Central pocket",
            PocketNames.UpperPocket => "Upper pocket",
            PocketNames.BottomPocket => "Bottom pocket",
            _ => pocketName
        };
    }

    private int GetPocketSlotCount(string pocketName)
    {
        return pocketName switch
        {
            PocketNames.LeftPocket => 2,
            PocketNames.RightPocket => 2,
            PocketNames.CentralPocket => 9,
            PocketNames.UpperPocket => 4,
            PocketNames.BottomPocket => 4,
            _ => 4
        };
    }

    private int GetGridConstraintCount(string pocketName)
    {
        return pocketName switch
        {
            PocketNames.CentralPocket => 3,
            PocketNames.UpperPocket => 2,
            PocketNames.BottomPocket => 2,
            _ => 1
        };
    }

    private PocketPanelBinding CreateBinding(string pocketName, string panelObjectName)
    {
        return new PocketPanelBinding
        {
            pocketName = pocketName,
            panelRoot = FindRectByName(panelObjectName)
        };
    }

    private void HideAllPanels()
    {
        for (int i = 0; i < pocketPanels.Count; i++)
        {
            if (pocketPanels[i]?.panelRoot != null)
                pocketPanels[i].panelRoot.gameObject.SetActive(false);
        }
    }

    private RectTransform FindRectByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
                return transforms[i] as RectTransform;
        }

        return null;
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    private Text FindTextByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child.GetComponent<Text>();

            Text nested = FindTextByName(child, objectName);
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

    private void RefreshHotbar()
    {
        if (inventorySystem == null || hotbarSlotViews.Count == 0)
            return;

        IReadOnlyList<InventorySlotData> hotbarSlots = inventorySystem.GetHotbarSlots();
        int selectedSlotIndex = inventorySystem.GetSelectedSlotIndex(HotbarBindingName);
        for (int i = 0; i < hotbarSlotViews.Count; i++)
        {
            InventorySlotData slot = i < hotbarSlots.Count ? hotbarSlots[i] : null;
            if (IsDraggedSourceSlot(HotbarBindingName, i))
                slot = null;
            hotbarSlotViews[i].Refresh(slot, i == selectedSlotIndex);
        }

        if (hotbarSelectedItemText != null)
        {
            InventoryItemDefinition selectedItem = inventorySystem.GetSelectedItem();
            hotbarSelectedItemText.text = selectedItem == null
                ? "Mano vuota"
                : selectedItem.DisplayName;
        }

        if (hotbarRoot != null)
            hotbarRoot.gameObject.SetActive(true);
    }

    private void EnsureOverlayDismissRegion()
    {
        if (inventoryOverlay == null)
            return;

        if (inventoryOverlay.GetComponent<InventoryOverlayDismissRegion>() != null)
            return;

        inventoryOverlay.gameObject.AddComponent<InventoryOverlayDismissRegion>();
    }

    private void EnsureCursorSlotView()
    {
        if (cursorSlotView != null || cursorStackRoot == null)
            return;

        cursorSlotView = cursorStackRoot.GetComponentInChildren<InventorySlotView>(true);
    }

    private void ResetDragState()
    {
        isDragging = false;
        dragSourceContainerName = null;
        dragSourceIndex = -1;
        pendingDropContainerName = null;
        pendingDropIndex = -1;

        if (cursorStackRoot != null)
            cursorStackRoot.gameObject.SetActive(false);
    }

    private bool IsDraggedSourceSlot(string containerName, int slotIndex)
    {
        return isDragging &&
               dragSourceIndex == slotIndex &&
               string.Equals(dragSourceContainerName, containerName, StringComparison.Ordinal);
    }

#if UNITY_EDITOR
    private void EnsurePocketPanelsExistInEditor()
    {
        if (Application.isPlaying)
            return;

        inventoryOverlay ??= FindRectByName("InventoryOverlay");
        if (inventoryOverlay == null)
            return;

        RectTransform templatePanel = FindRectByName("InventoryLeftPocket") ?? FindRectByName("InventoryRightPocket");
        if (templatePanel == null)
            return;

        bool changed = false;
        changed |= EnsurePocketPanel(templatePanel, PocketNames.LeftPocket);
        changed |= EnsurePocketPanel(templatePanel, PocketNames.RightPocket);
        changed |= EnsurePocketPanel(templatePanel, PocketNames.CentralPocket);
        changed |= EnsurePocketPanel(templatePanel, PocketNames.UpperPocket);
        changed |= EnsurePocketPanel(templatePanel, PocketNames.BottomPocket);

        if (!changed)
            return;

        EditorUtility.SetDirty(gameObject);
        if (inventoryOverlay != null)
            EditorUtility.SetDirty(inventoryOverlay.gameObject);

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private bool EnsurePocketPanel(RectTransform templatePanel, string pocketName)
    {
        string panelName = $"Inventory{pocketName}";
        RectTransform panelRoot = FindRectByName(panelName);
        bool created = false;

        if (panelRoot == null)
        {
            GameObject panelObject = Instantiate(templatePanel.gameObject, inventoryOverlay);
            panelObject.name = panelName;
            panelRoot = panelObject.GetComponent<RectTransform>();
            panelRoot.SetAsLastSibling();
            created = true;
        }

        if (panelRoot == null)
            return created;

        return UpdatePanelPresentation(panelRoot, pocketName) || created;
    }

    private bool UpdatePanelPresentation(RectTransform panelRoot, string pocketName)
    {
        bool changed = false;
        Text title = FindTextByName(panelRoot, "InventoryTitle");
        string friendlyLabel = GetFriendlyPocketLabel(pocketName);
        if (title != null && title.text != friendlyLabel)
        {
            title.text = GetFriendlyPocketLabel(pocketName);
            changed = true;
        }

        RectTransform gridRoot = FindGridRoot(panelRoot);
        if (gridRoot == null)
            return changed;

        GridLayoutGroup gridLayout = gridRoot.GetComponent<GridLayoutGroup>();
        int constraintCount = GetGridConstraintCount(pocketName);
        if (gridLayout != null && gridLayout.constraintCount != constraintCount)
        {
            gridLayout.constraintCount = GetGridConstraintCount(pocketName);
            changed = true;
        }

        InventorySlotView templateSlot = gridRoot.GetComponentInChildren<InventorySlotView>(true);
        if (templateSlot == null)
            return changed;

        int desiredSlotCount = GetPocketSlotCount(pocketName);
        return SyncGridSlotCount(gridRoot, templateSlot.gameObject, desiredSlotCount) || changed;
    }

    private RectTransform FindGridRoot(RectTransform panelRoot)
    {
        for (int i = 0; i < panelRoot.childCount; i++)
        {
            Transform child = panelRoot.GetChild(i);
            if (child.name == "InventoryGrid")
                return child as RectTransform;
        }

        return null;
    }

    private bool SyncGridSlotCount(RectTransform gridRoot, GameObject templateSlot, int desiredSlotCount)
    {
        bool changed = false;
        List<Transform> slotChildren = new List<Transform>();
        for (int i = 0; i < gridRoot.childCount; i++)
        {
            Transform child = gridRoot.GetChild(i);
            if (child.GetComponent<InventorySlotView>() != null)
                slotChildren.Add(child);
        }

        while (slotChildren.Count > desiredSlotCount)
        {
            Transform child = slotChildren[slotChildren.Count - 1];
            slotChildren.RemoveAt(slotChildren.Count - 1);
            DestroyImmediate(child.gameObject);
            changed = true;
        }

        while (slotChildren.Count < desiredSlotCount)
        {
            GameObject slotObject = Instantiate(templateSlot, gridRoot);
            slotObject.name = "Slot";
            slotChildren.Add(slotObject.transform);
            changed = true;
        }

        return changed;
    }
#endif

    void OnDestroy()
    {
        Unsubscribe();
    }
}
