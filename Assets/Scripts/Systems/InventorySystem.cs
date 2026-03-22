using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using OurGame.Core;

public class InventorySystem : SingletonMono<InventorySystem>
{
    [SerializeField] private int inventorySlotCount = 12;
    [SerializeField] private int hotbarSlotCount = 4;
    [SerializeField] private bool preferHotbarForNewStacks = true;

    private InventorySlotData[] mainSlots;
    private InventorySlotData[] hotbarSlots;
    private InventorySlotData cursorSlot = new InventorySlotData();
    private int selectedHotbarIndex;
    private bool inventoryOpen;
    private bool uiBootstrapped;

    public IReadOnlyList<InventorySlotData> MainSlots => mainSlots;
    public IReadOnlyList<InventorySlotData> HotbarSlots => hotbarSlots;
    public InventorySlotData CursorSlot => cursorSlot;
    public int SelectedHotbarIndex => selectedHotbarIndex;
    public bool IsInventoryOpen => inventoryOpen;

    public event Action OnInventoryChanged;
    public event Action<bool> OnInventoryToggled;
    public event Action<int> OnSelectedHotbarChanged;
    public event Action<InventoryItemDefinition> OnHeldItemChanged;

    protected override void Awake()
    {
        base.Awake();
        EnsureSlotsInitialized();
        EnsureUiBootstrapped();
        ApplyCursorState();
    }

    void Update()
    {
        HandleInventoryToggleInput();
        HandleHotbarSelectionInput();
    }

    public InventoryItemDefinition GetSelectedItem()
    {
        return hotbarSlots[selectedHotbarIndex].Item;
    }

    public bool TryConsumeSelectedItem(int amount)
    {
        if (amount <= 0)
            return true;

        InventorySlotData slot = hotbarSlots[selectedHotbarIndex];
        if (slot.IsEmpty || slot.Quantity < amount)
            return false;

        slot.Remove(amount);
        NotifyInventoryChanged();
        return true;
    }

    public bool TryTakeSelectedItem(int amount, out InventoryItemDefinition item, out int quantity)
    {
        item = null;
        quantity = 0;

        if (amount <= 0 || hotbarSlots == null || hotbarSlots.Length == 0)
            return false;

        InventorySlotData slot = hotbarSlots[selectedHotbarIndex];
        if (slot.IsEmpty)
            return false;

        item = slot.Item;
        int removed = slot.Remove(amount);
        if (removed <= 0)
        {
            item = null;
            return false;
        }

        quantity = removed;

        NotifyInventoryChanged();
        return true;
    }

    public bool CanAddItem(InventoryItemDefinition item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;
        remaining = CountRemainingCapacity(mainSlots, item, remaining);
        remaining = CountRemainingCapacity(hotbarSlots, item, remaining);
        return remaining == 0;
    }

    public bool TryAddItem(InventoryItemDefinition item, int amount)
    {
        if (!CanAddItem(item, amount))
            return false;

        int remaining = amount;

        if (preferHotbarForNewStacks)
        {
            remaining = FillExistingStacks(hotbarSlots, item, remaining);
            remaining = FillExistingStacks(mainSlots, item, remaining);
            remaining = FillEmptySlots(hotbarSlots, item, remaining);
            remaining = FillEmptySlots(mainSlots, item, remaining);
        }
        else
        {
            remaining = FillExistingStacks(mainSlots, item, remaining);
            remaining = FillExistingStacks(hotbarSlots, item, remaining);
            remaining = FillEmptySlots(mainSlots, item, remaining);
            remaining = FillEmptySlots(hotbarSlots, item, remaining);
        }

        if (remaining == 0)
            NotifyInventoryChanged();

        return remaining == 0;
    }

