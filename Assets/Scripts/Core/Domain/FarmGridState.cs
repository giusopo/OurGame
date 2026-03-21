using System.Collections.Generic;
using UnityEngine;

namespace OurGame.Core.Domain
{
    public class FarmGridState
    {
        public string gridId;
        public int width;
        public int height;

        private readonly Dictionary<Vector2Int, FarmTileState> tiles =
            new Dictionary<Vector2Int, FarmTileState>();

        public FarmGridState(string gridId, int width, int height)
        {
            this.gridId = gridId;
            this.width = width;
            this.height = height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    tiles[pos] = new FarmTileState(pos);
                }
            }
        }

        public FarmTileState GetTile(Vector2Int pos)
        {
            tiles.TryGetValue(pos, out var tile);
            return tile;
        }

        public IEnumerable<FarmTileState> GetAllTiles()
        {
            return tiles.Values;
        }
    }
}
