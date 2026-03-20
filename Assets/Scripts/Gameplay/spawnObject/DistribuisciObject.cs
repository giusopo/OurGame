using UnityEngine;
using System.Collections.Generic;

public class RandomSpawner : MonoBehaviour
{
    [Header("References")]
    public Terrain terrain;
    public GameObject prefab;

    [Header("Spawn Settings")]
    public int count = 100;
    public float spawnRadius = 50f;
    public float minDistance = 2f;

    [Header("Scale Settings")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public bool uniformScaling = true;

    [Header("Collision Settings")]
    public bool FornisciCollisioni;
    public LayerMask collisionMask;

    [Header("Optional")]
    public bool alignToSurface = true;
    public float maxSlope = 35f;
    public float yOffset = 0f;

    private List<Vector3> positions = new List<Vector3>();

    void Start()
    {
        if (terrain == null || prefab == null)
        {
            Debug.LogError("Terrain o Prefab non assegnati!");
            return;
        }

        Vector3 terrainPos = terrain.transform.position;
        TerrainData data = terrain.terrainData;

        int attempts = 0;

        while (positions.Count < count && attempts < count * 10)
        {
            attempts++;

            Vector2 random = Random.insideUnitCircle * spawnRadius;

            float worldX = transform.position.x + random.x;
            float worldZ = transform.position.z + random.y;

            if (!IsInsideTerrain(worldX, worldZ, terrainPos, data))
                continue;

            float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

            Vector3 pos = new Vector3(worldX, y + yOffset, worldZ);

            if (!IsSlopeValid(pos))
                continue;

            if (!IsValidPosition(pos))
                continue;

            positions.Add(pos);
            Spawn(pos);
        }

        Debug.Log($"Spawn completato: {positions.Count}/{count}");
    }

    // --------------------------
    // SPAWN
    // --------------------------
    void Spawn(Vector3 pos)
    {
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

        // rotazione casuale
        obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        // scala casuale
        ApplyRandomScale(obj);

        // allineamento al terreno
        if (alignToSurface)
            AlignToTerrain(obj);
    }

    // --------------------------
    // SCALA CASUALE
    // --------------------------
    void ApplyRandomScale(GameObject obj)
    {
        if (uniformScaling)
        {
            float scale = Random.Range(minScale, maxScale);
            obj.transform.localScale = Vector3.one * scale;
        }
        else
        {
            float scaleX = Random.Range(minScale, maxScale);
            float scaleY = Random.Range(minScale, maxScale);
            float scaleZ = Random.Range(minScale, maxScale);

            obj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }
    }

    // --------------------------
    // VALIDAZIONE POSIZIONE
    // --------------------------
    bool IsValidPosition(Vector3 pos)
    {
        foreach (var p in positions)
        {
            if (Vector3.Distance(p, pos) < minDistance)
                return false;
        }

        if (Physics.CheckSphere(pos, minDistance, collisionMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    // --------------------------
    // CONTROLLO TERRAIN
    // --------------------------
    bool IsInsideTerrain(float worldX, float worldZ, Vector3 terrainPos, TerrainData data)
    {
        return worldX >= terrainPos.x &&
               worldX <= terrainPos.x + data.size.x &&
               worldZ >= terrainPos.z &&
               worldZ <= terrainPos.z + data.size.z;
    }

    // --------------------------
    // ALLINEAMENTO AL TERRENO
    // --------------------------
    void AlignToTerrain(GameObject obj)
    {
        RaycastHit hit;

        if (Physics.Raycast(obj.transform.position + Vector3.up * 10f, Vector3.down, out hit, 50f))
        {
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            obj.transform.rotation = rot * obj.transform.rotation;
        }
    }

    // --------------------------
    // CONTROLLO PENDENZA
    // --------------------------
    bool IsSlopeValid(Vector3 pos)
    {
        RaycastHit hit;

        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out hit, 50f))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            return angle <= maxSlope;
        }

        return false;
    }

    // --------------------------
    // DEBUG VISIVO
    // --------------------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}