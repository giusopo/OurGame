using UnityEngine;

public abstract class InventoryItemDefinition : ScriptableObject
{
    [Header("Inventory Presentation")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private int maxStack = 99;
    [SerializeField, TextArea] private string description;

    [Header("World Drop")]
    [SerializeField] private GameObject worldDropPrefab;
    [SerializeField] private Vector3 worldDropScale = Vector3.one * 0.35f;

    public abstract string ItemId { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Icon => icon;
    public Color Tint => tint;
    public int MaxStack => Mathf.Max(1, maxStack);
    public string Description => description;
    public GameObject WorldDropPrefab => worldDropPrefab;
    public Vector3 WorldDropScale => worldDropScale == Vector3.zero ? Vector3.one * 0.35f : worldDropScale;
}
