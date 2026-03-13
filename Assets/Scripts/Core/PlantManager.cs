using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Core
{
    public class PlantManager : SingletonMono<PlantManager>
    {
        private List<Plant> plants = new List<Plant>();

        public void RegisterPlant(Plant plant)
        {
            if (!plants.Contains(plant))
                plants.Add(plant);
        }

        public void UnregisterPlant(Plant plant)
        {
            plants.Remove(plant);
        }

        public IReadOnlyList<Plant> Plants => plants;
    }
}