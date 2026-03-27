using System;
using System.Collections.Generic;

namespace OurGame.Core
{
    [Serializable]
    public class BackpackSaveData
    {
        public int version = 2;
        public string activePocketName;
        public int selectedHotbarIndex;
        public List<InventorySlotSaveData> hotbarSlots = new List<InventorySlotSaveData>();
        public List<PocketSaveData> pockets = new List<PocketSaveData>();
    }

    [Serializable]
    public class PocketSaveData
    {
        public string pocketName;
        public int capacity;
        public int selectedSlotIndex;
        public List<InventorySlotSaveData> slots = new List<InventorySlotSaveData>();
    }
}
