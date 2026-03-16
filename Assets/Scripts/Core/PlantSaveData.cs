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
        public string gridId;        
        public Vector2Int tilePosition; // posizione relativa nella griglia
    }
}