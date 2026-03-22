using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerItemDropController : MonoBehaviour
{
    private Transform itemDropOrigin;
    private float itemDropForwardOffset;
    private float itemDropUpOffset;

    public void Configure(Transform origin, float forwardOffset, float upOffset)
    {
        itemDropOrigin = origin;
        itemDropForwardOffset = forwardOffset;
        itemDropUpOffset = upOffset;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || InventorySystem.Instance.IsInventoryOpen)
            return;

        if (keyboard.qKey.wasPressedThisFrame)
            DropSelectedItem();
    }

    private void DropSelectedItem()
    {
        if (!InventorySystem.Instance.TryTakeSelectedItem(1, out InventoryItemDefinition item, out int quantity))
            return;

        Vector3 dropCenter = GetDropCenter();
        Transform playerRoot = GetPlayerRootTransform();
        Vector3 launchForward = playerRoot.forward.sqrMagnitude > 0.001f
            ? playerRoot.forward.normalized
            : Vector3.forward;
        Vector3 dropPosition =
            dropCenter
            + launchForward * Mathf.Clamp(itemDropForwardOffset, 0f, 1.25f)
            + Vector3.up * Mathf.Clamp(itemDropUpOffset, -0.25f, 0.35f);

        DroppedItem droppedItem = DroppedItem.Spawn(item, quantity, dropPosition, launchForward);
        if (droppedItem != null)
            droppedItem.IgnoreCollisionsWith(GetComponentsInChildren<Collider>());
    }

    private Vector3 GetDropCenter()
    {
        Collider playerCollider = GetPrimaryPlayerCollider();
        if (playerCollider != null)
            return playerCollider.bounds.center;

        if (itemDropOrigin != null)
            return itemDropOrigin.position;

        return GetPlayerRootTransform().position;
    }

    private Transform GetPlayerRootTransform()
    {
        PlayerController controller = GetComponentInParent<PlayerController>();
        return controller != null ? controller.transform : transform.root;
    }

    private Collider GetPrimaryPlayerCollider()
    {
        Collider[] parentColliders = GetComponentsInParent<Collider>(true);
        foreach (Collider col in parentColliders)
        {
            if (col != null && !col.isTrigger)
                return col;
        }

        Collider[] childColliders = GetPlayerRootTransform().GetComponentsInChildren<Collider>(true);
        foreach (Collider col in childColliders)
        {
            if (col != null && !col.isTrigger)
                return col;
        }

        return null;
    }
}
