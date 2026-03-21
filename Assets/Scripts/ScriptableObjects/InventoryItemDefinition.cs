using UnityEngine;

public abstract class InventoryItemDefinition : ScriptableObject
{
    [Header("Inventory Presentation")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private int maxStack = 99;
    [SerializeField, TextArea] private string description;

    public abstract string ItemId { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public Color Tint => tint;
    public int MaxStack => Mathf.Max(1, maxStack);
    public string Description => description;
}
