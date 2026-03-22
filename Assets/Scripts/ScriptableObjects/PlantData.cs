using UnityEngine;

[CreateAssetMenu(menuName = "Farming/Plant Data")]
public class PlantData : InventoryItemDefinition
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
    [Min(1)] public int harvestYield = 1;

    [Header("Harvest")]
    [SerializeField] private InventoryItemDefinition harvestItem;
    [SerializeField, Min(1)] private int harvestMinYield = 3;
    [SerializeField, Min(1)] private int harvestMaxYield = 7;

    [Header("Regrow")]
    public bool regrows;
    public int regrowDays;
    public int regrowHours;
    public int regrowMinutes;

    public override string ItemId => plantId;

    public long GetGrowthTimeTicks()
    {
        return growDays * 1440L + growHours * 60L + growMinutes;
    }

    public long GetRegrowTimeTicks()
    {
        return regrowDays * 1440L + regrowHours * 60L + regrowMinutes;
    }

    public InventoryItemDefinition GetHarvestItem()
    {
        return harvestItem != null ? harvestItem : this;
    }

    public int GetHarvestMinYield()
    {
        return harvestMinYield > 0 ? harvestMinYield : 3;
    }

    public int GetHarvestMaxYield()
    {
        int minYield = GetHarvestMinYield();
        if (harvestMaxYield > 0)
            return Mathf.Max(minYield, harvestMaxYield);

        return Mathf.Max(minYield, 7);
    }
}
