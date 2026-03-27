using UnityEngine;
using OurGame.Core;
using OurGame.Core.Domain;
using OurGame.Systems;

public class Plant : MonoBehaviour
{
    private GameObject currentVisual;

    public PlantData plantData;

    public FarmTile Tile { get; private set; }
    public PlantState State => state;

    public long PlantTick => state?.plantedTick ?? 0;
    public int GrowthStage => state?.growthStage ?? 0;

    private PlantState state;

    private ScheduledEvent growthEvent;

    public void PlantSeed(PlantData data, long currentTick, FarmTile tile)
    {
        plantData = data;
        Tile = tile;

        state = new PlantState(
            data.plantId,
            currentTick,
            data.GetGrowthTimeTicks(),
            data.growthStages.Length,
            data.regrows
        );

        UpdateVisual();
        ScheduleNextGrowth();

        Debug.Log(
            $"Planted {plantData.plantId} at tick {state.plantedTick}, growthTime={state.growthTime}"
        );

        Debug.Log("Spawning plant prefab: " + plantData.plantPrefab);
        PlantManager.Instance.RegisterPlant(this);
    }

    public void RestorePlant(PlantData data, long savedPlantedTick, int savedGrowthStage, FarmTile tile)
    {
        PlantState restoredState = new PlantState(
            data.plantId,
            savedPlantedTick,
            data.GetGrowthTimeTicks(),
            data.growthStages.Length,
            data.regrows
        );

        restoredState.growthStage = Mathf.Clamp(
            savedGrowthStage,
            0,
            data.growthStages.Length - 1
        );

        RestorePlant(data, restoredState, tile);
    }

    public void RestorePlant(PlantData data, PlantState restoredState, FarmTile tile)
    {
        if (restoredState == null)
        {
            Debug.LogError("Cannot restore plant from null state");
            return;
        }

        plantData = data;
        Tile = tile;
        state = restoredState;

        state.plantId = data.plantId;
        state.growthTime = data.GetGrowthTimeTicks();
        state.MaxStage = data.growthStages.Length;
        state.regrows = data.regrows;
        state.growthStage = Mathf.Clamp(
            state.growthStage,
            0,
            plantData.growthStages.Length - 1
        );

        UpdateVisual();

        if (!state.IsFullyGrown())
            ScheduleNextGrowth();

        Debug.Log("Spawning plant prefab: " + plantData.plantPrefab);
        PlantManager.Instance.RegisterPlant(this);
    }

    void ScheduleNextGrowth()
    {
        if (state == null || state.MaxStage <= 0 || state.IsFullyGrown())
            return;

        long nextTick = state.GetNextGrowthTick();

        if (growthEvent != null)
            growthEvent.Cancel();

        Debug.Log(
            $"Scheduling growth for {plantData.plantId} at tick {nextTick}, stage={state.growthStage}"
        );

        growthEvent = GameEventScheduler.Instance.Schedule(nextTick, ProcessGrowthEvent);
    }

    void ProcessGrowthEvent()
    {
        if (state == null)
            return;

        state.AdvanceGrowth();

        UpdateVisual();

        if (!state.IsFullyGrown())
            ScheduleNextGrowth();
    }

    void UpdateVisual()
    {
        if (plantData == null || state == null || plantData.growthStages.Length == 0)
            return;

        if (currentVisual != null)
            Destroy(currentVisual);

        currentVisual = Instantiate(
            plantData.growthStages[state.growthStage],
            transform.position,
            Quaternion.identity,
            transform
        );
    }

    public bool IsReadyToHarvest(long currentTick)
    {
        return state != null && state.IsReadyToHarvest(currentTick);
    }

    public void Harvest(long currentTick)
    {
        if (state == null || !state.IsReadyToHarvest(currentTick))
            return;

        InventoryItemDefinition harvestItem = plantData.GetHarvestItem();
        int harvestAmount = GetHarvestAmount(currentTick);

        if (harvestItem == null)
        {
            Debug.LogError("Harvest item non configurato per " + plantData.plantId);
            return;
        }

        if (!BackpackInventorySystem.Instance.TryAddItem(harvestItem, harvestAmount))
        {
            Debug.Log("Inventario pieno: raccolto annullato.");
            return;
        }

        Debug.Log(
            $"Collected {harvestAmount}x {harvestItem.DisplayName} from {plantData.plantId}"
        );

        if (plantData.regrows)
        {
            state.Harvest(currentTick, plantData.GetRegrowTimeTicks());
            UpdateVisual();
            ScheduleNextGrowth();
        }
        else
        {
            if (growthEvent != null)
                growthEvent.Cancel();

            Destroy(gameObject);
        }
    }

    private int GetHarvestAmount(long currentTick)
    {
        int tileX = Tile != null ? Tile.GridPosition.x : 0;
        int tileY = Tile != null ? Tile.GridPosition.y : 0;

        return state.GetDeterministicHarvestYield(
            plantData.GetHarvestMinYield(),
            plantData.GetHarvestMaxYield(),
            tileX,
            tileY,
            currentTick
        );
    }

    void OnDestroy()
    {
        if (growthEvent != null)
            growthEvent.Cancel();

        if (Tile != null && Tile.currentPlant == this)
            Tile.RemovePlant();

        if (PlantManager.TryGetInstance(out var manager))
            manager.UnregisterPlant(this);
    }

}
