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
        if (eventData.button != PointerEventData.InputButton.Left || controller == null)
            return;

        controller.HandleOverlayBackgroundClick();
    }

    private void AutoAssignReferences()
    {
        controller ??= FindFirstObjectByType<PocketInventoryUIController>();
    }
}
