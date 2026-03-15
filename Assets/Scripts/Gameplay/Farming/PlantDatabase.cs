using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Core
{
    public class PlantDatabase : SingletonMono<PlantDatabase>
    {
        [Header("All PlantData Assets")]
        public PlantData[] allPlants; // assegnali dall'inspector

        private Dictionary<string, PlantData> plantDict;

        protected override void Awake()
        {
            plantDict = new Dictionary<string, PlantData>();
            foreach (var plant in allPlants)
            {
                if (!string.IsNullOrEmpty(plant.plantId))
                    plantDict[plant.plantId] = plant;
                else
                    Debug.LogWarning("PlantData missing ID: " + plant.name);
            }
        }

        public PlantData GetPlant(string id)
        {
            if (plantDict.TryGetValue(id, out var data))
                return data;

            Debug.LogWarning("Plant not found in database: " + id);
            return null;
        }
    }
}