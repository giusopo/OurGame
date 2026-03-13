using UnityEngine;
using OurGame.Core;

public class PlantGrowthSystem : MonoBehaviour
{
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < 1f) return;
        timer = 0f;

        float currentGameTime = TimeManager.Instance.GetCurrentTimeInHours();

        foreach (Plant plant in PlantManager.Instance.Plants)
        {
            plant.UpdateGrowth(currentGameTime);
        }
    }
}