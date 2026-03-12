using UnityEngine;

[CreateAssetMenu(menuName = "Farming/Plant Data")]
public class PlantData : ScriptableObject
{
    public string plantName;

    public int daysToGrow;

    public GameObject[] growthStages;

    public int sellPrice;

    public bool regrows;

    public int regrowDays;
}