using System;
using System.Collections.Generic;
using OurGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OurGame.Systems
{
    [DisallowMultipleComponent]
    public class BackpackInventorySystem : SingletonMono<BackpackInventorySystem>
    {
        [SerializeField] private BackpackDefinition activeBackpackDefinition;

        private Backpack backpack;
        private string currentOpenPocket;
        private BackpackInteraction backpackInteraction;
        private readonly List<BackpackPocketDefinition> activePocketDefinitions = new List<BackpackPocketDefinition>();
        private const int HotbarSlotCount = 4;

        public Backpack Backpack => backpack;
        public string CurrentOpenPocket => currentOpenPocket;
        public bool IsInventoryOpen => !string.IsNullOrWhiteSpace(currentOpenPocket);
        public string ActivePocketName => backpack != null ? backpack.ActivePocketName : string.Empty;
        public BackpackDefinition ActiveBackpackDefinition => activeBackpackDefinition;
        public IReadOnlyList<BackpackPocketDefinition> PocketDefinitions => activePocketDefinitions;

        public event Action<string> OnPocketOpened;
        public event Action OnPocketClosed;
        public event Action<string> OnInventoryChanged;

        protected override void Awake()
        {
            base.Awake();
            ResolveActiveBackpackDefinition();
            RefreshPocketDefinitions();
            InitializeBackpack();
            EnsureBackpackInteraction();
        }

        void Start()
        {
            PocketInventoryUIController controller = PocketInventoryUIController.Instance;
            if (controller == null)
            {
                Debug.LogWarning("BackpackInventorySystem could not find a PocketInventoryUIController in the active scene.");
                return;
            }

            controller.InitializeFromSystem(this);
        }

        void Update()
        {
            HandleHotbarSelectionInput();

            if (IsInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
                CloseInventory();
        }

        public Pocket GetPocket(string pocketName)
        {
            return backpack != null ? backpack.GetPocket(pocketName) : null;
        }

        public void OpenPocket(string pocketName)
        {
            if (string.IsNullOrWhiteSpace(pocketName))
                return;

            if (currentOpenPocket == pocketName)
                return;

            if (IsInventoryOpen)
                return;

            Pocket pocket = GetPocket(pocketName);
            if (pocket == null)
            {
                Debug.LogWarning($"Pocket '{pocketName}' not found in backpack.");
                return;
            }

            currentOpenPocket = pocketName;
            backpack.SetActivePocket(pocketName);
            ApplyCursorState();
            OnPocketOpened?.Invoke(currentOpenPocket);
            OnInventoryChanged?.Invoke(currentOpenPocket);
        }

        public void CloseInventory()
        {
            if (!IsInventoryOpen)
                return;

            currentOpenPocket = null;
            ApplyCursorState();
            OnPocketClosed?.Invoke();
        }

        public void HandlePocketClicked(string pocketName)
        {
            OpenPocket(pocketName);
        }

        public void SelectSlot(string pocketName, int slotIndex)
        {
            if (backpack == null)
                return;

            backpack.SelectSlot(pocketName, slotIndex);
            OnInventoryChanged?.Invoke(pocketName);
        }

        public void SelectHotbarSlot(int hotbarIndex)
        {
            if (backpack == null || hotbarIndex < 0 || hotbarIndex >= HotbarSlotCount)
                return;

            backpack.SelectHotbarSlot(hotbarIndex);
            OnInventoryChanged?.Invoke(PocketNames.Hotbar);
        }

        public InventoryItemDefinition GetSelectedItem()
        {
            return backpack != null ? backpack.GetSelectedItem() : null;
        }

        public int GetSelectedSlotIndex(string pocketName)
        {
            if (pocketName == PocketNames.Hotbar)
                return backpack != null ? backpack.SelectedHotbarIndex : -1;

            Pocket pocket = GetPocket(pocketName);
            return pocket != null ? pocket.SelectedSlotIndex : -1;
        }

        public IReadOnlyList<InventorySlotData> GetHotbarSlots()
        {
            return backpack != null ? backpack.HotbarSlots : Array.Empty<InventorySlotData>();
        }

        public InventorySlotData GetSlot(string containerName, int slotIndex)
        {
            return backpack != null ? backpack.GetContainerSlot(containerName, slotIndex) : null;
        }

        public bool TryMoveSlot(string sourceContainerName, int sourceIndex, string targetContainerName, int targetIndex)
        {
            bool result = backpack != null &&
                          backpack.TryMoveSlot(sourceContainerName, sourceIndex, targetContainerName, targetIndex);

            if (!result)
                return false;

            OnInventoryChanged?.Invoke(sourceContainerName);
            if (targetContainerName != sourceContainerName)
                OnInventoryChanged?.Invoke(targetContainerName);

            return true;
        }

        public bool TryConsumeSelectedItem(int amount)
        {
            bool result = backpack != null && backpack.TryConsumeSelectedItem(amount);
            if (result)
                OnInventoryChanged?.Invoke(ActivePocketName);

            return result;
        }

        public bool TryTakeSelectedItem(int amount, out InventoryItemDefinition item, out int quantity)
        {
            item = null;
            quantity = 0;

            bool result = backpack != null && backpack.TryTakeSelectedItem(amount, out item, out quantity);
            if (result)
                OnInventoryChanged?.Invoke(ActivePocketName);

            return result;
        }

        public bool TryAddItem(InventoryItemDefinition item, int amount)
        {
            bool result = backpack != null && backpack.TryAddItem(item, amount);
            if (result)
                OnInventoryChanged?.Invoke(string.Empty);

            return result;
        }

        public bool TryAddItemToPocket(string pocketName, InventoryItemDefinition item, int amount)
        {
            bool result = backpack != null && backpack.TryAddItemToPocket(pocketName, item, amount);
            if (result)
                OnInventoryChanged?.Invoke(pocketName);

            return result;
        }

        public BackpackSaveData BuildSaveData()
        {
            return backpack != null ? backpack.ToSaveData() : new BackpackSaveData();
        }

        public void LoadFromSave(BackpackSaveData saveData)
        {
            ResolveActiveBackpackDefinition();
            RefreshPocketDefinitions();
            InitializeBackpack();
            backpack.LoadFromSave(saveData);
            currentOpenPocket = null;
            OnInventoryChanged?.Invoke(string.Empty);
        }

        public BackpackPocketDefinition GetPocketDefinition(string pocketName)
        {
            if (string.IsNullOrWhiteSpace(pocketName))
                return null;

            for (int i = 0; i < activePocketDefinitions.Count; i++)
            {
                if (activePocketDefinitions[i].PocketName == pocketName)
                    return activePocketDefinitions[i];
            }

            return null;
        }

        public void SetActiveBackpack(BackpackDefinition definition, bool preserveContents = true)
        {
            BackpackSaveData previousSave = preserveContents && backpack != null
                ? backpack.ToSaveData()
                : null;

            activeBackpackDefinition = definition;
            RefreshPocketDefinitions();
            InitializeBackpack();
            EnsureBackpackInteraction();

            if (previousSave != null)
                backpack.LoadFromSave(previousSave);

            currentOpenPocket = null;
            OnPocketClosed?.Invoke();
            OnInventoryChanged?.Invoke(string.Empty);
        }

        private void InitializeBackpack()
        {
            backpack = new Backpack(activePocketDefinitions);
        }

        private void ResolveActiveBackpackDefinition()
        {
            if (activeBackpackDefinition != null)
                return;

            activeBackpackDefinition = FindFirstObjectByType<BackpackDefinition>();

            if (activeBackpackDefinition == null)
            {
                Transform backpackRoot = FindBackpackRoot();
                if (backpackRoot != null)
                    activeBackpackDefinition = backpackRoot.GetComponent<BackpackDefinition>();
            }

            if (activeBackpackDefinition == null)
            {
                Transform backpackRoot = FindBackpackRoot();
                if (backpackRoot != null)
                    activeBackpackDefinition = backpackRoot.gameObject.AddComponent<BackpackDefinition>();
            }
        }

        private void RefreshPocketDefinitions()
        {
            activePocketDefinitions.Clear();

            if (activeBackpackDefinition != null)
                activePocketDefinitions.AddRange(activeBackpackDefinition.GetDefinitionsSnapshot());

            if (activePocketDefinitions.Count == 0)
            {
                for (int i = 0; i < PocketNames.Ordered.Length; i++)
                    activePocketDefinitions.Add(BackpackPocketDefinition.CreateDefault(PocketNames.Ordered[i]));
            }
        }

        private void EnsureBackpackInteraction()
        {
            Transform backpackRoot = FindBackpackRoot();

            if (backpackInteraction == null)
                backpackInteraction = backpackRoot != null
                    ? backpackRoot.GetComponent<BackpackInteraction>()
                    : null;

            if (backpackInteraction == null)
                backpackInteraction = FindFirstObjectByType<BackpackInteraction>();

            if (backpackInteraction == null)
            {
                if (backpackRoot != null)
                    backpackInteraction = backpackRoot.gameObject.AddComponent<BackpackInteraction>();
            }

            if (backpackInteraction == null)
                return;

            backpackInteraction.OnPocketClicked -= HandlePocketClicked;
            backpackInteraction.OnPocketClicked += HandlePocketClicked;
        }

        private Transform FindBackpackRoot()
        {
            if (activeBackpackDefinition != null)
                return activeBackpackDefinition.transform;

            BackpackDefinition definition = FindFirstObjectByType<BackpackDefinition>();
            if (definition != null)
                return definition.transform;

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == "AstronautBackpack")
                    return transforms[i];
            }

            return null;
        }

        private void ApplyCursorState()
        {
            Cursor.visible = IsInventoryOpen || Input.GetMouseButton(1);
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void HandleHotbarSelectionInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || IsInventoryOpen)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame) SelectHotbarSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectHotbarSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectHotbarSlot(2);
            if (keyboard.digit4Key.wasPressedThisFrame) SelectHotbarSlot(3);
        }

        void OnDestroy()
        {
            if (backpackInteraction != null)
                backpackInteraction.OnPocketClicked -= HandlePocketClicked;
        }
    }
}
