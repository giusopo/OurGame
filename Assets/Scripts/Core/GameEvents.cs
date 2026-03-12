using System;

namespace OurGame.Core
{
    /// <summary>
    /// Central static event aggregator for the project.
    /// Add events here to decouple systems via pub-sub.
    /// </summary>
    public static class GameEvents
    {
        // crop planted
        public static event Action<PlantData> OnPlantPlanted;
        public static void PlantPlanted(PlantData data) => OnPlantPlanted?.Invoke(data);

        // crop harvested
        public static event Action<PlantData> OnPlantHarvested;
        public static void PlantHarvested(PlantData data) => OnPlantHarvested?.Invoke(data);

        // day progression
        public static event Action OnDayPassed;
        public static void DayPassed() => OnDayPassed?.Invoke();
    }
}