using System;
using System.Collections.Generic;
using OurGame.Core;
using UnityEngine;

namespace OurGame.Systems
{
    [Serializable]
    public class Backpack
    {
        private const int HotbarSlotCount = 4;

        [SerializeField] private string activePocketName = PocketNames.CentralPocket;
        [SerializeField] private int selectedHotbarIndex;
        [SerializeField] private List<InventorySlotData> hotbarSlots = new List<InventorySlotData>();
        [SerializeField] private List<Pocket> serializedPockets = new List<Pocket>();

        private readonly Dictionary<string, Pocket> pockets = new Dictionary<string, Pocket>();
        private readonly List<string> pocketOrder = new List<string>();

        public string ActivePocketName => activePocketName;
        public int SelectedHotbarIndex => selectedHotbarIndex;
        public IReadOnlyDictionary<string, Pocket> Pockets => pockets;
        public IReadOnlyList<InventorySlotData> HotbarSlots => hotbarSlots;

        public Backpack(IEnumerable<KeyValuePair<string, int>> capacities)
        {
            Initialize(capacities);
        }

        public void Initialize(IEnumerable<KeyValuePair<string, int>> capacities)
        {
            pockets.Clear();
            serializedPockets.Clear();
            pocketOrder.Clear();

            foreach (KeyValuePair<string, int> pair in capacities)
            {
                Pocket pocket = new Pocket(pair.Key, pair.Value);
                pockets[pair.Key] = pocket;
                serializedPockets.Add(pocket);
                pocketOrder.Add(pair.Key);
            }

            if (pocketOrder.Count == 0)
            {
                for (int i = 0; i < PocketNames.Ordered.Length; i++)
                {
                    Pocket pocket = new Pocket(PocketNames.Ordered[i], 4);
                    pockets[pocket.Name] = pocket;
                    serializedPockets.Add(pocket);
                    pocketOrder.Add(pocket.Name);
                }
            }

            if (!pockets.ContainsKey(activePocketName))
                activePocketName = pocketOrder[0];

            EnsureHotbarInitialized();
        }

        public Pocket GetPocket(string pocketName)
        {
            if (string.IsNullOrWhiteSpace(pocketName))
                return null;

            pockets.TryGetValue(pocketName, out Pocket pocket);
            return pocket;
        }

        public IEnumerable<Pocket> GetAllPockets()
        {
            return serializedPockets;
        }

        public void SetActivePocket(string pocketName)
        {
            if (string.IsNullOrWhiteSpace(pocketName) || !pockets.ContainsKey(pocketName))
                return;

            activePocketName = pocketName;
        }

        public InventorySlotData GetHotbarSlot(int index)
        {
            EnsureHotbarInitialized();
            if (index < 0 || index >= hotbarSlots.Count)
                return null;

            return hotbarSlots[index];
        }

        public void SelectHotbarSlot(int index)
        {
            EnsureHotbarInitialized();
            selectedHotbarIndex = Mathf.Clamp(index, 0, hotbarSlots.Count - 1);
        }

        public void SelectSlot(string pocketName, int slotIndex)
        {
            Pocket pocket = GetPocket(pocketName);
            if (pocket == null)
                return;

            pocket.SelectSlot(slotIndex);
        }

        public InventoryItemDefinition GetSelectedItem()
        {
            InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);
            return slot != null && !slot.IsEmpty ? slot.Item : null;
        }

        public bool TryConsumeSelectedItem(int amount)
        {
            InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);
            if (slot == null || slot.IsEmpty || amount <= 0 || slot.Quantity < amount)
                return false;

