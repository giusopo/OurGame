using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Core
{
    public class FarmTileGrid : MonoBehaviour
    {
        public string gridId;
        public Vector3 origin => transform.position;

        [Header("Grid Settings")]
        public int width;
        public int height;
        public float tileSize = 1f;

        [Header("Grid Preview")]
        public bool showGridPreview = true;
        public Color gridColor = Color.green;

        public FarmTile tilePrefab;

        private Dictionary<Vector2Int, FarmTile> tiles =
            new Dictionary<Vector2Int, FarmTile>();

        void Awake()
        {
            FarmTile[] allTiles = GetComponentsInChildren<FarmTile>(true);

            foreach (var tile in allTiles)
            {
                tile.ParentGrid = this;

                if (tiles.ContainsKey(tile.GridPosition))
                {
                    Debug.LogError(
                        $"Duplicate tile in grid {gridId}: {tile.GridPosition}"
                    );
                    continue;
                }

                tiles[tile.GridPosition] = tile;
            }

            FarmGridManager.Instance.RegisterGrid(this);
        }

        public FarmTile GetTile(Vector2Int pos)
        {
            tiles.TryGetValue(pos, out var tile);
            return tile;
        }

        public IEnumerable<FarmTile> GetAllTiles()
        {
            return tiles.Values;
        }

        void OnDrawGizmos()
        {
            if (!showGridPreview)
                return;

            Gizmos.color = gridColor;

            Vector3 origin = transform.position;

            for (int x = 0; x <= width; x++)
            {
                Vector3 start = origin + new Vector3(x * tileSize, 0, 0);
                Vector3 end = origin + new Vector3(x * tileSize, 0, height * tileSize);

                Gizmos.DrawLine(start, end);
            }

            for (int y = 0; y <= height; y++)
            {
                Vector3 start = origin + new Vector3(0, 0, y * tileSize);
                Vector3 end = origin + new Vector3(width * tileSize, 0, y * tileSize);

                Gizmos.DrawLine(start, end);
            }
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos)
        {
            return transform.position +
                new Vector3(
                    (gridPos.x + 0.5f) * tileSize,
                    0,
                    (gridPos.y + 0.5f) * tileSize
                );
        }
    }
}