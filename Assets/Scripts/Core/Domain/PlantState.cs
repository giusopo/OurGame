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

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(plantId)
                && growthTime > 0
                && MaxStage > 0;
        }

        public int GetDeterministicHarvestYield(
            int minInclusive,
            int maxInclusive,
            int tileX,
            int tileY,
            long currentTick
        )
        {
            int min = System.Math.Max(1, minInclusive);
            int max = System.Math.Max(min, maxInclusive);
            int range = max - min + 1;

            long hash = 1469598103934665603L;
            hash = CombineHash(hash, plantedTick);
            hash = CombineHash(hash, currentTick);
            hash = CombineHash(hash, growthTime);
            hash = CombineHash(hash, growthStage);
            hash = CombineHash(hash, tileX);
            hash = CombineHash(hash, tileY);

            if (!string.IsNullOrEmpty(plantId))
            {
                for (int i = 0; i < plantId.Length; i++)
                    hash = CombineHash(hash, plantId[i]);
            }

            ulong positiveHash = unchecked((ulong)hash);
            return min + (int)(positiveHash % (ulong)range);
        }

        public void Harvest(long currentTick, long regrowTime)
        {
            if (!regrows)
                return;

            plantedTick = currentTick - (growthTime - regrowTime);
            growthStage = 0;
        }

        private long CombineHash(long current, long value)
        {
            unchecked
            {
                return (current ^ value) * 1099511628211L;
            }
        }
    }
}
