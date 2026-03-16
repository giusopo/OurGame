using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace OurGame.Core
{
    [System.Serializable]
    public class SaveData
    {
        public long currentTick;
        public List<PlantSaveData> plants = new List<PlantSaveData>();
    }

    public class SaveManager : SingletonMono<SaveManager>
    {
        private string saveFile => Path.Combine(Application.persistentDataPath, "save.json");

        public void SaveGame()
        {
            SaveData data = new SaveData
            {
                currentTick = TimeManager.Instance.CurrentTick
            };

            foreach (Plant plant in PlantManager.Instance.Plants)
            {
                data.plants.Add(new PlantSaveData
                {
                    plantId = plant.plantData.plantId,
                    plantedTick = plant.PlantTick,
                    growthStage = plant.GrowthStage,
                    tilePosition = plant.Tile.GridPosition,
                    gridId = plant.Tile.ParentGrid.gridId
                });
            }

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

            // Imposta il tick globale
            TimeManager.Instance.SetCurrentTick(data.currentTick);

            // distruggi eventuali piante in scena
            foreach (Plant plant in PlantManager.Instance.Plants.ToArray())
                Destroy(plant.gameObject);

            // Ricrea le piante
            foreach (PlantSaveData plantData in data.plants)
            {
                PlantData pData = PlantDatabase.Instance.GetPlant(plantData.plantId);

                if (pData == null)
                {
                    Debug.Log("pData è null");
                    Debug.LogWarning("PlantData not found in database: " + plantData.plantId);
                    continue;
                }

                Debug.Log("Restoring plant: " + plantData.plantId + " at grid " + plantData.gridId + " tile " + plantData.tilePosition);
                FarmTileGrid grid = FarmGridManager.Instance.GetGrid(plantData.gridId);
                FarmTile tile = grid?.GetTile(plantData.tilePosition);

                GameObject plantGO = Instantiate(
                    pData.plantPrefab,
                    tile.transform.position,
                    Quaternion.identity,
                    tile.transform
                );

                Plant plant = plantGO.GetComponent<Plant>();

                if (plant == null)
                {
                    Debug.Log("Plant prefab missing Plant component: " + pData.plantId);
                    continue;
                }

                plant.RestorePlant(
                    pData,
                    plantData.plantedTick,
                    plantData.growthStage,
                    tile
                );

                tile.currentPlant = plant;
            }

            Debug.Log("Game Loaded!");
        }
    }
}