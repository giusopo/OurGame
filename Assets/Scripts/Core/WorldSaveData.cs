using System;
using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Core
{
    [Serializable]
    public class SaveData
    {
        public int saveVersion;
        public long currentTick;
        public List<FarmGridSaveData> grids = new List<FarmGridSaveData>();
        public BackpackSaveData backpack = new BackpackSaveData();
    }

    [Serializable]
    public class FarmGridSaveData
    {
        public string gridId;
        public int width;
        public int height;
        public Vector3 worldPosition;
        public float tileSize;
        public List<FarmTileSaveData> tiles = new List<FarmTileSaveData>();
    }

    [Serializable]
    public class FarmTileSaveData
    {
        public Vector2Int position;
        public bool hasPlant = true;
        public OurGame.Core.Domain.PlantState plant;
    }
}