    public void HandleSlotLeftClick(InventorySection section, int index)
    {
        InventorySlotData slot = GetSlot(section, index);
        if (slot == null)
            return;

        if (section == InventorySection.Hotbar)
            SelectHotbarSlot(index);

        if (cursorSlot.IsEmpty)
        {
            if (slot.IsEmpty)
                return;

            cursorSlot.CopyFrom(slot);
            slot.Clear();
            NotifyInventoryChanged();
            return;
        }

        if (slot.IsEmpty)
        {
            slot.CopyFrom(cursorSlot);
            cursorSlot.Clear();
            NotifyInventoryChanged();
            return;
        }

        if (slot.CanStackWith(cursorSlot.Item))
        {
            int moved = slot.Add(cursorSlot.Item, cursorSlot.Quantity);
            cursorSlot.Remove(moved);
            NotifyInventoryChanged();
            return;
        }

        if (!slot.IsEmpty && slot.Item == cursorSlot.Item)
            return;

        InventorySlotData snapshot = slot.Clone();
        slot.CopyFrom(cursorSlot);
        cursorSlot.CopyFrom(snapshot);
        NotifyInventoryChanged();
    }

    public void HandleSlotRightClick(InventorySection section, int index)
    {
        InventorySlotData slot = GetSlot(section, index);
        if (slot == null)
            return;

        if (section == InventorySection.Hotbar)
            SelectHotbarSlot(index);

        if (cursorSlot.IsEmpty)
        {
            if (slot.IsEmpty)
                return;

            int amountToPick = Mathf.CeilToInt(slot.Quantity / 2f);
            cursorSlot.Set(slot.Item, amountToPick);
            slot.Remove(amountToPick);
            NotifyInventoryChanged();
            return;
        }

        if (slot.IsEmpty)
        {
            slot.Set(cursorSlot.Item, 1);
            cursorSlot.Remove(1);
            NotifyInventoryChanged();
            return;
        }

        if (!slot.CanStackWith(cursorSlot.Item))
            return;

        int moved = slot.Add(cursorSlot.Item, 1);
        if (moved > 0)
        {
            cursorSlot.Remove(1);
            NotifyInventoryChanged();
        }
    }

    public void SelectHotbarSlot(int index)
    {
        if (hotbarSlots == null || hotbarSlots.Length == 0)
            return;

        int clampedIndex = Mathf.Clamp(index, 0, hotbarSlots.Length - 1);
        if (selectedHotbarIndex == clampedIndex)
        {
            RaiseHeldItemChanged();
            return;
        }

        selectedHotbarIndex = clampedIndex;
        OnSelectedHotbarChanged?.Invoke(selectedHotbarIndex);
        RaiseHeldItemChanged();
    }

    public void ToggleInventory()
    {
        SetInventoryOpen(!inventoryOpen);
    }

    public void SetInventoryOpen(bool open)
    {
        if (inventoryOpen == open)
            return;

        inventoryOpen = open;
        ApplyCursorState();
        OnInventoryToggled?.Invoke(inventoryOpen);
    }

    public void LoadFromSave(
        List<InventorySlotSaveData> savedInventory,
        List<InventorySlotSaveData> savedHotbar,
        int savedSelectedHotbarIndex
    )
    {
        EnsureSlotsInitialized();

        RestoreSlotArray(mainSlots, savedInventory);
        RestoreSlotArray(hotbarSlots, savedHotbar);
        cursorSlot.Clear();

        selectedHotbarIndex = hotbarSlots.Length == 0
            ? 0
            : Mathf.Clamp(savedSelectedHotbarIndex, 0, hotbarSlots.Length - 1);

        OnSelectedHotbarChanged?.Invoke(selectedHotbarIndex);
        NotifyInventoryChanged();
    }

    private void RestoreSlotArray(
        InventorySlotData[] targetSlots,
        List<InventorySlotSaveData> savedSlots
    )
    {
        for (int i = 0; i < targetSlots.Length; i++)
            targetSlots[i].Clear();

        if (savedSlots == null)
            return;

        int count = Mathf.Min(targetSlots.Length, savedSlots.Count);
        for (int i = 0; i < count; i++)
        {
            InventorySlotSaveData savedSlot = savedSlots[i];
            if (savedSlot == null || string.IsNullOrWhiteSpace(savedSlot.itemId) || savedSlot.quantity <= 0)
                continue;

            InventoryItemDefinition item = InventoryItemDatabase.Instance.GetItem(savedSlot.itemId);
            if (item == null)
            {
                Debug.LogWarning("Item non trovato nel database: " + savedSlot.itemId);
                continue;
            }

            targetSlots[i].Set(item, Mathf.Min(savedSlot.quantity, item.MaxStack));
        }
    }

