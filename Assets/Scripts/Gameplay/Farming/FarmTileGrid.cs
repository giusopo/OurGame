using System.Collections.Generic;
using UnityEngine;
using OurGame.Core.Domain;

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
        private FarmGridState state;

        private Dictionary<Vector2Int, FarmTile> tiles =
            new Dictionary<Vector2Int, FarmTile>();

        public FarmGridState State => state;
        public bool IsRuntimeCreated { get; private set; }

        void Awake()
        {
            if (string.IsNullOrWhiteSpace(gridId) || width <= 0 || height <= 0)
                return;

            InitializeGrid(new FarmGridState(gridId, width, height));
        }

        public void ApplyState(FarmGridState newState)
        {
            if (newState == null)
                newState = new FarmGridState(gridId, width, height);

            InitializeGrid(newState);
        }

        public void InitializeRuntimeGrid(
            string newGridId,
            int newWidth,
            int newHeight,
            float newTileSize,
            Vector3 worldPosition,
            FarmTile runtimeTilePrefab
        )
        {
            gridId = newGridId;
            width = newWidth;
            height = newHeight;
            tileSize = newTileSize;
            tilePrefab = runtimeTilePrefab;
            transform.position = worldPosition;
            IsRuntimeCreated = true;

            InitializeGrid(new FarmGridState(gridId, width, height));
        }

        private void CacheTiles()
        {
            tiles.Clear();
            FarmTile[] allTiles = GetComponentsInChildren<FarmTile>(true);
            foreach (var tile in allTiles)
            {
                if (tiles.ContainsKey(tile.GridPosition))
                {
                    Debug.LogError(
                        $"Duplicate tile in grid {gridId}: {tile.GridPosition}"
                    );
                    continue;
                }

                tiles[tile.GridPosition] = tile;
            }
        }

        private void InitializeGrid(FarmGridState newState)
        {
            state = newState;
            gridId = state.gridId;
            width = state.width;
            height = state.height;

            CacheTiles();
            EnsureRuntimeTilesMatchState();
            BindTilesToState();
            RegisterIfNeeded();

            if (tiles.Count != width * height)
            {
                Debug.LogWarning(
                    $"Grid {gridId} mismatch: expected {width * height}, found {tiles.Count}"
                );
            }
        }

        private void EnsureRuntimeTilesMatchState()
        {
            if (tiles.Count == width * height)
                return;

            if (tilePrefab == null)
            {
                Debug.LogWarning("Cannot rebuild grid without tilePrefab: " + gridId);
                return;
            }

            RebuildRuntimeTiles();
            CacheTiles();
        }

        private void RebuildRuntimeTiles()
        {
            Terrain terrain = Terrain.activeTerrain;

            if (terrain == null)
            {
                Debug.LogError("No active Terrain found!");
                return;
            }

            Vector3 terrainPos = terrain.transform.position;
            TerrainData data = terrain.terrainData;

            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    FarmTile tile = Instantiate(tilePrefab, transform);

                    tile.GridPosition = new Vector2Int(x, y);
                    tile.name = $"Tile_{x}_{y}";

                    // 🌍 posizione base griglia
                    float worldX = transform.position.x + (x + 0.5f) * tileSize;
                    float worldZ = transform.position.z + (y + 0.5f) * tileSize;

                    // ⛰️ controllo dentro terrain
                    if (worldX < terrainPos.x ||
                        worldX > terrainPos.x + data.size.x ||
                        worldZ < terrainPos.z ||
                        worldZ > terrainPos.z + data.size.z)
                    {
                        continue;
                    }

                    // 📏 altezza terrain (come RandomSpawner)
                    float height = terrain.SampleHeight(new Vector3(worldX, 0, worldZ))
                                + terrainPos.y;

                    Vector3 pos = new Vector3(worldX, height, worldZ);

                    tile.transform.position = pos;

                    // 🧭 allineamento alla normale (come AlignToTerrain)
                    if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f))
                    {
                        Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                        tile.transform.rotation = rot;
                    }
                }
            }
        }

        private void BindTilesToState()
        {
            foreach (var tile in tiles.Values)
            {
                var tileState = state.GetTile(tile.GridPosition);
                if (tileState == null)
                {
                    Debug.LogWarning(
                        $"Tile {tile.GridPosition} in grid {gridId} has no matching domain state"
                    );
                }

                tile.Initialize(tileState, this);
            }
        }

        private void RegisterIfNeeded()
        {
            FarmTileGrid registeredGrid = FarmGridManager.Instance.GetGrid(gridId);
            if (registeredGrid == this)
                return;

            if (registeredGrid != null)
            {
                Debug.LogError("Duplicate grid registration attempted: " + gridId);
                return;
            }

            FarmGridManager.Instance.RegisterGrid(this);
        }

        public FarmTile GetTile(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
            {
                Debug.LogWarning($"Out of bounds tile: {pos}");
                return null;
            }

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

        void OnDestroy()
        {
            if (FarmGridManager.TryGetInstance(out var manager))
                manager.UnregisterGrid(this);
        }
    }
}
