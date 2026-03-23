using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDroppedItemTracker : MonoBehaviour
{
    private readonly List<DroppedItem> nearbyDroppedItems = new List<DroppedItem>();

    public DroppedItem SelectedDroppedItem { get; private set; }
    public bool HasSelectedDroppedItem => SelectedDroppedItem != null;
    public string CurrentPromptText => SelectedDroppedItem != null
        ? $"Raccogli {GetDisplayName(SelectedDroppedItem)}"
        : string.Empty;

    public event Action<DroppedItem> OnSelectedDroppedItemChanged;

    void Update()
    {
        PruneDestroyedItems();
        RecalculateSelection();
    }

    public void HandleTriggerEnter(Collider other)
    {
        DroppedItem droppedItem = other.GetComponentInParent<DroppedItem>();
        if (droppedItem == null || nearbyDroppedItems.Contains(droppedItem))
            return;

        nearbyDroppedItems.Add(droppedItem);
        RecalculateSelection();
    }

    public void HandleTriggerExit(Collider other)
    {
        DroppedItem droppedItem = other.GetComponentInParent<DroppedItem>();
        if (droppedItem == null)
            return;

        if (nearbyDroppedItems.Remove(droppedItem))
            RecalculateSelection();
    }

    public bool TryCollectSelected()
    {
        if (SelectedDroppedItem == null)
            return false;

        bool collected = SelectedDroppedItem.TryCollect();
        if (collected)
        {
            nearbyDroppedItems.Remove(SelectedDroppedItem);
            RecalculateSelection();
        }

        return collected;
    }

    private void PruneDestroyedItems()
    {
        bool removedAny = false;
        for (int i = nearbyDroppedItems.Count - 1; i >= 0; i--)
        {
            if (nearbyDroppedItems[i] == null)
            {
                nearbyDroppedItems.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
            RecalculateSelection();
    }

    private void RecalculateSelection()
    {
        DroppedItem bestItem = null;
        float bestDistance = float.PositiveInfinity;
        Vector3 playerPosition = transform.position;

        foreach (DroppedItem droppedItem in nearbyDroppedItems)
        {
            if (droppedItem == null || !droppedItem.CanBePickedUp)
                continue;

            float distance = (droppedItem.transform.position - playerPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestItem = droppedItem;
            }
        }

        if (SelectedDroppedItem == bestItem)
            return;

        SelectedDroppedItem = bestItem;
        OnSelectedDroppedItemChanged?.Invoke(SelectedDroppedItem);
    }

    private static string GetDisplayName(DroppedItem droppedItem)
    {
        if (droppedItem == null || droppedItem.Item == null)
            return "oggetto";

        return string.IsNullOrWhiteSpace(droppedItem.Item.DisplayName)
            ? droppedItem.Item.name
            : droppedItem.Item.DisplayName;
    }
}
