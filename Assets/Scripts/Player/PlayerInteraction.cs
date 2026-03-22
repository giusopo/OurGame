using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public PlantData debugPlant;
    [SerializeField] private Transform itemDropOrigin;
    [SerializeField] private float itemDropForwardOffset = 0.50f;
    [SerializeField] private float itemDropUpOffset = 0.5f;
    [SerializeField] private string interactionKeyLabel = "E";
    [SerializeField] private float pickupHoldDuration = 0.55f;
    [SerializeField] private float pickupReleaseDecayDuration = 0.3f;
       [SerializeField] private float promptShowDelay = 0.5f;

    private PlayerDroppedItemTracker droppedItemTracker;
    private PlayerFarmTileTracker farmTileTracker;
    private PlayerFarmInteractor farmInteractor;
    private PlayerItemDropController itemDropController;
    private PlayerPickupInteractionController pickupInteractionController;

    public DroppedItem SelectedDroppedItem => droppedItemTracker != null
        ? droppedItemTracker.SelectedDroppedItem
        : null;

    public FarmTile CurrentFarmTile => farmTileTracker != null
        ? farmTileTracker.CurrentTile
        : null;

    public string CurrentInteractionPrompt
    {
        get
        {
            if (droppedItemTracker != null && droppedItemTracker.HasSelectedDroppedItem)
                return droppedItemTracker.CurrentPromptText;

            return farmInteractor != null
                ? farmInteractor.CurrentPromptText
                : string.Empty;
        }
    }

    void Awake()
    {
        droppedItemTracker = GetOrAddComponent<PlayerDroppedItemTracker>();
        farmTileTracker = GetOrAddComponent<PlayerFarmTileTracker>();
        farmInteractor = GetOrAddComponent<PlayerFarmInteractor>();
        itemDropController = GetOrAddComponent<PlayerItemDropController>();
        pickupInteractionController = GetOrAddComponent<PlayerPickupInteractionController>();

        farmInteractor.Configure(farmTileTracker, debugPlant);
        itemDropController.Configure(itemDropOrigin, itemDropForwardOffset, itemDropUpOffset);
        pickupInteractionController.Configure(
            droppedItemTracker,
            farmInteractor,
            interactionKeyLabel,
            pickupHoldDuration,
            pickupReleaseDecayDuration,
            promptShowDelay
        );
    }

    public void OnInteract(InputValue value)
    {
        pickupInteractionController?.HandleInteractInput(value.isPressed);
    }

    void OnTriggerEnter(Collider other)
    {
        droppedItemTracker?.HandleTriggerEnter(other);
        farmTileTracker?.HandleTriggerEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        droppedItemTracker?.HandleTriggerExit(other);
        farmTileTracker?.HandleTriggerExit(other);
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent<T>();

        return component;
    }
}
