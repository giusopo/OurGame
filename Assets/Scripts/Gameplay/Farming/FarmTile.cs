using UnityEngine;
using OurGame.Core;

public class FarmTile : MonoBehaviour
{
    public Vector2Int GridPosition; // posizione relativa alla griglia
    public Plant currentPlant;
    public FarmTileGrid ParentGrid;

    public bool IsEmpty() => currentPlant == null;

    public void PlantSeed(PlantData plantData, long currentTick)
    {
        if (!IsEmpty()) return;

        GameObject plantGO = Instantiate(
            plantData.plantPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );

        Plant plant = plantGO.GetComponent<Plant>();
        plant.PlantSeed(plantData, currentTick, this);
        currentPlant = plant;
    }

    public void RemovePlant() => currentPlant = null;
}