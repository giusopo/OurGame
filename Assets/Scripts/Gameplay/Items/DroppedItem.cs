using System.Collections.Generic;
using UnityEngine;
using OurGame.Core;

[DisallowMultipleComponent]
public class DroppedItem : MonoBehaviour
{
    [SerializeField] private float pickupCooldownSeconds = 0.6f;
    [SerializeField] private float throwForce = 4.5f;
    [SerializeField] private float upwardForce = 2.25f;
    [SerializeField] private float pickupRadius = 1.15f;
    [SerializeField] private float collisionIgnoreDurationSeconds = 0.6f;
    [SerializeField] private float fallbackColliderRadius = 0.2f;

    private InventoryItemDefinition item;
    private int quantity;
    private Rigidbody physicsBody;
    private SphereCollider pickupTrigger;
    private float pickupBlockedUntil;
    private float restoreIgnoredCollisionsAt = -1f;
    private readonly List<Collider> ignoredPlayerColliders = new List<Collider>();
    private readonly List<Collider> ownedPhysicsColliders = new List<Collider>();

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

        GameObject droppedObject = CreateDroppedObject(item, position);
        if (droppedObject == null)
            return null;

        DroppedItem droppedItem = droppedObject.GetComponent<DroppedItem>();
        if (droppedItem == null)
            droppedItem = droppedObject.AddComponent<DroppedItem>();

        droppedObject.name = $"Dropped_{item.DisplayName}";
        droppedItem.Initialize(item, quantity);
        droppedItem.Throw(position, forward);
        return droppedItem;
    }

    void Update()
    {
        if (restoreIgnoredCollisionsAt < 0f || Time.time < restoreIgnoredCollisionsAt)
            return;

        RestoreIgnoredCollisions();
    }

    public void Initialize(InventoryItemDefinition itemDefinition, int stackQuantity)
    {
        item = itemDefinition;
        quantity = Mathf.Max(1, stackQuantity);
        pickupBlockedUntil = Time.time + pickupCooldownSeconds;

        physicsBody = GetComponent<Rigidbody>();
        if (physicsBody == null)
            physicsBody = gameObject.AddComponent<Rigidbody>();

        physicsBody.mass = 1f;
        physicsBody.isKinematic = false;
        physicsBody.interpolation = RigidbodyInterpolation.Interpolate;
        physicsBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        EnsureSupportedPhysicsColliders();
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

    public void IgnoreCollisionsWith(Collider[] playerColliders, float? durationOverrideSeconds = null)
    {
        if (playerColliders == null || playerColliders.Length == 0 || ownedPhysicsColliders.Count == 0)
            return;

        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null || playerCollider.isTrigger)
                continue;

            foreach (Collider itemCollider in ownedPhysicsColliders)
            {
                if (itemCollider == null || itemCollider.isTrigger)
                    continue;

                Physics.IgnoreCollision(itemCollider, playerCollider, true);
            }

            if (!ignoredPlayerColliders.Contains(playerCollider))
                ignoredPlayerColliders.Add(playerCollider);
        }

        if (ignoredPlayerColliders.Count > 0)
        {
            float duration = durationOverrideSeconds ?? collisionIgnoreDurationSeconds;
            restoreIgnoredCollisionsAt = Time.time + Mathf.Max(0.05f, duration);
        }
    }

    private void Throw(Vector3 position, Vector3 forward)
    {
        transform.position = position;

        Vector3 launchDirection = forward.sqrMagnitude > 0.001f
            ? forward.normalized
            : Vector3.forward;

        physicsBody.position = position;
        physicsBody.linearVelocity = Vector3.zero;
        physicsBody.angularVelocity = Vector3.zero;
        physicsBody.AddForce(
            launchDirection * throwForce + Vector3.up * upwardForce,
            ForceMode.Impulse
        );
    }

    private void EnsureSupportedPhysicsColliders()
    {
        ownedPhysicsColliders.Clear();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            if (col == null || col.isTrigger)
                continue;

            if (col is MeshCollider meshCollider && !meshCollider.convex)
            {
                meshCollider.enabled = false;
                continue;
            }

            ownedPhysicsColliders.Add(col);
        }

        if (ownedPhysicsColliders.Count > 0)
            return;

        SphereCollider fallbackCollider = gameObject.GetComponent<SphereCollider>();
        if (fallbackCollider == null || fallbackCollider.isTrigger)
            fallbackCollider = gameObject.AddComponent<SphereCollider>();

        fallbackCollider.isTrigger = false;
        fallbackCollider.radius = ComputeFallbackRadius();
        fallbackCollider.center = Vector3.zero;
        ownedPhysicsColliders.Add(fallbackCollider);
    }

    private void EnsurePickupTrigger()
    {
        Transform existingTrigger = transform.Find("PickupTrigger");
        GameObject triggerObject = existingTrigger != null
            ? existingTrigger.gameObject
            : new GameObject("PickupTrigger");

        triggerObject.transform.SetParent(transform, false);
        triggerObject.transform.localPosition = Vector3.zero;

        pickupTrigger = triggerObject.GetComponent<SphereCollider>();
        if (pickupTrigger == null)
            pickupTrigger = triggerObject.AddComponent<SphereCollider>();

        pickupTrigger.isTrigger = true;
        pickupTrigger.radius = pickupRadius;
    }

    private float ComputeFallbackRadius()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return fallbackColliderRadius;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        float maxExtent = Mathf.Max(
            combinedBounds.extents.x,
            combinedBounds.extents.y,
            combinedBounds.extents.z
        );

        return Mathf.Max(fallbackColliderRadius, maxExtent);
    }

    private void RestoreIgnoredCollisions()
    {
        if (ownedPhysicsColliders.Count > 0)
        {
            foreach (Collider playerCollider in ignoredPlayerColliders)
            {
                if (playerCollider == null)
                    continue;

                foreach (Collider itemCollider in ownedPhysicsColliders)
                {
                    if (itemCollider != null)
                        Physics.IgnoreCollision(itemCollider, playerCollider, false);
                }
            }
        }

        ignoredPlayerColliders.Clear();
        restoreIgnoredCollisionsAt = -1f;
    }

    void OnDestroy()
    {
        RestoreIgnoredCollisions();
    }

    private static GameObject CreateDroppedObject(InventoryItemDefinition item, Vector3 position)
    {
        if (item.WorldDropPrefab != null)
            return Instantiate(item.WorldDropPrefab, position, Quaternion.identity);

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.name = "FallbackVisual";
        fallback.transform.position = position;

        Renderer renderer = fallback.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = item.Tint;

        return fallback;
    }
}
