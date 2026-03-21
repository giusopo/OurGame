using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Core
{
    public class FarmGridManager : SingletonMono<FarmGridManager>
    {
        private Dictionary<string, FarmTileGrid> grids = new Dictionary<string, FarmTileGrid>();

        public void RegisterGrid(FarmTileGrid grid)
        {
            if (grids.ContainsKey(grid.gridId))
            {
                Debug.LogError("Duplicate grid: " + grid.gridId);
                return;
            }

            grids[grid.gridId] = grid;
            Debug.Log("Registered grid: " + grid.gridId);
        }

        public FarmTileGrid GetGrid(string id)
        {
            grids.TryGetValue(id, out var grid);
            return grid;
        }

        public void UnregisterGrid(FarmTileGrid grid)
        {
            if (grids.ContainsKey(grid.gridId))
                grids.Remove(grid.gridId);
        }

        public IEnumerable<FarmTileGrid> GetAllGrids() => grids.Values;
    }
}
