using UnityEngine;
using OurGame.Core;

public class Plant : MonoBehaviour
{
    private GameObject currentVisual;

    public PlantData plantData;

    public FarmTile Tile { get; private set; }

    public long PlantTick => plantedTick;
    public int GrowthStage => growthStage;

    private long plantedTick;
    private long growthTime;
    private int growthStage = 0;

    private ScheduledEvent growthEvent;

    public void PlantSeed(PlantData data, long currentTick, FarmTile tile)
    {
        plantData = data;
        plantedTick = currentTick;
        Tile = tile;

        growthTime = plantData.GetGrowthTimeTicks();
        growthStage = 0;

        UpdateVisual();
        ScheduleNextGrowth();

        Debug.Log($"Planted {plantData.plantId} at tick {plantedTick}, growthTime={growthTime}");

        Debug.Log("Spawning plant prefab: " + plantData.plantPrefab);
        PlantManager.Instance.RegisterPlant(this);
    }

    public void RestorePlant(PlantData data, long savedPlantedTick, int savedGrowthStage, FarmTile tile)
    {
        plantData = data;
        plantedTick = savedPlantedTick;
        Tile = tile;

        growthStage = Mathf.Clamp(
            savedGrowthStage,
            0,
            plantData.growthStages.Length - 1
        );

        growthTime = plantData.GetGrowthTimeTicks();

        UpdateVisual();

        if (growthStage < plantData.growthStages.Length - 1)
            ScheduleNextGrowth();

        Debug.Log("Spawning plant prefab: " + plantData.plantPrefab);
        PlantManager.Instance.RegisterPlant(this);
    }

    void ScheduleNextGrowth()
    {
        int stages = plantData.growthStages.Length;
        if (growthStage >= stages) return;

        long stageDuration = Mathf.CeilToInt((float)growthTime / stages);
        long nextTick = plantedTick + stageDuration * (growthStage + 1);

        if (growthEvent != null)
            growthEvent.Cancel();

        Debug.Log($"Scheduling growth for {plantData.plantId} at tick {nextTick}, stage={growthStage}");

        growthEvent = GameEventScheduler.Instance.Schedule(nextTick, ProcessGrowthEvent);
    }

    void ProcessGrowthEvent()
    {
        growthStage++;

        if (growthStage >= plantData.growthStages.Length)
        {
            growthStage = plantData.growthStages.Length - 1;
            return;
        }

        UpdateVisual();

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
            if (growthEvent != null)
                growthEvent.Cancel();

            Tile?.RemovePlant();

            PlantManager.Instance.UnregisterPlant(this);

            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (growthEvent != null)
            growthEvent.Cancel();

        Tile?.RemovePlant();

        if (PlantManager.TryGetInstance(out var manager))
            manager.UnregisterPlant(this);
    }

}