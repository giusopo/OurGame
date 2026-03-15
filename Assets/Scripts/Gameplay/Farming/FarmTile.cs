using UnityEngine;

public class FarmTile : MonoBehaviour
{
    public Plant currentPlant;

    public bool IsEmpty()
    {
        return currentPlant == null;
    }

    public void PlantSeed(PlantData plantData, long currentTick)
    {
        if (!IsEmpty())
            return;

        GameObject plantGO = new GameObject("Plant");
        plantGO.transform.position = transform.position;

        Plant plant = plantGO.AddComponent<Plant>();
        plant.PlantSeed(plantData, currentTick);

        currentPlant = plant;
    }

    public void Harvest(long currentTick)
    {
        if (currentPlant == null)
            return;

        if (currentPlant.IsReadyToHarvest(currentTick))
        {
            currentPlant.Harvest(currentTick);
            currentPlant = null;
        }
    }
}