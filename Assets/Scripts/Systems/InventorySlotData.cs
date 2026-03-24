using System;
using UnityEngine;

namespace OurGame.Systems
{
    [Serializable]
    public class InventorySlotData
    {
        [SerializeField] private InventoryItemDefinition item;
        [SerializeField] private int quantity;

        public InventoryItemDefinition Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public void Set(InventoryItemDefinition newItem, int newQuantity)
        {
            item = newItem;
            quantity = Mathf.Max(0, newQuantity);

            if (item == null || quantity == 0)
                Clear();
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }

        public bool CanStackWith(InventoryItemDefinition otherItem)
        {
            return !IsEmpty && item == otherItem && quantity < item.MaxStack;
        }

        public int Add(InventoryItemDefinition otherItem, int amount)
        {
            if (otherItem == null || amount <= 0)
                return 0;

            if (IsEmpty)
            {
                int movedIntoEmptySlot = Mathf.Min(otherItem.MaxStack, amount);
                Set(otherItem, movedIntoEmptySlot);
                return movedIntoEmptySlot;
            }

            if (!CanStackWith(otherItem))
                return 0;

            int moved = Mathf.Min(otherItem.MaxStack - quantity, amount);
            quantity += moved;
            return moved;
        }

        public int Remove(int amount)
        {
            if (IsEmpty || amount <= 0)
                return 0;

            int removed = Mathf.Min(quantity, amount);
            quantity -= removed;

            if (quantity <= 0)
                Clear();

            return removed;
        }

        public void CopyFrom(InventorySlotData other)
        {
            if (other == null || other.IsEmpty)
            {
                Clear();
                return;
            }

            Set(other.Item, other.Quantity);
        }

        public InventorySlotData Clone()
        {
            InventorySlotData clone = new InventorySlotData();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
