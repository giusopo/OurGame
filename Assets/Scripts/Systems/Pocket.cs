using System;
using System.Collections.Generic;
using OurGame.Core;
using UnityEngine;

namespace OurGame.Systems
{
    [Serializable]
    public class Pocket : IPocket
    {
        [SerializeField] private string name;
        [SerializeField] private int capacity;
        [SerializeField] private int selectedSlotIndex;
        [SerializeField] private List<InventorySlotData> slots = new List<InventorySlotData>();

        public string Name => name;
        public int Capacity => capacity;
        public IReadOnlyList<InventorySlotData> Slots => slots;
        public int SelectedSlotIndex => selectedSlotIndex;

        public Pocket(string pocketName, int pocketCapacity)
        {
            name = pocketName;
            capacity = Mathf.Max(1, pocketCapacity);
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            capacity = Mathf.Max(1, capacity);

            if (slots == null)
                slots = new List<InventorySlotData>();

            while (slots.Count < capacity)
                slots.Add(new InventorySlotData());

            if (slots.Count > capacity)
                slots.RemoveRange(capacity, slots.Count - capacity);

            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, capacity - 1);
        }

        public InventorySlotData GetSlot(int index)
        {
            EnsureInitialized();
            if (index < 0 || index >= slots.Count)
                return null;

            return slots[index];
        }

        public void SelectSlot(int index)
        {
            EnsureInitialized();
            selectedSlotIndex = Mathf.Clamp(index, 0, slots.Count - 1);
        }

        public InventoryItemDefinition GetSelectedItem()
        {
            InventorySlotData slot = GetSlot(selectedSlotIndex);
            return slot != null && !slot.IsEmpty ? slot.Item : null;
        }

        public bool CanAddItem(InventoryItemDefinition item, int amount)
        {
            return GetRemainingCapacity(item, amount) == 0;
        }

        public bool TryAddItem(InventoryItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
                return false;

            return TryAddItemInternal(item, amount) == 0;
        }

        public int TryAddItemInternal(InventoryItemDefinition item, int amount)
        {
            EnsureInitialized();
            if (item == null || amount <= 0)
                return amount;

            int remaining = amount;
            for (int i = 0; i < slots.Count; i++)
            {
                if (remaining <= 0)
                    return 0;

                if (!slots[i].CanStackWith(item))
                    continue;

                remaining -= slots[i].Add(item, remaining);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (remaining <= 0)
                    return 0;

                if (!slots[i].IsEmpty)
                    continue;

                remaining -= slots[i].Add(item, remaining);
            }

            return remaining;
        }

        public bool TryConsumeSelectedItem(int amount)
        {
            InventorySlotData slot = GetSlot(selectedSlotIndex);
            if (slot == null || slot.IsEmpty || slot.Quantity < amount || amount <= 0)
                return false;

            slot.Remove(amount);
            return true;
        }

        public bool TryTakeSelectedItem(int amount, out InventoryItemDefinition item, out int quantity)
        {
            item = null;
            quantity = 0;

            InventorySlotData slot = GetSlot(selectedSlotIndex);
            if (slot == null || slot.IsEmpty || amount <= 0)
                return false;

            item = slot.Item;
            quantity = slot.Remove(amount);
            if (quantity <= 0)
            {
                item = null;
                return false;
            }

            return true;
        }

        public void Clear()
        {
            EnsureInitialized();
            for (int i = 0; i < slots.Count; i++)
                slots[i].Clear();
        }

        public PocketSaveData ToSaveData()
        {
            EnsureInitialized();

            PocketSaveData saveData = new PocketSaveData
            {
                pocketName = name,
                capacity = capacity,
                selectedSlotIndex = selectedSlotIndex
            };

            for (int i = 0; i < slots.Count; i++)
            {
                saveData.slots.Add(new InventorySlotSaveData
                {
                    itemId = slots[i].IsEmpty ? string.Empty : slots[i].Item.ItemId,
                    quantity = slots[i].Quantity
                });
            }

            return saveData;
        }

        public void LoadFromSave(PocketSaveData saveData)
        {
            if (saveData == null)
            {
                Clear();
                return;
            }

            EnsureInitialized();
            Clear();

            int count = Mathf.Min(slots.Count, saveData.slots.Count);
            for (int i = 0; i < count; i++)
            {
                InventorySlotSaveData slotSave = saveData.slots[i];
                if (slotSave == null || string.IsNullOrWhiteSpace(slotSave.itemId) || slotSave.quantity <= 0)
                    continue;

                InventoryItemDefinition item = InventoryItemDatabase.Instance.GetItem(slotSave.itemId);
                if (item == null)
                    continue;

                slots[i].Set(item, Mathf.Min(slotSave.quantity, item.MaxStack));
            }

            selectedSlotIndex = Mathf.Clamp(saveData.selectedSlotIndex, 0, slots.Count - 1);
        }

        private int GetRemainingCapacity(InventoryItemDefinition item, int amount)
        {
            EnsureInitialized();
            int remaining = amount;

            for (int i = 0; i < slots.Count; i++)
            {
                if (remaining <= 0)
                    return 0;

                if (slots[i].IsEmpty)
                {
                    remaining -= item.MaxStack;
                    continue;
                }

                if (slots[i].Item != item)
                    continue;

                remaining -= item.MaxStack - slots[i].Quantity;
            }

            return Mathf.Max(0, remaining);
        }
    }
}
