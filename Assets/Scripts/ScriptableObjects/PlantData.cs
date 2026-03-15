using UnityEngine;

[CreateAssetMenu(menuName = "Farming/Plant Data")]
public class PlantData : ScriptableObject
{
    [Header("Identification")]
    public string plantId; // ID univoco usato per save/load

    [Header("Prefab")]
    public GameObject plantPrefab; // prefab principale della pianta

    [Header("Growth")]
    public GameObject[] growthStages;

    [Header("Growth Time")]
    public int growDays;
    public int growHours;
    public int growMinutes;

    [Header("Economy")]
    public int sellPrice;

    [Header("Regrow")]
    public bool regrows;
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
