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
    private const string InventoryCanvasTag = "InventoryCanvas";
    private const string InventoryOverlayTag = "InventoryOverlay";
    private const string CentralPocketTag = "CentralPocketUI";
    private const string BottomPocketTag = "BottomPocketUI";
    private const string UpperPocketTag = "UpperPocketUI";
    private const string LeftPocketTag = "LeftPocketUI";
    private const string RightPocketTag = "RightPocketUI";

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
    [SerializeField] private float pocketUiRevealDelay = 0.18f;
    [SerializeField] private List<PocketPanelBinding> pocketPanels = new List<PocketPanelBinding>();

    private readonly Dictionary<string, PocketPanelBinding> panelByPocket =
        new Dictionary<string, PocketPanelBinding>();
    private readonly List<InventorySlotView> hotbarSlotViews = new List<InventorySlotView>();

    private BackpackInventorySystem inventorySystem;
    private InventorySlotView cursorSlotView;
    private bool referencesBound;
    private bool panelsPrepared;
    private bool isDragging;
    private string dragSourceContainerName;
    private int dragSourceIndex = -1;
    private string pendingDropContainerName;
    private int pendingDropIndex = -1;
    private Coroutine pendingPocketShowRoutine;

    void OnValidate()
    {
        referencesBound = false;
        panelsPrepared = false;
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

        PreparePocketPanels();
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
        panelsPrepared = false;
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
        PreparePocketPanels(true);
        HideAllUIs();
        RefreshHotbar();
        RefreshVisiblePocket();
    }

    public void ShowPocketUI(string pocketName)
    {
        CacheReferences();
        PreparePocketPanels();
        if (!referencesBound)
        {
            Debug.LogWarning($"PocketInventoryUIController cannot show pocket '{pocketName}' because required scene references are missing.");
            return;
        }

        HideAllPanels();
        RefreshHotbar();
        EnsureBindingPanelResolved(pocketName);

        if (!panelByPocket.TryGetValue(pocketName, out PocketPanelBinding panel) || panel.panelRoot == null)
        {
            Debug.LogWarning($"PocketInventoryUIController cannot find UI panel for pocket '{pocketName}'.");
            return;
        }

        AttachCanvasToPocketAnchor(pocketName);

        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(true);

        if (inventoryOverlay != null)
            inventoryOverlay.gameObject.SetActive(true);

        panel.panelRoot.gameObject.SetActive(true);
        RefreshPanel(pocketName);
    }

    public void HideAllUIs()
    {
        CancelPendingPocketReveal();
        HideAllPanels();
        ResetDragState();

        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(false);

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
        if (!Application.isPlaying || pocketUiRevealDelay <= 0f)
        {
            ShowPocketUI(pocketName);
            return;
        }

        SchedulePocketReveal(pocketName);
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

    private void PreparePocketPanels(bool force = false)
    {
        if (!force && !NeedsPanelPreparation())
            return;

        SyncPocketBindingsWithDefinitions();
        panelByPocket.Clear();

        for (int i = 0; i < pocketPanels.Count; i++)
        {
            PocketPanelBinding binding = pocketPanels[i];
            if (binding == null || string.IsNullOrWhiteSpace(binding.pocketName))
                continue;

            binding.panelRoot = ResolvePocketPanelRoot(binding.pocketName);
            if (binding.panelRoot == null)
                continue;

            EnsurePanelMatchesPocket(binding.panelRoot, binding.pocketName);
            binding.selectedItemText ??= FindTextByName(binding.panelRoot, "SelectedItem");
            binding.slotViews.Clear();

            InventorySlotView[] slotViews = binding.panelRoot.GetComponentsInChildren<InventorySlotView>(true);
            for (int slotIndex = 0; slotIndex < slotViews.Length; slotIndex++)
            {
                slotViews[slotIndex].Configure(this, binding.pocketName, slotIndex);
                slotViews[slotIndex].SetShortcutVisible(false);
                binding.slotViews.Add(slotViews[slotIndex]);
            }

            panelByPocket[binding.pocketName] = binding;
            binding.panelRoot.gameObject.SetActive(false);
        }

        panelsPrepared = true;
    }

    private bool NeedsPanelPreparation()
    {
        if (!panelsPrepared)
            return true;

        int validBindingCount = 0;
        for (int i = 0; i < pocketPanels.Count; i++)
        {
            PocketPanelBinding binding = pocketPanels[i];
            if (binding == null || string.IsNullOrWhiteSpace(binding.pocketName))
                continue;

            validBindingCount++;
            if (binding.panelRoot == null)
                return true;

            if (!panelByPocket.TryGetValue(binding.pocketName, out PocketPanelBinding mappedBinding))
                return true;

            if (mappedBinding == null || mappedBinding.panelRoot == null)
                return true;

            if (mappedBinding.slotViews.Count == 0)
                return true;
        }

        return panelByPocket.Count != validBindingCount;
    }

    private void AutoAssignReferences()
    {
        canvasRoot ??= FindRectByTag(InventoryCanvasTag);
        canvasRoot ??= FindRectByName("InventoryCanvas");
        inventoryOverlay ??= FindRectByTag(InventoryOverlayTag);
        inventoryOverlay ??= FindRectByName("InventoryOverlay");
        hotbarRoot ??= FindRectByName("HotbarRoot");
        cursorStackRoot ??= FindRectByName("CursorStack");
        hotbarRowRoot ??= FindTransformByName("HotbarRow");
        hotbarSelectedItemText ??= FindTextByName(hotbarRoot, "SelectedItem");
        SyncPocketBindingsWithDefinitions();
    }

    private string GetFriendlyPocketLabel(string pocketName)
    {
        BackpackPocketDefinition definition = GetPocketDefinition(pocketName);
        return definition != null ? definition.DisplayName : pocketName;
    }

    private int GetPocketSlotCount(string pocketName)
    {
        BackpackPocketDefinition definition = GetPocketDefinition(pocketName);
        return definition != null ? definition.Capacity : 4;
    }

    private int GetGridConstraintCount(string pocketName)
    {
        BackpackPocketDefinition definition = GetPocketDefinition(pocketName);
        return definition != null ? definition.Columns : 1;
    }

    private PocketPanelBinding CreateBinding(string pocketName)
    {
        return new PocketPanelBinding
        {
            pocketName = pocketName,
            panelRoot = ResolvePocketPanelRoot(pocketName)
        };
    }

    private void AttachCanvasToPocketAnchor(string pocketName)
    {
        if (canvasRoot == null || string.IsNullOrWhiteSpace(pocketName))
            return;

        Transform pocketAnchor = ResolvePocketAnchor(pocketName);
        if (pocketAnchor == null)
        {
            Debug.LogWarning(
                $"PocketInventoryUIController could not find UI anchor '{GetPocketAnchorName(pocketName)}' for pocket '{pocketName}'."
            );
            return;
        }

        if (canvasRoot.parent != pocketAnchor)
            canvasRoot.SetParent(pocketAnchor, false);

        canvasRoot.localPosition = Vector3.zero;
        canvasRoot.localRotation = Quaternion.identity;

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            EnsureCanvasWorldCamera(canvas);
        }
    }

    private void EnsureCanvasWorldCamera(Canvas canvas)
    {
        if (canvas == null)
            return;

        if (canvas.worldCamera != null && canvas.worldCamera.isActiveAndEnabled)
            return;

        Camera resolvedCamera = ResolveInventoryUICamera();
        if (resolvedCamera != null)
            canvas.worldCamera = resolvedCamera;
    }

    private Camera ResolveInventoryUICamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null || !candidate.isActiveAndEnabled)
                continue;

            return candidate;
        }

        return null;
    }

    private Transform ResolvePocketAnchor(string pocketName)
    {
        return FindTransformByName(GetPocketAnchorName(pocketName));
    }

    private string GetPocketAnchorName(string pocketName)
    {
        return string.IsNullOrWhiteSpace(pocketName)
            ? string.Empty
            : $"{pocketName}UIAnchor";
    }

    private void HideAllPanels()
    {
        if (inventoryOverlay != null)
        {
            for (int i = 0; i < inventoryOverlay.childCount; i++)
            {
                Transform child = inventoryOverlay.GetChild(i);
                if (child.name.StartsWith("Inventory") && child.name.EndsWith("Pocket"))
                    child.gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < pocketPanels.Count; i++)
        {
            if (pocketPanels[i]?.panelRoot != null)
                pocketPanels[i].panelRoot.gameObject.SetActive(false);
        }

        HideUnusedPocketPanels();
    }

    private RectTransform FindRectByName(string objectName)
    {
        RectTransform localMatch =
            FindDescendantByName(inventoryOverlay, objectName) as RectTransform ??
            FindDescendantByName(canvasRoot, objectName) as RectTransform;
        if (localMatch != null)
            return localMatch;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == null || !transforms[i].gameObject.scene.IsValid())
                continue;

            if (transforms[i].name == objectName)
                return transforms[i] as RectTransform;
        }

        return null;
    }

    private RectTransform FindRectByTag(string tagName)
    {
        RectTransform localMatch =
            FindDescendantByTag(inventoryOverlay, tagName) as RectTransform ??
            FindDescendantByTag(canvasRoot, tagName) as RectTransform;
        if (localMatch != null)
            return localMatch;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;

            if (candidate.gameObject.tag == tagName)
                return candidate as RectTransform;
        }

        return null;
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform localMatch =
            FindDescendantByName(inventoryOverlay, objectName) ??
            FindDescendantByName(canvasRoot, objectName);
        if (localMatch != null)
            return localMatch;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == null || !transforms[i].gameObject.scene.IsValid())
                continue;

            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    private Transform FindDescendantByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform nested = FindDescendantByName(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private Transform FindDescendantByTag(Transform root, string tagName)
    {
        if (root == null || string.IsNullOrWhiteSpace(tagName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.gameObject.tag == tagName)
                return child;

            Transform nested = FindDescendantByTag(child, tagName);
            if (nested != null)
                return nested;
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

    private void SchedulePocketReveal(string pocketName)
    {
        CancelPendingPocketReveal();
        pendingPocketShowRoutine = StartCoroutine(DelayedShowPocketUI(pocketName));
    }

    private System.Collections.IEnumerator DelayedShowPocketUI(string pocketName)
    {
        yield return new WaitForSeconds(pocketUiRevealDelay);
        pendingPocketShowRoutine = null;

        if (inventorySystem == null || !inventorySystem.IsInventoryOpen)
            yield break;

        if (!string.Equals(inventorySystem.CurrentOpenPocket, pocketName, StringComparison.Ordinal))
            yield break;

        ShowPocketUI(pocketName);
    }

    private void CancelPendingPocketReveal()
    {
        if (pendingPocketShowRoutine == null)
            return;

        StopCoroutine(pendingPocketShowRoutine);
        pendingPocketShowRoutine = null;
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

    private BackpackPocketDefinition GetPocketDefinition(string pocketName)
    {
        List<BackpackPocketDefinition> definitions = GetEffectivePocketDefinitions();
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i].PocketName == pocketName)
                return definitions[i];
        }

        return null;
    }

    private List<BackpackPocketDefinition> GetEffectivePocketDefinitions()
    {
        if (inventorySystem != null && inventorySystem.PocketDefinitions.Count > 0)
            return new List<BackpackPocketDefinition>(inventorySystem.PocketDefinitions);

        BackpackDefinition backpackDefinition = FindFirstObjectByType<BackpackDefinition>();
        if (backpackDefinition != null)
            return backpackDefinition.GetDefinitionsSnapshot();

        List<BackpackPocketDefinition> fallback = new List<BackpackPocketDefinition>();
        for (int i = 0; i < PocketNames.Ordered.Length; i++)
            fallback.Add(BackpackPocketDefinition.CreateDefault(PocketNames.Ordered[i]));

        return fallback;
    }

    private void SyncPocketBindingsWithDefinitions()
    {
        List<BackpackPocketDefinition> definitions = GetEffectivePocketDefinitions();
        List<PocketPanelBinding> bindings = new List<PocketPanelBinding>();

        for (int i = 0; i < definitions.Count; i++)
        {
            BackpackPocketDefinition definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.PocketName))
                continue;

            PocketPanelBinding binding = pocketPanels.Find(existing =>
                existing != null && existing.pocketName == definition.PocketName
            );

            if (binding == null)
                binding = CreateBinding(definition.PocketName);

            binding.panelRoot = ResolvePocketPanelRoot(definition.PocketName);
            if (binding.selectedItemText == null && binding.panelRoot != null)
                binding.selectedItemText = FindTextByName(binding.panelRoot, "SelectedItem");

            bindings.Add(binding);
        }

        pocketPanels = bindings;
    }

    private void HideUnusedPocketPanels()
    {
        if (inventoryOverlay == null)
            return;

        HashSet<string> validNames = new HashSet<string>();
        for (int i = 0; i < pocketPanels.Count; i++)
        {
            if (pocketPanels[i] != null)
                validNames.Add($"Inventory{pocketPanels[i].pocketName}");
        }

        for (int i = 0; i < inventoryOverlay.childCount; i++)
        {
            Transform child = inventoryOverlay.GetChild(i);
            if (!child.name.StartsWith("Inventory") || !child.name.EndsWith("Pocket"))
                continue;

            if (!validNames.Contains(child.name))
                child.gameObject.SetActive(false);
        }
    }

    private void EnsureBindingPanelResolved(string pocketName)
    {
        for (int i = 0; i < pocketPanels.Count; i++)
        {
            PocketPanelBinding binding = pocketPanels[i];
            if (binding == null || binding.pocketName != pocketName)
                continue;

            binding.panelRoot = ResolvePocketPanelRoot(pocketName);

            if (binding.selectedItemText == null && binding.panelRoot != null)
                binding.selectedItemText = FindTextByName(binding.panelRoot, "SelectedItem");

            if (binding.panelRoot != null)
            {
                EnsurePanelMatchesPocket(binding.panelRoot, binding.pocketName);
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

            return;
        }
    }

    private RectTransform ResolvePocketPanelRoot(string pocketName)
    {
        string pocketPanelTag = GetPocketPanelTag(pocketName);
        if (string.IsNullOrWhiteSpace(pocketPanelTag))
            return null;

        return FindRectByTag(pocketPanelTag);
    }

    private string GetPocketPanelTag(string pocketName)
    {
        return pocketName switch
        {
            PocketNames.CentralPocket => CentralPocketTag,
            PocketNames.LeftPocket => LeftPocketTag,
            PocketNames.RightPocket => RightPocketTag,
            PocketNames.UpperPocket => UpperPocketTag,
            // Il pocket runtime si chiama BottomPocket, mentre il pannello UI usa il tag BottomPocketUI.
            PocketNames.BottomPocket => BottomPocketTag,
            _ => string.Empty
        };
    }

    private bool EnsurePanelMatchesPocket(RectTransform panelRoot, string pocketName)
    {
        if (panelRoot == null)
            return false;

        bool changed = false;
        Text title = FindTextByName(panelRoot, "InventoryTitle");
        string friendlyLabel = GetFriendlyPocketLabel(pocketName);
        if (title != null && title.text != friendlyLabel)
        {
            title.text = friendlyLabel;
            changed = true;
        }

        RectTransform gridRoot = FindGridRoot(panelRoot);
        if (gridRoot == null)
            return changed;

        GridLayoutGroup gridLayout = gridRoot.GetComponent<GridLayoutGroup>();
        int constraintCount = Mathf.Max(1, GetGridConstraintCount(pocketName));
        if (gridLayout != null)
        {
            if (gridLayout.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                changed = true;
            }

            if (gridLayout.constraintCount != constraintCount)
            {
                gridLayout.constraintCount = constraintCount;
                changed = true;
            }
        }

        InventorySlotView templateSlot = gridRoot.GetComponentInChildren<InventorySlotView>(true);
        if (templateSlot == null)
            return changed;

        int desiredSlotCount = Mathf.Max(1, GetPocketSlotCount(pocketName));
        return SyncGridSlotCountRuntimeAware(gridRoot, templateSlot.gameObject, desiredSlotCount) || changed;
    }

    private RectTransform FindGridRoot(RectTransform panelRoot)
    {
        return FindDescendantByName(panelRoot, "InventoryGrid") as RectTransform;
    }

    private bool SyncGridSlotCountRuntimeAware(RectTransform gridRoot, GameObject templateSlot, int desiredSlotCount)
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

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
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

#if UNITY_EDITOR
    private void EnsurePocketPanelsExistInEditor()
    {
        if (Application.isPlaying)
            return;

        inventoryOverlay ??= FindRectByName("InventoryOverlay");
        if (inventoryOverlay == null)
            return;

        bool changed = false;
        List<BackpackPocketDefinition> definitions = GetEffectivePocketDefinitions();
        for (int i = 0; i < definitions.Count; i++)
        {
            RectTransform panelRoot = ResolvePocketPanelRoot(definitions[i].PocketName);
            if (panelRoot != null)
                changed |= EnsurePanelMatchesPocket(panelRoot, definitions[i].PocketName);
        }

        if (!changed)
            return;

        EditorUtility.SetDirty(gameObject);
        if (inventoryOverlay != null)
            EditorUtility.SetDirty(inventoryOverlay.gameObject);

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

#endif

    void OnDestroy()
    {
        CancelPendingPocketReveal();
        Unsubscribe();
    }
}
