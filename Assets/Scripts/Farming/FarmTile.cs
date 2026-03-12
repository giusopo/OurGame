using UnityEngine;

public class FarmTile : MonoBehaviour
{
    public Plant currentPlant;

    public bool IsEmpty()
    {
        return currentPlant == null;
    }

    public void PlantSeed(PlantData plantData, float currentGameTime)
    {
        if (!IsEmpty())
            return;

        GameObject plantGO = new GameObject("Plant");
        plantGO.transform.position = transform.position;

        Plant plant = plantGO.AddComponent<Plant>();
        plant.PlantSeed(plantData, currentGameTime);

        currentPlant = plant;
    }

    public void Harvest(float currentGameTime)
    {
        if (currentPlant == null)
            return;

        if (currentPlant.IsReadyToHarvest(currentGameTime))
        {
            currentPlant.Harvest(currentGameTime);
            currentPlant = null;
        }
    }
}