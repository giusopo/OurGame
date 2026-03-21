using System.Collections.Generic;
using UnityEngine;
using OurGame.Core;

public class InventoryItemDatabase : SingletonMono<InventoryItemDatabase>
{
    [SerializeField] private InventoryItemDefinition[] additionalItems;

    private readonly Dictionary<string, InventoryItemDefinition> itemsById =
        new Dictionary<string, InventoryItemDefinition>();

    protected override void Awake()
    {
        base.Awake();
        RebuildDatabase();
    }

    public InventoryItemDefinition GetItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        if (itemsById.Count == 0)
            RebuildDatabase();

        itemsById.TryGetValue(itemId, out InventoryItemDefinition item);
        return item;
    }

    public void RebuildDatabase()
    {
        itemsById.Clear();

        InventoryItemDefinition[] resourceItems =
            Resources.LoadAll<InventoryItemDefinition>(string.Empty);

        foreach (InventoryItemDefinition item in resourceItems)
            TryRegister(item);

        foreach (InventoryItemDefinition item in additionalItems)
            TryRegister(item);
    }

    private void TryRegister(InventoryItemDefinition item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            return;

        if (itemsById.ContainsKey(item.ItemId))
        {
            Debug.LogWarning("Duplicate inventory item id rilevato: " + item.ItemId);
            return;
        }

        itemsById[item.ItemId] = item;
    }
}
