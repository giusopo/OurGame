using UnityEngine;

namespace OurGame.Core.Domain
{
    public class FarmTileState
    {
        public Vector2Int position;
        public PlantState plantState;

        public FarmTileState(Vector2Int position)
        {
            this.position = position;
        }

        public bool IsEmpty()
        {
            return plantState == null || !plantState.IsValid();
        }

        public void SetPlant(PlantState plant)
        {
            plantState = plant != null && plant.IsValid() ? plant : null;
        }

        public void RemovePlant()
        {
            plantState = null;
        }
    }
}
