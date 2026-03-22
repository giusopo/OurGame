using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFarmTileTracker : MonoBehaviour
{
    public FarmTile CurrentTile { get; private set; }

    public void HandleTriggerEnter(Collider other)
    {
        FarmTile tile = other.GetComponent<FarmTile>();
        if (tile == null)
            return;

        CurrentTile = tile;
        Debug.Log("Player entrato in FarmTile: " + tile.ParentGrid.gridId + " Posizione: " + tile.GridPosition);
    }

    public void HandleTriggerExit(Collider other)
    {
        FarmTile tile = other.GetComponent<FarmTile>();
        if (tile == null || tile != CurrentTile)
            return;

        CurrentTile = null;
        Debug.Log("Player lasciato FarmTile");
    }
}
