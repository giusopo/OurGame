using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Core
{
    public class GridFactory : SingletonMono<GridFactory>
    {
        [SerializeField] private FarmTile defaultTilePrefab;
        [SerializeField] private Transform runtimeGridContainer;

        public FarmTileGrid GetOrCreateGrid(FarmGridSaveData gridSave)
        {
            FarmTileGrid existingGrid = FarmGridManager.Instance.GetGrid(gridSave.gridId);
            if (existingGrid != null)
            {
                ConfigureExistingGrid(existingGrid, gridSave);
                return existingGrid;
            }

            return CreateGrid(gridSave);
        }

        public void DestroyRuntimeGridsExcept(ISet<string> allowedGridIds)
        {
            foreach (FarmTileGrid grid in FarmGridManager.Instance.GetAllGrids())
            {
                if (grid == null || !grid.IsRuntimeCreated)
                    continue;

                if (allowedGridIds != null && allowedGridIds.Contains(grid.gridId))
                    continue;

                Destroy(grid.gameObject);
            }
        }

        private void ConfigureExistingGrid(FarmTileGrid grid, FarmGridSaveData gridSave)
        {
            grid.transform.position = gridSave.worldPosition;
            grid.tileSize = gridSave.tileSize > 0f ? gridSave.tileSize : grid.tileSize;

            if (grid.tilePrefab == null)
                grid.tilePrefab = ResolveTilePrefab();
        }

        private FarmTileGrid CreateGrid(FarmGridSaveData gridSave)
        {
            FarmTile tilePrefab = ResolveTilePrefab();
            if (tilePrefab == null)
            {
                Debug.LogError(
                    "GridFactory could not resolve a tile prefab to recreate grid " + gridSave.gridId
                );
                return null;
            }

            GameObject gridObject = new GameObject($"RuntimeGrid_{gridSave.gridId}");
            if (runtimeGridContainer != null)
                gridObject.transform.SetParent(runtimeGridContainer, false);

            FarmTileGrid grid = gridObject.AddComponent<FarmTileGrid>();
            grid.InitializeRuntimeGrid(
                gridSave.gridId,
                gridSave.width,
                gridSave.height,
                gridSave.tileSize > 0f ? gridSave.tileSize : 1f,
                gridSave.worldPosition,
                tilePrefab
            );

            return grid;
        }

        private FarmTile ResolveTilePrefab()
        {
            if (defaultTilePrefab != null)
                return defaultTilePrefab;

            foreach (FarmTileGrid grid in FarmGridManager.Instance.GetAllGrids())
            {
                if (grid != null && grid.tilePrefab != null)
                    return grid.tilePrefab;
            }

            return null;
        }
    }
}
