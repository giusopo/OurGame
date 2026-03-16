using UnityEngine;
using UnityEditor;
using OurGame.Core;

[CustomEditor(typeof(FarmTileGrid))]
public class FarmTileGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FarmTileGrid grid = (FarmTileGrid)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Grid"))
        {
            GenerateGrid(grid);
        }

        if (GUILayout.Button("Clear Grid"))
        {
            ClearGrid(grid);
        }
    }

    void GenerateGrid(FarmTileGrid grid)
    {
        if (grid.tilePrefab == null)
        {
            Debug.LogError("Tile Prefab missing!");
            return;
        }

        ClearGrid(grid);

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                Vector3 pos = new Vector3(
                    x * grid.tileSize,
                    0,
                    y * grid.tileSize
                );

                FarmTile tile = (FarmTile)PrefabUtility.InstantiatePrefab(grid.tilePrefab);

                tile.transform.SetParent(grid.transform);
                tile.transform.localPosition = pos;

                tile.GridPosition = new Vector2Int(x, y);

                tile.name = $"Tile_{x}_{y}";
            }
        }

        Debug.Log("Grid Generated");
    }

    void ClearGrid(FarmTileGrid grid)
    {
        for (int i = grid.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(grid.transform.GetChild(i).gameObject);
        }

        Debug.Log("Grid Cleared");
    }
}