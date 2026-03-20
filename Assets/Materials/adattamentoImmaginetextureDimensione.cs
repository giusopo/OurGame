using UnityEngine;

public class AutoTextureTiling : MonoBehaviour
{
    public float tilingFactor = 1f;

    void Update()
    {
        Renderer rend = GetComponent<Renderer>();

        if (rend != null)
        {
            Vector3 scale = transform.localScale;

            rend.material.mainTextureScale = new Vector2(
                scale.x * tilingFactor,
                scale.z * tilingFactor
            );
        }
    }
}