    private void HandleInventoryToggleInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.capsLockKey.wasPressedThisFrame)
        {
            ToggleInventory();
            return;
        }

        if (inventoryOpen && keyboard.escapeKey.wasPressedThisFrame)
            SetInventoryOpen(false);
    }

    private void HandleHotbarSelectionInput()
    {
        if (inventoryOpen || hotbarSlots == null || hotbarSlots.Length == 0)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) SelectHotbarSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectHotbarSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectHotbarSlot(2);
            if (keyboard.digit4Key.wasPressedThisFrame) SelectHotbarSlot(3);
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        float scrollValue = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scrollValue, 0f))
            return;

        if (scrollValue > 0f)
            SelectHotbarSlot((selectedHotbarIndex - 1 + hotbarSlots.Length) % hotbarSlots.Length);
        else if (scrollValue < 0f)
            SelectHotbarSlot((selectedHotbarIndex + 1) % hotbarSlots.Length);
    }

    private void ApplyCursorState()
    {
        Cursor.visible = inventoryOpen;
        Cursor.lockState = inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void EnsureSlotsInitialized()
    {
        inventorySlotCount = Mathf.Max(12, inventorySlotCount);
        hotbarSlotCount = Mathf.Max(4, hotbarSlotCount);

        mainSlots ??= CreateSlotArray(inventorySlotCount);
        hotbarSlots ??= CreateSlotArray(hotbarSlotCount);

        if (mainSlots.Length != inventorySlotCount)
            mainSlots = ResizeSlotArray(mainSlots, inventorySlotCount);

        if (hotbarSlots.Length != hotbarSlotCount)
            hotbarSlots = ResizeSlotArray(hotbarSlots, hotbarSlotCount);

        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbarSlots.Length - 1);
    }

    private void EnsureUiBootstrapped()
    {
        if (uiBootstrapped)
            return;

        uiBootstrapped = true;
        InventoryUIController.Instance.InitializeFromSystem(this);
    }

    private InventorySlotData[] CreateSlotArray(int count)
    {
        InventorySlotData[] slots = new InventorySlotData[count];
        for (int i = 0; i < count; i++)
            slots[i] = new InventorySlotData();
        return slots;
    }

    private InventorySlotData[] ResizeSlotArray(InventorySlotData[] source, int newSize)
    {
        InventorySlotData[] resized = CreateSlotArray(newSize);
        if (source == null)
            return resized;

        int copyCount = Mathf.Min(source.Length, newSize);
        for (int i = 0; i < copyCount; i++)
            resized[i].CopyFrom(source[i]);

        return resized;
    }

    private int CountRemainingCapacity(
        InventorySlotData[] slots,
        InventoryItemDefinition item,
        int remaining
    )
    {
        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0)
                return 0;

            if (slot.IsEmpty)
            {
                remaining -= item.MaxStack;
                continue;
            }

            if (slot.Item != item)
                continue;

            remaining -= item.MaxStack - slot.Quantity;
        }

        return Mathf.Max(0, remaining);
    }

    private int FillExistingStacks(
        InventorySlotData[] slots,
        InventoryItemDefinition item,
        int remaining
    )
    {
        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0)
                return 0;

            if (!slot.CanStackWith(item))
                continue;

            int moved = slot.Add(item, remaining);
            remaining -= moved;
        }

        return remaining;
    }

    private int FillEmptySlots(
        InventorySlotData[] slots,
        InventoryItemDefinition item,
        int remaining
    )
    {
        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0)
                return 0;

            if (!slot.IsEmpty)
                continue;

            int moved = Mathf.Min(item.MaxStack, remaining);
            slot.Set(item, moved);
            remaining -= moved;
        }

        return remaining;
    }

    private InventorySlotData GetSlot(InventorySection section, int index)
    {
        InventorySlotData[] slots = section == InventorySection.Hotbar ? hotbarSlots : mainSlots;
        if (index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
        RaiseHeldItemChanged();
    }

    private void RaiseHeldItemChanged()
    {
        OnHeldItemChanged?.Invoke(GetSelectedItem());
    }
}
