using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Simple Item")]
public class SimpleInventoryItemDefinition : InventoryItemDefinition
{
    [SerializeField] private string itemId;

    public override string ItemId => itemId;
}
