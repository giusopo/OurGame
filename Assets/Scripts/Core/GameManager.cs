using UnityEngine;

namespace OurGame.Core
{
    /// <summary>
    /// Central manager orchestrating high-level game state.
    /// Should persist between scenes and initialize subsystems.
    /// </summary>
    public class GameManager : SingletonMono<GameManager>
    {
        public int currentDay = 1;

        private void Start()
        {
            // initialize other managers as needed
            // e.g. AudioManager.Instance.Init();
        }

        public void AdvanceDay()
        {
            currentDay++;
            GameEvents.DayPassed();
        }
    }
}