using UnityEngine;

namespace OurGame.Core
{
    /// <summary>
    /// Central manager orchestrating high-level game state.
    /// Integrato con salvataggio/caricamento e sistema di tick.
    /// </summary>
    public class GameManager : SingletonMono<GameManager>
    {
        [Header("Save Settings")]
        [Tooltip("Salva il gioco ogni X tick di gioco")]
        public long autoSaveIntervalTicks = 60; // es. ogni 60 tick = 1 ora di gioco

        private long lastSaveTick = 0;

        private void Start()
        {
            // Carica il gioco all'avvio
            SaveManager.Instance.LoadGame();

            // Aggiorna lastSaveTick in base al tick corrente
            lastSaveTick = TimeManager.Instance.CurrentTick;
        }

        private void Update()
        {
            long currentTick = TimeManager.Instance.CurrentTick;

            // Controlla se è il momento di fare un autosave
            if (currentTick - lastSaveTick >= autoSaveIntervalTicks)
            {
                SaveGame();
                lastSaveTick = currentTick;
            }
        }

        /// <summary>
        /// Salva lo stato del gioco usando il SaveManager
        /// </summary>
        public void SaveGame()
        {
            Debug.Log("Autosaving game at tick " + TimeManager.Instance.CurrentTick);
            SaveManager.Instance.SaveGame();
        }

        /// <summary>
        /// Restituisce il giorno corrente di gioco (coerente con il TimeManager)
        /// </summary>
        public int GetCurrentDay()
        {
            return TimeManager.Instance.GetCurrentDay();
        }

        /// <summary>
        /// Esempio di avanzamento manuale giorno, utile per debug o eventi particolari
        /// </summary>
        public void AdvanceDay()
        {
            GameEvents.DayPassed();
        }

        private void OnApplicationQuit()
        {
            // Salvataggio finale quando si chiude il gioco
            SaveGame();
        }
    }
}