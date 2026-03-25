using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class BoxColliderHoverHighlight : MonoBehaviour
{
    private static readonly int[,] EdgeIndices = new int[,]
    {
        { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
        { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
        { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },
    };

    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.35f, 0.95f);
    [SerializeField] private float lineWidth = 0.020f;

    private BoxCollider targetCollider;
    private Transform lineRoot;
    private readonly LineRenderer[] edgeRenderers = new LineRenderer[12];
    private Material lineMaterial;

    public bool IsHighlighted { get; private set; }

    void Awake()
    {
        targetCollider = GetComponent<BoxCollider>();
        EnsureLineRenderers();
        SetHighlighted(false);
    }

    void LateUpdate()
    {
        if (!IsHighlighted || targetCollider == null)
            return;

        UpdateEdgePositions();
    }

    public void SetHighlighted(bool highlighted)
    {
        EnsureLineRenderers();
        IsHighlighted = highlighted;

        if (lineRoot != null)
            lineRoot.gameObject.SetActive(highlighted);

        if (highlighted)
            UpdateEdgePositions();
    }

    private void EnsureLineRenderers()
    {
        if (lineRoot != null)
            return;

        targetCollider = GetComponent<BoxCollider>();

        lineRoot = new GameObject("BoxColliderHighlight").transform;
        lineRoot.SetParent(transform, false);

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;

        for (int i = 0; i < edgeRenderers.Length; i++)
        {
            GameObject edgeObject = new GameObject($"Edge_{i:D2}");
            edgeObject.transform.SetParent(lineRoot, false);

            LineRenderer line = edgeObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.material = lineMaterial;
            line.startColor = highlightColor;
            line.endColor = highlightColor;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            edgeRenderers[i] = line;
        }
    }

    private void UpdateEdgePositions()
    {
        Vector3[] corners = GetWorldCorners();
        for (int i = 0; i < edgeRenderers.Length; i++)
        {
            LineRenderer line = edgeRenderers[i];
            if (line == null)
                continue;

            int startIndex = EdgeIndices[i, 0];
            int endIndex = EdgeIndices[i, 1];
            line.SetPosition(0, corners[startIndex]);
            line.SetPosition(1, corners[endIndex]);
        }
    }

    private Vector3[] GetWorldCorners()
    {
        Vector3 center = targetCollider.center;
        Vector3 halfSize = targetCollider.size * 0.5f;

        Vector3[] localCorners = new Vector3[8];
        localCorners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        localCorners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        localCorners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        localCorners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        localCorners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        localCorners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        localCorners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
        localCorners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

        for (int i = 0; i < localCorners.Length; i++)
            localCorners[i] = transform.TransformPoint(localCorners[i]);

        return localCorners;
    }

    void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}
