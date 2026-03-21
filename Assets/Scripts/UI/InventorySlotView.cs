using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotView : MonoBehaviour, IPointerClickHandler
{
    private InventoryUIController owner;
    private InventorySection section;
    private int slotIndex;

    private Image backgroundImage;
    private Image iconImage;
    private Image selectionOverlay;
    private Text quantityText;
    private Text shortcutText;
    private Text placeholderText;

    public void Initialize(
        InventoryUIController controller,
        InventorySection slotSection,
        int index,
        Font font,
        string shortcutLabel
    )
    {
        owner = controller;
        section = slotSection;
        slotIndex = index;

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        backgroundImage = gameObject.GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = gameObject.AddComponent<Image>();

        backgroundImage.color = new Color(0.09f, 0.11f, 0.15f, 0.92f);
        backgroundImage.raycastTarget = true;

        selectionOverlay = CreateImage("Selection", new Color(1f, 0.77f, 0.25f, 0.2f));
        selectionOverlay.enabled = false;

        iconImage = CreateImage("Icon", Color.white);
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.offsetMin = new Vector2(10f, 10f);
        iconRect.offsetMax = new Vector2(-10f, -10f);
        iconImage.preserveAspect = true;
        iconImage.enabled = false;
        iconImage.raycastTarget = false;

        placeholderText = CreateText("Placeholder", font, 18, TextAnchor.MiddleCenter);
        placeholderText.color = new Color(0.92f, 0.92f, 0.92f, 0.88f);
        placeholderText.raycastTarget = false;

        quantityText = CreateText("Quantity", font, 16, TextAnchor.LowerRight);
        quantityText.color = Color.white;
        RectTransform quantityRect = quantityText.rectTransform;
        quantityRect.offsetMin = new Vector2(4f, 4f);
        quantityRect.offsetMax = new Vector2(-8f, -6f);
        quantityText.raycastTarget = false;

        shortcutText = CreateText("Shortcut", font, 12, TextAnchor.UpperLeft);
        shortcutText.text = shortcutLabel;
        shortcutText.color = new Color(0.75f, 0.81f, 0.92f, 0.85f);
        RectTransform shortcutRect = shortcutText.rectTransform;
        shortcutRect.offsetMin = new Vector2(8f, 0f);
        shortcutRect.offsetMax = new Vector2(0f, -6f);
        shortcutText.raycastTarget = false;
    }

    public void Refresh(InventorySlotData slot, bool selected)
    {
        selectionOverlay.enabled = selected;
        backgroundImage.color = selected
            ? new Color(0.18f, 0.21f, 0.27f, 0.98f)
            : new Color(0.09f, 0.11f, 0.15f, 0.92f);

        if (slot == null || slot.IsEmpty)
        {
            iconImage.enabled = false;
            placeholderText.text = string.Empty;
            quantityText.text = string.Empty;
            return;
        }

        if (slot.Item.Icon != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = slot.Item.Icon;
            iconImage.color = slot.Item.Tint;
            placeholderText.text = string.Empty;
        }
        else
        {
            iconImage.enabled = false;
            placeholderText.text = GetAbbreviation(slot.Item.DisplayName);
        }

        quantityText.text = slot.Quantity > 1 ? slot.Quantity.ToString() : string.Empty;
    }

    public void SetShortcutVisible(bool visible)
    {
        if (shortcutText != null)
            shortcutText.enabled = visible;
    }

    public void SetRaycastTarget(bool enabled)
    {
        if (backgroundImage != null)
            backgroundImage.raycastTarget = enabled;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.HandleSlotClick(section, slotIndex, eventData.button);
    }

    private Image CreateImage(string name, Color color)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(transform, false);

        Image image = child.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }

    private Text CreateText(string name, Font font, int fontSize, TextAnchor alignment)
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
        child.transform.SetParent(transform, false);

        Text text = child.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
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
