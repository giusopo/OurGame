using System;
using UnityEngine;
namespace OurGame.Core
{
    [Serializable]
public class PlantSaveData
{
    public string plantDataName;    // Nome dello ScriptableObject per identificare il tipo di pianta
    public long plantedTick;
    public int growthStage;
    public Vector3 position;
}
}
