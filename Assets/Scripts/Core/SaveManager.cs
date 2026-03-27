using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OurGame.Core.Domain;
using System.Linq;
using OurGame.Systems;

namespace OurGame.Core
{
    public class SaveManager : SingletonMono<SaveManager>
    {
        private const int WorldStateSaveVersion = 4;

        private string saveFile => Path.Combine(Application.persistentDataPath, "save.json");

        public void SaveGame()
        {
            SaveData data = new SaveData
            {
                saveVersion = WorldStateSaveVersion,
                currentTick = TimeManager.Instance.CurrentTick,
                grids = BuildGridSaveData(),
                backpack = BackpackInventorySystem.Instance.BuildSaveData()
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(saveFile, json);
            Debug.Log("Game Saved in: " + saveFile);
        }

        public void LoadGame()
        {
            Debug.Log("Loading game from: " + saveFile);

            if (!File.Exists(saveFile))
            {
                Debug.Log("No save file found.");
                return;
            }

            string json = File.ReadAllText(saveFile);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("Save file could not be parsed.");
                return;
            }

            if (data.grids == null || data.grids.Count == 0)
            {
                Debug.Log(
                    "Save file does not contain world-state grids. Skipping load for this save."
                );
                return;
            }

            if (data.saveVersion != 0 && data.saveVersion != WorldStateSaveVersion)
            {
                Debug.LogWarning(
                    $"Unexpected save version {data.saveVersion}. Attempting world-state load."
                );
            }

            TimeManager.Instance.SetCurrentTick(data.currentTick);

            PrepareWorldForLoad(data.grids);
            LoadWorldState(data);

            BackpackInventorySystem.Instance.LoadFromSave(data.backpack);

            Debug.Log("Game Loaded!");
        }

        private List<FarmGridSaveData> BuildGridSaveData()
        {
            List<FarmGridSaveData> grids = new List<FarmGridSaveData>();

            foreach (FarmTileGrid grid in FarmGridManager.Instance.GetAllGrids())
            {
                if (grid == null || grid.State == null)
                    continue;

                FarmGridState gridState = grid.State;
                FarmGridSaveData gridSave = new FarmGridSaveData
                {
                    gridId = gridState.gridId,
                    width = gridState.width,
                    height = gridState.height,
                    worldPosition = grid.transform.position,
                    tileSize = grid.tileSize
                };

                foreach (FarmTileState tileState in gridState.GetAllTiles())
                {
                    PlantState plantState = CopyPlantState(tileState.plantState);
                    gridSave.tiles.Add(new FarmTileSaveData
                    {
                        position = tileState.position,
                        hasPlant = plantState != null,
                        plant = plantState
                    });
                }

                grids.Add(gridSave);
            }

            return grids;
        }

        private void PrepareWorldForLoad(List<FarmGridSaveData> savedGrids)
        {
            HashSet<string> savedGridIds = new HashSet<string>();
            if (savedGrids != null)
            {
                foreach (FarmGridSaveData grid in savedGrids)
                {
                    if (grid != null && !string.IsNullOrWhiteSpace(grid.gridId))
                        savedGridIds.Add(grid.gridId);
                }
            }

            GridFactory.Instance.DestroyRuntimeGridsExcept(savedGridIds);

            foreach (FarmTileGrid grid in FarmGridManager.Instance.GetAllGrids())
            {
                if (grid == null)
                    continue;

                foreach (FarmTile tile in grid.GetAllTiles())
                    tile?.RemovePlant();
            }

            foreach (Plant plant in PlantManager.Instance.Plants.ToArray())
            {
                if (plant != null)
                    Destroy(plant.gameObject);
            }
        }

        private void LoadWorldState(SaveData data)
        {
            if (data.grids == null)
                return;

            foreach (FarmGridSaveData gridSave in data.grids)
            {
                if (gridSave == null || string.IsNullOrWhiteSpace(gridSave.gridId))
                    continue;

                FarmTileGrid grid = GridFactory.Instance.GetOrCreateGrid(gridSave);
                if (grid == null)
                    continue;

                FarmGridState restoredGridState = BuildGridState(gridSave);
                grid.ApplyState(restoredGridState);
                RestorePlantsFromGrid(grid, restoredGridState);
            }
        }

        private FarmGridState BuildGridState(FarmGridSaveData gridSave)
        {
            FarmGridState gridState = new FarmGridState(
                gridSave.gridId,
                gridSave.width,
                gridSave.height
            );

            if (gridSave.tiles == null)
                return gridState;

            foreach (FarmTileSaveData tileSave in gridSave.tiles)
            {
                FarmTileState tileState = gridState.GetTile(tileSave.position);
                if (tileState == null)
                    continue;

                if (!tileSave.hasPlant)
                {
                    tileState.RemovePlant();
                    continue;
                }

                tileState.plantState = CopyPlantState(tileSave.plant);
            }

            return gridState;
        }

        private void RestorePlantsFromGrid(FarmTileGrid grid, FarmGridState restoredGridState)
        {
            foreach (FarmTileState tileState in restoredGridState.GetAllTiles())
            {
                if (tileState == null || tileState.IsEmpty())
                    continue;

                FarmTile tile = grid.GetTile(tileState.position);
                if (tile == null)
                    continue;

                PlantData plantData = PlantDatabase.Instance.GetPlant(tileState.plantState.plantId);
                if (plantData == null)
                {
                    Debug.LogWarning(
                        "PlantData not found in database: " + tileState.plantState.plantId
                    );
                    continue;
                }

                GameObject plantGO = Instantiate(
                    plantData.plantPrefab,
                    tile.transform.position,
                    Quaternion.identity,
                    tile.transform
                );

                Plant plant = plantGO.GetComponent<Plant>();
                if (plant == null)
                {
                    Debug.LogError("Plant prefab missing Plant component: " + plantData.plantId);
                    Destroy(plantGO);
                    continue;
                }

                plant.RestorePlant(plantData, tileState.plantState, tile);
                tile.currentPlant = plant;
                tile.State?.SetPlant(plant.State);
            }
        }

        private PlantState CopyPlantState(PlantState source)
        {
            if (source == null || !source.IsValid())
                return null;

            PlantState copy = new PlantState(
                source.plantId,
                source.plantedTick,
                source.growthTime,
                source.MaxStage,
                source.regrows
            );
            copy.growthStage = source.growthStage;
            return copy;
        }
    }
}
