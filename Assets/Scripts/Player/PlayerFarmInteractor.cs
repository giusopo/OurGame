using UnityEngine;
using OurGame.Core;
using OurGame.Systems;

[DisallowMultipleComponent]
public class PlayerFarmInteractor : MonoBehaviour
{
    private PlayerFarmTileTracker tileTracker;
    private PlantData debugPlant;

    public FarmTile CurrentTile => tileTracker != null ? tileTracker.CurrentTile : null;

    public string CurrentPromptText
    {
        get
        {
            FarmTile currentTile = CurrentTile;
            if (currentTile == null)
                return string.Empty;

            if (currentTile.IsEmpty())
                return "Pianta";

            Plant plant = currentTile.currentPlant;
            if (plant != null && plant.IsReadyToHarvest(TimeManager.Instance.CurrentTick))
                return "Raccogli";

            return string.Empty;
        }
    }

    public void Configure(PlayerFarmTileTracker tracker, PlantData fallbackPlant)
    {
        tileTracker = tracker;
        debugPlant = fallbackPlant;
    }

    public bool TryInteract()
    {
        FarmTile currentTile = tileTracker != null ? tileTracker.CurrentTile : null;
        if (currentTile == null)
            return false;

        long currentTick = TimeManager.Instance.CurrentTick;

        Debug.Log("Interagisci con la tile: " + currentTile.GridPosition);

        if (currentTile.IsEmpty())
        {
            PlantData selectedSeed = BackpackInventorySystem.Instance.GetSelectedItem() as PlantData;

            if (selectedSeed != null)
            {
                currentTile.PlantSeed(selectedSeed, currentTick);
                BackpackInventorySystem.Instance.TryConsumeSelectedItem(1);
                return true;
            }

            if (debugPlant != null)
            {
                currentTile.PlantSeed(debugPlant, currentTick);
                return true;
            }

            return false;
        }

        Plant plant = currentTile.currentPlant;
        if (plant != null && plant.IsReadyToHarvest(currentTick))
        {
            plant.Harvest(currentTick);
            return true;
        }

        Debug.Log("La pianta non è pronta.");
        return false;
    }
}
