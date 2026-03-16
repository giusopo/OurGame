using UnityEngine;
using UnityEngine.InputSystem;
using OurGame.Core;

public class PlayerInteraction : MonoBehaviour
{
    public PlantData debugPlant;

    private FarmTile currentTile;

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed || currentTile == null)
            return;

        long currentTick = TimeManager.Instance.CurrentTick;

        Debug.Log("Interagisci con la tile: " + currentTile.GridPosition);

        // Pianta seed
        if (currentTile.IsEmpty())
        {
            currentTile.PlantSeed(debugPlant, currentTick);
        }
        else
        {
            Plant plant = currentTile.currentPlant;

            if (plant != null && plant.IsReadyToHarvest(currentTick))
            {
                plant.Harvest(currentTick);
            }
            else
            {
                Debug.Log("La pianta non è pronta.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        FarmTile tile = other.GetComponent<FarmTile>();

        if (tile != null)
        {
            currentTile = tile;
            Debug.Log("Player entrato in FarmTile: " + tile.ParentGrid.gridId + " Posizione: " + tile.GridPosition);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FarmTile tile = other.GetComponent<FarmTile>();

        if (tile != null && tile == currentTile)
        {
            currentTile = null;
            Debug.Log("Player lasciato FarmTile");
        }
    }
}