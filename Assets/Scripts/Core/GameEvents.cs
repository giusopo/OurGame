using System;
using OurGame.Farming;

namespace OurGame.Core
{
    /// <summary>
    /// Central static event aggregator for the project.
    /// Add events here to decouple systems via pub-sub.
    /// </summary>
    public static class GameEvents
    {
        // Example event: crop planted
        public static event Action<CropData> OnCropPlanted;
        public static void CropPlanted(CropData data) => OnCropPlanted?.Invoke(data);

        // Add additional events below

        public static event Action OnDayPassed;
        public static void DayPassed() => OnDayPassed?.Invoke();
    }
}