using UnityEngine;
using OurGame.Core;

public class Plant : MonoBehaviour
{
    private GameObject currentVisual;
    public PlantData plantData;
    public long PlantTick => plantedTick;
    public int GrowthStage => growthStage;
    private long plantedTick;          // tick di piantagione
    private long growthTime;           // tempo totale in tick per maturazione
    private int growthStage = 0;

    private ScheduledEvent growthEvent; // riferimento all'evento schedulato

    public void PlantSeed(PlantData data, long currentTick)
    {
        plantData = data;
        plantedTick = currentTick;

        growthTime = plantData.GetGrowthTimeTicks();
        growthStage = 0;

        UpdateVisual();
        ScheduleNextGrowth();

        Debug.Log($"Planted {plantData.plantId} at tick {plantedTick}, growthTime={growthTime} ticks");

        PlantManager.Instance.RegisterPlant(this);
    }

    public void RestorePlant(PlantData data, long savedPlantedTick, int savedGrowthStage)
    {
        plantData = data;
        plantedTick = savedPlantedTick;
        // Assicura che lo stage di crescita sia valido
        growthStage = Mathf.Clamp(
            savedGrowthStage,
            0,
            plantData.growthStages.Length - 1
        );

        growthTime = plantData.GetGrowthTimeTicks();

        // Aggiorna la visuale corretta
        UpdateVisual();

        // Schedula il prossimo stage solo se non è completamente cresciuta
        if (growthStage < plantData.growthStages.Length - 1)
            ScheduleNextGrowth();

        PlantManager.Instance.RegisterPlant(this);
    }

    void ScheduleNextGrowth()
    {
        int stages = plantData.growthStages.Length;
        if (growthStage >= stages) return;

        long stageDuration = Mathf.CeilToInt((float)growthTime / stages);
        long nextTick = plantedTick + stageDuration * (growthStage + 1);

        // cancella l'event precedente se esiste
        if (growthEvent != null)
            growthEvent.Cancel();

        Debug.Log($"Scheduling growth for {plantData.plantId} at tick {nextTick}, growthStage={growthStage}");
        growthEvent = GameEventScheduler.Instance.Schedule(nextTick, ProcessGrowthEvent);
    }

    void ProcessGrowthEvent()
    {
        growthStage++;

        if (growthStage >= plantData.growthStages.Length)
        {
            growthStage = plantData.growthStages.Length - 1;
            return; // ultima crescita raggiunta
        }

        UpdateVisual();

        // schedula il prossimo stage
        ScheduleNextGrowth();
    }

    void UpdateVisual()
    {
        if (currentVisual != null)
            Destroy(currentVisual);

        currentVisual = Instantiate(
            plantData.growthStages[growthStage],
            transform.position,
            Quaternion.identity,
            transform
        );
    }

    public bool IsReadyToHarvest(long currentTick)
    {
        return (currentTick - plantedTick) >= growthTime;
    }

    public void Harvest(long currentTick)
    {
        if (!IsReadyToHarvest(currentTick))
            return;

        Debug.Log("Collected " + plantData.plantId);

        if (plantData.regrows)
        {
            long regrowTime = plantData.GetRegrowTimeTicks();
            plantedTick = currentTick - (growthTime - regrowTime);

            growthStage = 0;
            UpdateVisual();
            ScheduleNextGrowth();
        }
        else
        {
            // cancella evento pendente per evitare zombie
            if (growthEvent != null)
                growthEvent.Cancel();

            PlantManager.Instance.UnregisterPlant(this);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (growthEvent != null)
            growthEvent.Cancel();

        if (PlantManager.TryGetInstance(out var manager))
            manager.UnregisterPlant(this);
    }

}