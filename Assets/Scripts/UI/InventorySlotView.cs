using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using OurGame.Systems;
using OurGame.UI;

public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Scene References")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectionOverlay;
    [SerializeField] private Text quantityText;
    [SerializeField] private Text shortcutText;
    [SerializeField] private Text placeholderText;

    private InventoryUIController owner;
    private InventorySection section;
    private int slotIndex;
    private bool referencesBound;
    private bool renderAsCursorGhost;
    private bool shortcutVisible = true;

    public InventorySection Section => section;
    public int SlotIndex => slotIndex;

    void Reset()
    {
        CacheReferences();
    }

    void OnValidate()
    {
        referencesBound = false;
        CacheReferences();
        EnsureVisualOrder();
    }

    void Awake()
    {
        CacheReferences();
        EnsureVisualOrder();

        if (selectionOverlay != null)
            selectionOverlay.enabled = false;

        if (iconImage != null)
            iconImage.enabled = false;

        if (quantityText != null)
            quantityText.text = string.Empty;

        if (placeholderText != null)
            placeholderText.text = string.Empty;
    }

    public void Configure(
        InventoryUIController controller,
        InventorySection slotSection,
        int index,
        string shortcutLabel
    )
    {
        CacheReferences();

        owner = controller;
        section = slotSection;
        slotIndex = index;
        EnsureVisualOrder();

        if (shortcutText != null)
            shortcutText.text = shortcutLabel;
    }

    public void Refresh(InventorySlotData slot, bool selected)
    {
        CacheReferences();
        if (!referencesBound)
            return;

        if (selectionOverlay != null)
            selectionOverlay.enabled = selected;

        if (frameImage != null)
        {
            frameImage.enabled = !renderAsCursorGhost;
            frameImage.color = selected
                ? new Color(0.96f, 0.78f, 0.26f, 1f)
                : new Color(0.39f, 0.46f, 0.57f, 0.92f);
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = !renderAsCursorGhost;
            backgroundImage.color = selected
                ? new Color(0.17f, 0.2f, 0.25f, 0.98f)
                : new Color(0.09f, 0.11f, 0.15f, 0.96f);
        }

        if (shortcutText != null)
            shortcutText.enabled = !renderAsCursorGhost && shortcutVisible;

        if (slot == null || slot.IsEmpty)
        {
            if (iconImage != null)
                iconImage.enabled = false;

            if (placeholderText != null)
            {
                placeholderText.enabled = !renderAsCursorGhost;
                placeholderText.text = string.Empty;
            }

            if (quantityText != null)
            {
                quantityText.enabled = !renderAsCursorGhost;
                quantityText.text = string.Empty;
            }

            return;
        }

        if (slot.Item.Icon != null)
        {
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = slot.Item.Icon;
                iconImage.color = slot.Item.Tint;
            }

            if (placeholderText != null)
            {
                placeholderText.enabled = !renderAsCursorGhost;
                placeholderText.text = string.Empty;
            }
        }
        else
        {
            if (iconImage != null)
                iconImage.enabled = false;

            if (placeholderText != null)
            {
                placeholderText.enabled = !renderAsCursorGhost;
                placeholderText.text = GetAbbreviation(slot.Item.DisplayName);
            }
        }

        if (quantityText != null)
        {
            quantityText.enabled = !renderAsCursorGhost;
            quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : string.Empty;
        }
    }

    public void SetShortcutVisible(bool visible)
    {
        CacheReferences();
        shortcutVisible = visible;
        if (shortcutText != null)
            shortcutText.enabled = !renderAsCursorGhost && visible;
    }

    public void SetCursorGhostMode(bool enabled)
    {
        CacheReferences();
        renderAsCursorGhost = enabled;

        if (frameImage != null)
            frameImage.enabled = !enabled;

        if (backgroundImage != null)
            backgroundImage.enabled = !enabled;

        if (selectionOverlay != null)
            selectionOverlay.enabled = false;

        if (shortcutText != null)
            shortcutText.enabled = !enabled && shortcutVisible;

        if (quantityText != null)
            quantityText.enabled = !enabled;

        if (placeholderText != null)
            placeholderText.enabled = !enabled;
    }

    public void SetRaycastTarget(bool enabled)
    {
        CacheReferences();
        if (frameImage != null)
            frameImage.raycastTarget = enabled;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.HandleSlotClick(section, slotIndex, eventData.button);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.BeginSlotDrag(section, slotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndSlotDrag(eventData);
    }

    private void CacheReferences()
    {
        if (referencesBound)
            return;

        frameImage ??= GetComponent<Image>();
        backgroundImage ??= FindChildComponent<Image>("Background");
        iconImage ??= FindChildComponent<Image>("Icon");
        selectionOverlay ??= FindChildComponent<Image>("Selection");
        quantityText ??= FindChildComponent<Text>("Quantity");
        shortcutText ??= FindChildComponent<Text>("Shortcut");
        placeholderText ??= FindChildComponent<Text>("Placeholder");

        referencesBound = frameImage != null
            && backgroundImage != null
            && iconImage != null
            && selectionOverlay != null
            && quantityText != null
            && shortcutText != null
            && placeholderText != null;

        if (!referencesBound)
        {
            Debug.LogWarning(
                $"InventorySlotView '{name}' is missing one or more UI child references."
            );
        }
    }

    private void EnsureVisualOrder()
    {
        if (backgroundImage != null)
            backgroundImage.transform.SetSiblingIndex(0);

        if (selectionOverlay != null)
            selectionOverlay.transform.SetSiblingIndex(1);

        if (iconImage != null)
            iconImage.transform.SetSiblingIndex(2);

        if (placeholderText != null)
            placeholderText.transform.SetSiblingIndex(3);

        if (quantityText != null)
            quantityText.transform.SetSiblingIndex(4);

        if (shortcutText != null)
            shortcutText.transform.SetSiblingIndex(5);
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private string GetAbbreviation(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "?";

        string trimmed = label.Trim();
        return trimmed.Length <= 2
            ? trimmed.ToUpperInvariant()
            : trimmed.Substring(0, 2).ToUpperInvariant();
    }
}
