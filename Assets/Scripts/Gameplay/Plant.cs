using UnityEngine;
using OurGame.Core;

public class Plant : MonoBehaviour
{
    public PlantData plantData;

    private float plantedTime;    // minuti di gioco
    private float growthTime;     // minuti necessari
    private float nextGrowthCheckTime;
    private int growthStage = 0;

    private GameObject currentVisual;

    public void PlantSeed(PlantData data, float currentGameTime)
    {
        plantData = data;
        plantedTime = currentGameTime;

        // growthTime totale della pianta in ore
        growthTime = plantData.GetGrowthTimeMinutes();
        growthStage = 0;

        ScheduleNextGrowth();

        PlantManager.Instance.RegisterPlant(this);

        UpdateVisual();
    }

    void ScheduleNextGrowth()
    {
        int stages = plantData.growthStages.Length;

        float stageDuration = growthTime / stages;

        nextGrowthCheckTime = plantedTime + stageDuration * (growthStage + 1);
    }

    public void UpdateGrowth(float currentGameTime)
    {
        if (currentGameTime < nextGrowthCheckTime)
            return;

        growthStage++;

        if (growthStage >= plantData.growthStages.Length)
            return;

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

    public bool IsReadyToHarvest(float currentGameTime)
    {
        return (currentGameTime - plantedTime) >= growthTime;
    }

    public void Harvest(float currentGameTime)
    {
        if (!IsReadyToHarvest(currentGameTime))
            return;

        Debug.Log("Collected " + plantData.plantName);

        if (plantData.regrows)
        {
            // la pianta torna a uno stato prima della maturità
            float regrowTime = plantData.GetRegrowTimeMinutes();
            plantedTime = currentGameTime - (growthTime - regrowTime);
        }
        else
        {
            // rimuovi dal manager prima di distruggere
            PlantManager.Instance.UnregisterPlant(this);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (PlantManager.Instance != null)
            PlantManager.Instance.UnregisterPlant(this);
    }

}