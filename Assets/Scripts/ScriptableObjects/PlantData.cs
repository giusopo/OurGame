using UnityEngine;

[CreateAssetMenu(menuName = "Farming/Plant Data")]
public class PlantData : ScriptableObject
{
    public string plantName;

    [Header("Growth Time")]
    public int growDays;
    public int growHours;
    public int growMinutes;

    public GameObject[] growthStages;

    public int sellPrice;

    public bool regrows;

    [Header("Regrow Time")]
    public int regrowDays;
    public int regrowHours;
    public int regrowMinutes;

    public float GetGrowthTimeMinutes()
    {
        return growDays * 1440f + growHours * 60f + growMinutes;
    }

    public float GetRegrowTimeMinutes()
    {
        return regrowDays * 1440f + regrowHours * 60f + regrowMinutes;
    }
}