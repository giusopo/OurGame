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

    public long GetGrowthTimeTicks()
    {
        return growDays * 1440L + growHours * 60L + growMinutes;
    }

    public long GetRegrowTimeTicks()
    {
        return regrowDays * 1440L + regrowHours * 60L + regrowMinutes;
    }
}