using UnityEngine;
using OurGame.Core;
using OurGame.Core.Domain;

public class FarmTile : MonoBehaviour
{
    public Vector2Int GridPosition; // posizione relativa alla griglia
    public Plant currentPlant;
    public FarmTileGrid ParentGrid;
    private FarmTileState state;

    public FarmTileState State => state;

    public void Initialize(FarmTileState state, FarmTileGrid grid)
    {
        this.state = state;
        ParentGrid = grid;
    }

    public bool IsEmpty() => state == null || state.IsEmpty();

    public void PlantSeed(PlantData plantData, long currentTick)
    {
        if (!IsEmpty()) return;

        if (state == null)
        {
            Debug.LogError("FarmTile state not initialized");
            return;
        }

        GameObject plantGO = Instantiate(
            plantData.plantPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        Plant plant = plantGO.GetComponent<Plant>();
        if (plant == null)
        {
            Debug.LogError("Plant prefab missing Plant component");
            Destroy(plantGO);
            return;
        }

        plant.PlantSeed(plantData, currentTick, this);
        currentPlant = plant;
        state.SetPlant(plant.State);
    }

    public void RemovePlant()
    {
        state?.RemovePlant();
        currentPlant = null;
    }
}
