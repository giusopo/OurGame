using UnityEngine;
using UnityEngine.InputSystem;
using OurGame.Core;

public class PlayerInteraction : MonoBehaviour
{
    public PlantData debugPlant;
    private FarmTile currentTile; // tile su cui il player si trova

    // Input System
    public void OnInteract(InputValue value)
    {
        Debug.Log("Input Interact: " + value.isPressed);

        if (value.isPressed && currentTile != null)
        {
            float currentGameTime = TimeManager.Instance.GetCurrentTimeInMinutes();

            Debug.Log("Interagisci con il tile!");

            if (currentTile.IsEmpty())
                currentTile.PlantSeed(debugPlant, currentGameTime);
            else
                currentTile.Harvest(currentGameTime);
        }
    }

    // Triggers per rilevare la tile sotto il player
    private void OnTriggerEnter(Collider other)
    {
        FarmTile tile = other.GetComponent<FarmTile>();
        if (tile != null)
        {
            currentTile = tile;
            Debug.Log("Player sopra FarmTile");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FarmTile tile = other.GetComponent<FarmTile>();
        if (tile != null && tile == currentTile)
        {
            currentTile = null;
            Debug.Log("Player lasciato FarmTile");
        }
    }
}