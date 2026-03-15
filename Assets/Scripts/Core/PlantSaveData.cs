using System;
using UnityEngine;
namespace OurGame.Core
{
    [Serializable]
public class PlantSaveData
{
    public string plantId;
    public long plantedTick;
    public int growthStage;
    public Vector3 position;
}
}
