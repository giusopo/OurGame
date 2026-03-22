using UnityEngine;
using UnityEngine.InputSystem;
using OurGame.Core;

public class PlayerInteraction : MonoBehaviour
{
    public PlantData debugPlant;
    [SerializeField] private Transform itemDropOrigin;
    [SerializeField] private float itemDropForwardOffset = 1f;
    [SerializeField] private float itemDropUpOffset = 1.1f;

    private FarmTile currentTile;
    private DroppedItem currentDroppedItem;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || InventorySystem.Instance.IsInventoryOpen)
            return;

        if (keyboard.qKey.wasPressedThisFrame)
            DropSelectedItem();
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (InventorySystem.Instance.IsInventoryOpen)
            return;

        if (currentDroppedItem != null && currentDroppedItem.TryCollect())
            return;

        if (currentTile == null)
            return;

        long currentTick = TimeManager.Instance.CurrentTick;

        Debug.Log("Interagisci con la tile: " + currentTile.GridPosition);

        // Pianta seed
        if (currentTile.IsEmpty())
        {
            PlantData selectedSeed = InventorySystem.Instance.GetSelectedItem() as PlantData;

            if (selectedSeed != null)
            {
                currentTile.PlantSeed(selectedSeed, currentTick);
                InventorySystem.Instance.TryConsumeSelectedItem(1);
                return;
            }

            if (debugPlant != null)
                currentTile.PlantSeed(debugPlant, currentTick);
        }
        else
        {
            Plant plant = currentTile.currentPlant;

            if (plant != null && plant.IsReadyToHarvest(currentTick))
            {
                plant.Harvest(currentTick);
            }
            else
            {
                Debug.Log("La pianta non è pronta.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DroppedItem droppedItem = other.GetComponentInParent<DroppedItem>();
        if (droppedItem != null)
        {
            currentDroppedItem = droppedItem;
            return;
        }

        FarmTile tile = other.GetComponent<FarmTile>();

        if (tile != null)
        {
            currentTile = tile;
            Debug.Log("Player entrato in FarmTile: " + tile.ParentGrid.gridId + " Posizione: " + tile.GridPosition);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DroppedItem droppedItem = other.GetComponentInParent<DroppedItem>();
        if (droppedItem != null && droppedItem == currentDroppedItem)
            currentDroppedItem = null;

        FarmTile tile = other.GetComponent<FarmTile>();

        if (tile != null && tile == currentTile)
        {
            currentTile = null;
            Debug.Log("Player lasciato FarmTile");
        }
    }

    private void DropSelectedItem()
    {
        if (!InventorySystem.Instance.TryTakeSelectedItem(1, out InventoryItemDefinition item, out int quantity))
            return;

        Transform origin = itemDropOrigin != null ? itemDropOrigin : transform;
        Vector3 dropPosition =
            origin.position
            + origin.forward * itemDropForwardOffset
            + Vector3.up * itemDropUpOffset;

        DroppedItem.Spawn(item, quantity, dropPosition, origin.forward);
    }
}
