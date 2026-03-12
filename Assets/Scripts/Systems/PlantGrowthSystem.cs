using UnityEngine;
using OurGame.Core;

public class PlantGrowthSystem : MonoBehaviour
{
    void Update()
    {
        if (TimeManager.Instance == null) return;

        float currentGameTime = TimeManager.Instance.GetCurrentTimeInHours();

        Plant[] plants = FindObjectsByType<Plant>(FindObjectsSortMode.None);
        foreach (Plant plant in plants)
        {
            plant.UpdateGrowth(currentGameTime);
        }
    }
}