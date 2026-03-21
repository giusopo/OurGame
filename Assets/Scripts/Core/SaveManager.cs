using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OurGame.Core.Domain;
using System.Linq;

namespace OurGame.Core
{
    public class SaveManager : SingletonMono<SaveManager>
    {
        private const int WorldStateSaveVersion = 2;

        private string saveFile => Path.Combine(Application.persistentDataPath, "save.json");

        public void SaveGame()
        {
            SaveData data = new SaveData
            {
                saveVersion = WorldStateSaveVersion,
                currentTick = TimeManager.Instance.CurrentTick,
                grids = BuildGridSaveData()
            };

            SaveInventory(data);

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
                Debug.LogWarning(
                    "Save file does not contain world-state grids and is incompatible with the current save system."
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

            InventorySystem.Instance.LoadFromSave(
                data.inventorySlots,
                data.hotbarSlots,
                data.selectedHotbarIndex
            );

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
                    gridSave.tiles.Add(new FarmTileSaveData
                    {
                        position = tileState.position,
                        plant = CopyPlantState(tileState.plantState)
                    });
                }

                grids.Add(gridSave);
            }

            return grids;
        }

        private void SaveInventory(SaveData data)
        {
            foreach (InventorySlotData slot in InventorySystem.Instance.MainSlots)
            {
                data.inventorySlots.Add(new InventorySlotSaveData
                {
                    itemId = slot.IsEmpty ? string.Empty : slot.Item.ItemId,
                    quantity = slot.Quantity
                });
            }

            foreach (InventorySlotData slot in InventorySystem.Instance.HotbarSlots)
            {
                data.hotbarSlots.Add(new InventorySlotSaveData
                {
                    itemId = slot.IsEmpty ? string.Empty : slot.Item.ItemId,
                    quantity = slot.Quantity
                });
            }

            data.selectedHotbarIndex = InventorySystem.Instance.SelectedHotbarIndex;
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

                tileState.plantState = CopyPlantState(tileSave.plant);
            }

            return gridState;
        }

        private void RestorePlantsFromGrid(FarmTileGrid grid, FarmGridState restoredGridState)
        {
            foreach (FarmTileState tileState in restoredGridState.GetAllTiles())
            {
                if (tileState == null || tileState.plantState == null)
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
            if (source == null)
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
