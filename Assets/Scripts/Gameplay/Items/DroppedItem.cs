using UnityEngine;
using OurGame.Core;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class DroppedItem : MonoBehaviour
{
    [SerializeField] private float pickupCooldownSeconds = 0.6f;
    [SerializeField] private float throwForce = 4.5f;
    [SerializeField] private float upwardForce = 2.25f;
    [SerializeField] private float pickupRadius = 1.15f;

    private InventoryItemDefinition item;
    private int quantity;
    private Rigidbody rb;
    private float pickupBlockedUntil;
    private SphereCollider pickupTrigger;

    public InventoryItemDefinition Item => item;
    public int Quantity => quantity;
    public bool CanBePickedUp => Time.time >= pickupBlockedUntil;

    public static DroppedItem Spawn(
        InventoryItemDefinition item,
        int quantity,
        Vector3 position,
        Vector3 forward
    )
    {
        if (item == null || quantity <= 0)
            return null;

        GameObject go = new GameObject($"Dropped_{item.DisplayName}");
        DroppedItem droppedItem = go.AddComponent<DroppedItem>();
        droppedItem.Initialize(item, quantity);
        droppedItem.Throw(position, forward);
        return droppedItem;
    }

    public void Initialize(InventoryItemDefinition itemDefinition, int stackQuantity)
    {
        item = itemDefinition;
        quantity = Mathf.Max(1, stackQuantity);
        pickupBlockedUntil = Time.time + pickupCooldownSeconds;

        rb = GetComponent<Rigidbody>();
        rb.mass = 1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BuildVisual();
        EnsurePhysicsCollider();
        EnsurePickupTrigger();
    }

    public bool TryCollect()
    {
        if (item == null || quantity <= 0 || !CanBePickedUp)
            return false;

        if (!InventorySystem.Instance.TryAddItem(item, quantity))
            return false;

        Destroy(gameObject);
        return true;
    }

    private void Throw(Vector3 position, Vector3 forward)
    {
        transform.position = position;

        Vector3 launchDirection = forward.sqrMagnitude > 0.001f
            ? forward.normalized
            : Vector3.forward;

        rb.AddForce(
            launchDirection * throwForce + Vector3.up * upwardForce,
            ForceMode.Impulse
        );
    }

    private void BuildVisual()
    {
        if (item.WorldDropPrefab != null)
        {
            GameObject visual = Instantiate(item.WorldDropPrefab, transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = item.WorldDropScale;
            RemoveNestedRigidbodies(visual);
            return;
        }

        GameObject visualFallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualFallback.name = "FallbackVisual";
        visualFallback.transform.SetParent(transform, false);
        visualFallback.transform.localScale = item.WorldDropScale;

        Renderer renderer = visualFallback.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = item.Tint;
    }

    private void EnsurePhysicsCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null && !col.isTrigger)
                return;
        }

        SphereCollider physicsCollider = gameObject.AddComponent<SphereCollider>();
        physicsCollider.radius = 0.25f;
    }

    private void EnsurePickupTrigger()
    {
        GameObject triggerObject = new GameObject("PickupTrigger");
        triggerObject.transform.SetParent(transform, false);

        pickupTrigger = triggerObject.AddComponent<SphereCollider>();
        pickupTrigger.isTrigger = true;
        pickupTrigger.radius = pickupRadius;
    }

    private void RemoveNestedRigidbodies(GameObject visual)
    {
        Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody nestedBody in rigidbodies)
        {
            if (nestedBody != null)
                Destroy(nestedBody);
        }
    }
}
