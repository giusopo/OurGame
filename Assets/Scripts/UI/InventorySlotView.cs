using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using OurGame.Systems;

public class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Scene References")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectionOverlay;
    [SerializeField] private Text quantityText;
    [SerializeField] private Text shortcutText;
    [SerializeField] private Text placeholderText;

    private PocketInventoryUIController pocketOwner;
    private string pocketName;
    private int slotIndex;
    private bool referencesBound;
    private bool renderAsCursorGhost;
    private bool shortcutVisible = true;

    public string PocketName => pocketName;
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

    public void Configure(PocketInventoryUIController controller, string owningPocketName, int index)
    {
        CacheReferences();

        pocketOwner = controller;
        pocketName = owningPocketName;
        slotIndex = index;
        EnsureVisualOrder();

        if (shortcutText != null)
            shortcutText.text = string.Empty;
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
                ? new Color32(157, 191, 190, 255)
                : new Color32(97, 167, 255, 255);
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = !renderAsCursorGhost;
            backgroundImage.color = selected
                ? new Color32(157, 191, 190, 255)
                : new Color32(224, 224, 224, 220);
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
            quantityText.enabled = slot.Quantity > 1;
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

    public void SetShortcutLabel(string label)
    {
        CacheReferences();
        if (shortcutText != null)
            shortcutText.text = label ?? string.Empty;
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
            quantityText.enabled = quantityText.text.Length > 0;

        if (placeholderText != null)
            placeholderText.enabled = !enabled;
    }

    public void SetRaycastTarget(bool enabled)
    {
        CacheReferences();
        if (frameImage != null)
            frameImage.raycastTarget = enabled;

        if (backgroundImage != null)
            backgroundImage.raycastTarget = enabled;

        if (iconImage != null)
            iconImage.raycastTarget = enabled;

        if (selectionOverlay != null)
            selectionOverlay.raycastTarget = enabled;

        if (quantityText != null)
            quantityText.raycastTarget = enabled;

        if (shortcutText != null)
            shortcutText.raycastTarget = enabled;

        if (placeholderText != null)
            placeholderText.raycastTarget = enabled;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (pocketOwner != null)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                pocketOwner.HandleSlotClick(pocketName, slotIndex);

            return;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || pocketOwner == null)
            return;

        pocketOwner.BeginSlotDrag(pocketName, slotIndex, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (pocketOwner == null)
            return;

        pocketOwner.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (pocketOwner == null)
            return;

        pocketOwner.EndSlotDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (pocketOwner == null)
            return;

        pocketOwner.RegisterDropTarget(pocketName, slotIndex);
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
