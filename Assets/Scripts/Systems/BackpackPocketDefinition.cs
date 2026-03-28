using System;
using UnityEngine;

namespace OurGame.Systems
{
    [Serializable]
    public class BackpackPocketDefinition
    {
        [SerializeField] private string pocketName;
        [SerializeField] private string displayName;
        [SerializeField] private int rows = 1;
        [SerializeField] private int columns = 1;

        public string PocketName => pocketName;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? pocketName : displayName;
        public int Rows => Mathf.Max(1, rows);
        public int Columns => Mathf.Max(1, columns);
        public int Capacity => Rows * Columns;

        public BackpackPocketDefinition()
        {
            pocketName = string.Empty;
            displayName = string.Empty;
            rows = 1;
            columns = 1;
        }

        public BackpackPocketDefinition(string pocketName, string displayName, int rows, int columns)
        {
            this.pocketName = pocketName;
            this.displayName = displayName;
            this.rows = Mathf.Max(1, rows);
            this.columns = Mathf.Max(1, columns);
        }

        public BackpackPocketDefinition Clone()
        {
            return new BackpackPocketDefinition(PocketName, DisplayName, Rows, Columns);
        }

        public void Normalize()
        {
            rows = Mathf.Max(1, rows);
            columns = Mathf.Max(1, columns);

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = PocketName;
        }

        public static BackpackPocketDefinition CreateDefault(string pocketName)
        {
            return pocketName switch
            {
                PocketNames.LeftPocket => new BackpackPocketDefinition(pocketName, "Left pocket", 1, 2),
                PocketNames.RightPocket => new BackpackPocketDefinition(pocketName, "Right pocket", 1, 2),
                PocketNames.CentralPocket => new BackpackPocketDefinition(pocketName, "Central pocket", 3, 3),
                PocketNames.UpperPocket => new BackpackPocketDefinition(pocketName, "Upper pocket", 2, 2),
                PocketNames.BottomPocket => new BackpackPocketDefinition(pocketName, "Bottom pocket", 2, 2),
                _ => new BackpackPocketDefinition(pocketName, pocketName, 1, 1)
            };
        }
    }
}