            slot.Remove(amount);
            return true;
        }

        public bool TryTakeSelectedItem(int amount, out InventoryItemDefinition item, out int quantity)
        {
            item = null;
            quantity = 0;

            InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);
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

        public bool TryAddItemToPocket(string pocketName, InventoryItemDefinition item, int amount)
        {
            Pocket pocket = GetPocket(pocketName);
            return pocket != null && pocket.TryAddItem(item, amount);
        }

        public bool TryAddItem(InventoryItemDefinition item, int amount)
        {
            if (item == null || amount <= 0)
                return false;

            int remaining = amount;
            remaining = TryAddToContainer(hotbarSlots, item, remaining, allowEmptySlots: false);
            remaining = TryAddToContainer(hotbarSlots, item, remaining, allowEmptySlots: true);

            for (int i = 0; i < pocketOrder.Count; i++)
            {
                Pocket pocket = GetPocket(pocketOrder[i]);
                if (pocket == null)
                    continue;

                remaining = pocket.TryAddItemInternal(item, remaining);
                if (remaining <= 0)
                    return true;
            }

            return false;
        }

        public BackpackSaveData ToSaveData()
        {
            EnsureHotbarInitialized();
            BackpackSaveData saveData = new BackpackSaveData
            {
                activePocketName = activePocketName,
                selectedHotbarIndex = selectedHotbarIndex
            };

            for (int i = 0; i < hotbarSlots.Count; i++)
            {
                saveData.hotbarSlots.Add(new InventorySlotSaveData
                {
                    itemId = hotbarSlots[i].IsEmpty ? string.Empty : hotbarSlots[i].Item.ItemId,
                    quantity = hotbarSlots[i].Quantity
                });
            }

            for (int i = 0; i < serializedPockets.Count; i++)
                saveData.pockets.Add(serializedPockets[i].ToSaveData());

            return saveData;
        }

        public void LoadFromSave(BackpackSaveData saveData)
        {
            if (saveData == null)
                return;

            if (saveData.pockets != null)
            {
                for (int i = 0; i < saveData.pockets.Count; i++)
                {
                    PocketSaveData pocketSave = saveData.pockets[i];
                    Pocket pocket = GetPocket(pocketSave.pocketName);
                    if (pocket == null)
                        continue;

                    pocket.LoadFromSave(pocketSave);
                }
            }

            if (!string.IsNullOrWhiteSpace(saveData.activePocketName) && pockets.ContainsKey(saveData.activePocketName))
                activePocketName = saveData.activePocketName;

            EnsureHotbarInitialized();
            ClearHotbar();

            if (saveData.hotbarSlots != null)
            {
                int count = Mathf.Min(hotbarSlots.Count, saveData.hotbarSlots.Count);
                for (int i = 0; i < count; i++)
                {
                    InventorySlotSaveData slotSave = saveData.hotbarSlots[i];
                    if (slotSave == null || string.IsNullOrWhiteSpace(slotSave.itemId) || slotSave.quantity <= 0)
                        continue;

                    InventoryItemDefinition item = InventoryItemDatabase.Instance.GetItem(slotSave.itemId);
                    if (item == null)
                        continue;

                    hotbarSlots[i].Set(item, Mathf.Min(slotSave.quantity, item.MaxStack));
                }
            }

            selectedHotbarIndex = Mathf.Clamp(saveData.selectedHotbarIndex, 0, hotbarSlots.Count - 1);
        }

        public bool TryMoveSlot(string sourceContainerName, int sourceIndex, string targetContainerName, int targetIndex)
        {
            InventorySlotData sourceSlot = ResolveSlot(sourceContainerName, sourceIndex);
            InventorySlotData targetSlot = ResolveSlot(targetContainerName, targetIndex);
            if (sourceSlot == null || targetSlot == null || sourceSlot.IsEmpty)
                return false;

            if (ReferenceEquals(sourceSlot, targetSlot))
                return false;

            if (targetSlot.IsEmpty)
            {
                targetSlot.CopyFrom(sourceSlot);
                sourceSlot.Clear();
                return true;
            }

            if (targetSlot.Item == sourceSlot.Item && targetSlot.Quantity < targetSlot.Item.MaxStack)
            {
                int moved = targetSlot.Add(sourceSlot.Item, sourceSlot.Quantity);
                sourceSlot.Remove(moved);
                return moved > 0;
            }

            return false;
        }

        public InventorySlotData GetContainerSlot(string containerName, int slotIndex)
        {
            return ResolveSlot(containerName, slotIndex);
        }

        private InventorySlotData ResolveSlot(string containerName, int slotIndex)
        {
            if (string.Equals(containerName, PocketNames.Hotbar, StringComparison.Ordinal))
                return GetHotbarSlot(slotIndex);

            Pocket pocket = GetPocket(containerName);
            return pocket?.GetSlot(slotIndex);
        }

        private void EnsureHotbarInitialized()
        {
            if (hotbarSlots == null)
                hotbarSlots = new List<InventorySlotData>();

            while (hotbarSlots.Count < HotbarSlotCount)
                hotbarSlots.Add(new InventorySlotData());

            if (hotbarSlots.Count > HotbarSlotCount)
                hotbarSlots.RemoveRange(HotbarSlotCount, hotbarSlots.Count - HotbarSlotCount);

            selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, HotbarSlotCount - 1);
        }

        private void ClearHotbar()
        {
            EnsureHotbarInitialized();
            for (int i = 0; i < hotbarSlots.Count; i++)
                hotbarSlots[i].Clear();
        }

        private int TryAddToContainer(
            IReadOnlyList<InventorySlotData> container,
            InventoryItemDefinition item,
            int amount,
            bool allowEmptySlots
        )
        {
            int remaining = amount;
            if (container == null || item == null || remaining <= 0)
                return remaining;

            for (int i = 0; i < container.Count; i++)
            {
                InventorySlotData slot = container[i];
                if (slot == null)
                    continue;

                if (!allowEmptySlots && !slot.CanStackWith(item))
                    continue;

                if (allowEmptySlots)
                {
                    if (!slot.IsEmpty && !slot.CanStackWith(item))
                        continue;
                }

                remaining -= slot.Add(item, remaining);
                if (remaining <= 0)
                    return 0;
            }

            return remaining;
        }

        public void SaveToDisk(string playerPrefsKey = "backpack_inventory_v1")
        {
            PlayerPrefs.SetString(playerPrefsKey, JsonUtility.ToJson(ToSaveData()));
            PlayerPrefs.Save();
        }

        public void LoadFromDisk(string playerPrefsKey = "backpack_inventory_v1")
        {
            if (!PlayerPrefs.HasKey(playerPrefsKey))
                return;

            BackpackSaveData saveData = JsonUtility.FromJson<BackpackSaveData>(PlayerPrefs.GetString(playerPrefsKey));
            LoadFromSave(saveData);
        }
    }
}
