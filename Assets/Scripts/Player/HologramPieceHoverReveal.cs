using UnityEngine;

[DisallowMultipleComponent]
public class HologramPieceHoverReveal : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    void Awake()
    {
        CacheRenderer();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        CacheRenderer();
        if (targetRenderer != null)
            targetRenderer.enabled = visible;
    }

    private void CacheRenderer()
    {
        if (targetRenderer != null)
            return;

        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>(true);
    }
}
