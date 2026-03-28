using System;
using System.Collections.Generic;
using UnityEngine;
using OurGame.Systems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerPocketHoverHighlighter : MonoBehaviour
{
    private const string HologramObjectName = "AstronautBackpackHologram";

    [SerializeField] private Transform hologramRoot;
    [SerializeField] private BackpackDefinition backpackDefinition;
    [SerializeField] private Camera hoverCamera;
    [SerializeField] private LayerMask pocketRaycastMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float rayDistance = 15f;

    private readonly List<HologramPieceHoverReveal> pocketReveals = new List<HologramPieceHoverReveal>();
    private readonly Dictionary<Collider, HologramPieceHoverReveal> colliderToReveal =
        new Dictionary<Collider, HologramPieceHoverReveal>();
    private readonly Dictionary<HologramPieceHoverReveal, string> revealToPocketName =
        new Dictionary<HologramPieceHoverReveal, string>();

    private HologramPieceHoverReveal currentReveal;
    private string currentHoveredPocketName;

    public string CurrentHoveredPocketName => currentHoveredPocketName;

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
        CachePocketReveals();
    }

    void LateUpdate()
    {
        RefreshPocketRevealsIfNeeded();

        if (!ShouldProcessHover())
        {
            SetCurrentReveal(null);
            return;
        }

        Camera activeCamera = ResolveCamera();
        if (activeCamera == null)
        {
            SetCurrentReveal(null);
            return;
        }

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        HologramPieceHoverReveal hoveredReveal = null;

        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                pocketRaycastMask,
                QueryTriggerInteraction.Collide
            )
        )
        {
            hoveredReveal = ResolveReveal(hit.collider);
        }

        SetCurrentReveal(hoveredReveal);
    }

    private bool ShouldProcessHover()
    {
        bool inventoryOpen = BackpackInventorySystem.Instance != null && BackpackInventorySystem.Instance.IsInventoryOpen;
        return inventoryOpen || Cursor.visible || Input.GetMouseButton(1);
    }

    private void CachePocketReveals()
    {
        AutoAssignReferences();
        pocketReveals.Clear();
        colliderToReveal.Clear();
        revealToPocketName.Clear();

        foreach (BackpackPocketDefinition definition in GetPocketDefinitions())
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.PocketName))
                continue;

            string pocketName = definition.PocketName;
            Transform pocketTransform = FindPocketTransform(pocketName);
            if (pocketTransform == null)
                continue;

            Collider pieceCollider = pocketTransform.GetComponent<Collider>();
            if (pieceCollider == null)
                continue;

            HologramPieceHoverReveal reveal =
                pocketTransform.GetComponent<HologramPieceHoverReveal>();
            if (reveal == null)
                reveal = pocketTransform.gameObject.AddComponent<HologramPieceHoverReveal>();

            reveal.SetVisible(false);
            pocketReveals.Add(reveal);
            colliderToReveal[pieceCollider] = reveal;
            revealToPocketName[reveal] = pocketName;
        }
    }

    private HologramPieceHoverReveal ResolveReveal(Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        if (colliderToReveal.TryGetValue(hitCollider, out HologramPieceHoverReveal cachedReveal))
            return cachedReveal;

        HologramPieceHoverReveal reveal = hitCollider.GetComponent<HologramPieceHoverReveal>();
        if (reveal == null)
            reveal = hitCollider.GetComponentInParent<HologramPieceHoverReveal>();

        if (!IsKnownPocketReveal(reveal))
            return null;

        colliderToReveal[hitCollider] = reveal;
        return reveal;
    }

    private Transform FindPocketTransform(string pocketName)
    {
        Transform searchRoot = hologramRoot != null ? hologramRoot : transform;
        return FindDescendantByName(searchRoot, pocketName);
    }

    private static Transform FindDescendantByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform nested = FindDescendantByName(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private Camera ResolveCamera()
    {
        if (hoverCamera != null && hoverCamera.isActiveAndEnabled)
            return hoverCamera;

        hoverCamera = Camera.main;
        if (hoverCamera != null && hoverCamera.isActiveAndEnabled)
            return hoverCamera;

        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            hoverCamera = cameraController.GetComponent<Camera>();
            if (hoverCamera != null && hoverCamera.isActiveAndEnabled)
                return hoverCamera;
        }

        AstronautThirdPersonCamera astronautCamera = FindFirstObjectByType<AstronautThirdPersonCamera>();
        if (astronautCamera != null)
        {
            hoverCamera = astronautCamera.GetComponent<Camera>();
            if (hoverCamera != null && hoverCamera.isActiveAndEnabled)
                return hoverCamera;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled)
            {
                hoverCamera = cameras[i];
                return hoverCamera;
            }
        }

        return hoverCamera;
    }

    private void AutoAssignReferences()
    {
        if (backpackDefinition == null)
            backpackDefinition = FindFirstObjectByType<BackpackDefinition>();

        if (hologramRoot == null && backpackDefinition != null && backpackDefinition.HologramRoot != null)
            hologramRoot = backpackDefinition.HologramRoot;

        if (hologramRoot == null)
            hologramRoot = FindDescendantByName(transform, HologramObjectName);
    }

    private void RefreshPocketRevealsIfNeeded()
    {
        List<BackpackPocketDefinition> definitions = GetPocketDefinitions();
        if (definitions.Count != pocketReveals.Count)
        {
            CachePocketReveals();
            return;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            if (!revealToPocketName.ContainsValue(definitions[i].PocketName))
            {
                CachePocketReveals();
                return;
            }
        }
    }

    private List<BackpackPocketDefinition> GetPocketDefinitions()
    {
        if (BackpackInventorySystem.Instance != null && BackpackInventorySystem.Instance.PocketDefinitions.Count > 0)
            return new List<BackpackPocketDefinition>(BackpackInventorySystem.Instance.PocketDefinitions);

        AutoAssignReferences();
        if (backpackDefinition != null)
            return backpackDefinition.GetDefinitionsSnapshot();

        List<BackpackPocketDefinition> fallback = new List<BackpackPocketDefinition>();
        for (int i = 0; i < PocketNames.Ordered.Length; i++)
            fallback.Add(BackpackPocketDefinition.CreateDefault(PocketNames.Ordered[i]));

        return fallback;
    }

    private bool IsKnownPocketReveal(HologramPieceHoverReveal reveal)
    {
        return reveal != null && pocketReveals.Contains(reveal);
    }

    private void SetCurrentReveal(HologramPieceHoverReveal nextReveal)
    {
        if (currentReveal == nextReveal)
            return;

        if (currentReveal != null)
            currentReveal.SetVisible(false);

        currentReveal = nextReveal;
        currentHoveredPocketName = currentReveal != null && revealToPocketName.TryGetValue(currentReveal, out string pocketName)
            ? pocketName
            : null;

        if (currentReveal != null)
            currentReveal.SetVisible(true);
    }

    void OnDisable()
    {
        SetCurrentReveal(null);
    }
}
