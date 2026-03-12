using UnityEngine;

public class Plant : MonoBehaviour
{
    public PlantData plantData;

    private float plantedTime = 0f;    // tempo di gioco in ore
    private float growthTime = 0f;     // tempo totale necessario per crescere
    private int growthStage = 0;

    private GameObject currentVisual;

    public void PlantSeed(PlantData data, float currentGameTime)
    {
        plantData = data;
        plantedTime = currentGameTime;

        // growthTime totale della pianta in ore
        growthTime = plantData.daysToGrow * 24f; // 1 giorno = 24 ore di gioco
        growthStage = 0;

        UpdateVisual();
    }

    public void UpdateGrowth(float currentGameTime)
    {
        float elapsed = currentGameTime - plantedTime; // ore di gioco trascorse
        int newStage = Mathf.FloorToInt((elapsed / growthTime) * plantData.growthStages.Length);
        newStage = Mathf.Clamp(newStage, 0, plantData.growthStages.Length - 1);

        if (newStage != growthStage)
        {
            Debug.Log("Plant " + plantData.plantName + " advanced to stage " + newStage);
            growthStage = newStage;
            UpdateVisual();
        }
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
            // riparte da regrowDays prima della maturità
            plantedTime = currentGameTime - (growthTime - plantData.regrowDays * 24f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}