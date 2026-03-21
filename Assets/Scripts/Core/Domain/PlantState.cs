namespace OurGame.Core.Domain
{
    [System.Serializable]
    public class PlantState
    {
        public string plantId;
        public long plantedTick;
        public int growthStage;
        public long growthTime;
        public bool regrows;

        public int MaxStage;

        public PlantState(
            string plantId,
            long plantedTick,
            long growthTime,
            int maxStage,
            bool regrows
        )
        {
            this.plantId = plantId;
            this.plantedTick = plantedTick;
            this.growthTime = growthTime;
            MaxStage = maxStage;
            this.regrows = regrows;
            growthStage = 0;
        }

        public long GetStageDuration()
        {
            return (long)System.Math.Ceiling((double)growthTime / MaxStage);
        }

        public long GetNextGrowthTick()
        {
            return plantedTick + GetStageDuration() * (growthStage + 1);
        }

        public void AdvanceGrowth()
        {
            if (growthStage < MaxStage - 1)
                growthStage++;
        }

        public bool IsFullyGrown()
        {
            return growthStage >= MaxStage - 1;
        }

        public bool IsReadyToHarvest(long currentTick)
        {
            return (currentTick - plantedTick) >= growthTime;
        }

        public void Harvest(long currentTick, long regrowTime)
        {
            if (!regrows)
                return;

            plantedTick = currentTick - (growthTime - regrowTime);
            growthStage = 0;
        }
    }
}
