using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class InventoryOverlayDismissRegion : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PocketInventoryUIController controller;

    void Reset()
    {
        AutoAssignReferences();
    }

    void OnValidate()
    {
        AutoAssignReferences();
    }

    void Awake()
    {
        AutoAssignReferences();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        AutoAssignReferences();
        if (controller == null)
            return;

        GameObject raycastTarget =
            eventData.pointerCurrentRaycast.gameObject ??
            eventData.pointerPressRaycast.gameObject;

        if (raycastTarget != gameObject)
            return;

        controller.HandleOverlayBackgroundClick();
    }

    private void AutoAssignReferences()
    {
        controller ??= FindFirstObjectByType<PocketInventoryUIController>();
    }
}
