using UnityEngine;

[DisallowMultipleComponent]
public class DroppedItemSelectionHighlight : MonoBehaviour
{
    private const int SegmentCount = 48;

    private LineRenderer lineRenderer;
    private Material lineMaterial;

    void Awake()
    {
        EnsureLineRenderer();
    }

    void LateUpdate()
    {
        if (lineRenderer == null)
            return;

        Bounds bounds = GetCombinedBounds();
        float radius = Mathf.Max(0.18f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.2f);
        float y = bounds.min.y + 0.05f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        Color color = Color.Lerp(
            new Color(1f, 0.8f, 0.18f, 0.45f),
            new Color(1f, 0.95f, 0.55f, 0.92f),
            pulse
        );

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.widthMultiplier = Mathf.Lerp(0.04f, 0.07f, pulse);

        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = (i / (float)SegmentCount) * Mathf.PI * 2f;
            Vector3 point = new Vector3(
                bounds.center.x + Mathf.Cos(angle) * radius,
                y,
                bounds.center.z + Mathf.Sin(angle) * radius
            );
            lineRenderer.SetPosition(i, point);
        }
    }

    void OnEnable()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = true;
    }

    void OnDisable()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void EnsureLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;

        lineRenderer.loop = false;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = SegmentCount + 1;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.material = lineMaterial;
        lineRenderer.textureMode = LineTextureMode.Stretch;
    }

    private Bounds GetCombinedBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);

            return bounds;
        }

        return new Bounds(transform.position, Vector3.one * 0.35f);
    }

    void OnDestroy()
    {
        if (lineRenderer != null)
            Destroy(lineRenderer);

        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}
