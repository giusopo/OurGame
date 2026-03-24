using UnityEngine;

[AddComponentMenu("OurGame/Tools/Terrain Detail Cleaner")]
public class TerrainDetailCleaner : MonoBehaviour
{
    [Header("Cleaning Settings")]
    [Min(1f)]
    public float radius = 3f;

    public bool clearOnStart = true;

    void Start()
    {
        if (clearOnStart)
            Clear();
    }

    public void Clear()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogWarning("TerrainDetailCleaner requires an active Terrain to clear details.");
            return;
        }

        TerrainData data = terrain.terrainData;

        Vector3 tPos = terrain.transform.position;
        Vector3 wPos = transform.position;

        // 🎯 conversione world → detail map
        float normX = (wPos.x - tPos.x) / data.size.x;
        float normZ = (wPos.z - tPos.z) / data.size.z;

        int mapX = Mathf.RoundToInt(normX * (data.detailWidth - 1));
        int mapZ = Mathf.RoundToInt(normZ * (data.detailHeight - 1));

        int r = Mathf.RoundToInt(radius);
        mapX = Mathf.Clamp(mapX, r, Mathf.Max(r, data.detailWidth - r - 1));
        mapZ = Mathf.Clamp(mapZ, r, Mathf.Max(r, data.detailHeight - r - 1));

        int size = r * 2 + 1;
        int[,] empty = new int[size, size];

        // 🌿 crea mask circolare (NON quadrata)
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float dx = x - r;
                float dz = z - r;

                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist <= r)
                    empty[x, z] = 0;
            }
        }

        // 🌍 applica su tutti i layer
        for (int layer = 0; layer < data.detailPrototypes.Length; layer++)
        {
            data.SetDetailLayer(
                mapX - r,
                mapZ - r,
                layer,
                empty
            );
        }
    }

    void OnValidate()
    {
        radius = Mathf.Max(1f, radius);
    }
}
