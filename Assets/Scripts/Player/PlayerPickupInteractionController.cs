using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPickupInteractionController : MonoBehaviour
{
    private const float MinimumDuration = 0.05f;
    private enum InteractionTargetKind
    {
        None,
        Pickup,
        Farm,
    }

    private PlayerDroppedItemTracker droppedItemTracker;
    private PlayerFarmInteractor farmInteractor;
    private InteractionPromptUI promptUI;
    private string interactionKeyLabel = "E";
    private float pickupHoldDuration = 0.55f;
    private float pickupReleaseDecayDuration = 0.3f;
    private float promptShowDelay = 0.18f;

    private bool interactHeld;
    private float holdProgressSeconds;
    private DroppedItem highlightedItem;
    private DroppedItemSelectionHighlight highlightVisual;
    private string currentPromptKey = string.Empty;
    private float promptShowDelayRemaining;

    public void Configure(
        PlayerDroppedItemTracker tracker,
        PlayerFarmInteractor interactor,
        string keyLabel,
        float holdDuration,
        float releaseDecayDuration,
        float showDelay
    )
    {
        if (droppedItemTracker != null)
            droppedItemTracker.OnSelectedDroppedItemChanged -= HandleSelectedDroppedItemChanged;

        droppedItemTracker = tracker;
        farmInteractor = interactor;
        interactionKeyLabel = string.IsNullOrWhiteSpace(keyLabel) ? "E" : keyLabel.Trim().ToUpperInvariant();
        pickupHoldDuration = Mathf.Max(MinimumDuration, holdDuration);
        pickupReleaseDecayDuration = Mathf.Max(MinimumDuration, releaseDecayDuration);
        promptShowDelay = Mathf.Max(0f, showDelay);
        promptUI = GetOrCreatePromptUI();

        if (droppedItemTracker != null)
            droppedItemTracker.OnSelectedDroppedItemChanged += HandleSelectedDroppedItemChanged;

        HandleSelectedDroppedItemChanged(droppedItemTracker != null ? droppedItemTracker.SelectedDroppedItem : null);
        UpdatePromptVisual();
    }

    void Update()
    {
        UpdateHoldProgress();
        UpdatePromptVisual();
    }

    public void HandleInteractInput(bool isPressed)
    {
        if (IsInventoryOpen())
        {
            interactHeld = false;
            return;
        }

        interactHeld = isPressed && GetCurrentInteractionKind() != InteractionTargetKind.None;
    }

    private void UpdateHoldProgress()
    {
        if (promptShowDelayRemaining > 0f)
            promptShowDelayRemaining = Mathf.Max(0f, promptShowDelayRemaining - Time.deltaTime);

        if (IsInventoryOpen())
        {
            interactHeld = false;
            DecayHoldProgress();
            return;
        }

        InteractionTargetKind currentKind = GetCurrentInteractionKind();
        bool canInteract = currentKind != InteractionTargetKind.None;
        bool promptReady = canInteract && promptShowDelayRemaining <= 0f;

        if (!canInteract)
        {
            interactHeld = false;
            DecayHoldProgress();
            return;
        }

        if (!promptReady)
            return;

        if (interactHeld)
            holdProgressSeconds = Mathf.Min(pickupHoldDuration, holdProgressSeconds + Time.deltaTime);
        else
            DecayHoldProgress();

        if (holdProgressSeconds < pickupHoldDuration)
            return;

        holdProgressSeconds = 0f;
        interactHeld = false;
        FinalizeInteraction(currentKind);
    }

    private void UpdatePromptVisual()
    {
        if (promptUI == null)
            return;

        bool inventoryOpen = IsInventoryOpen();
        InteractionTargetInfo targetInfo = GetCurrentInteractionTarget();
        if (highlightVisual != null)
            highlightVisual.enabled = targetInfo.Kind == InteractionTargetKind.Pickup && !inventoryOpen;

        UpdatePromptDelay(targetInfo);

        bool visible = !inventoryOpen
            && targetInfo.Kind != InteractionTargetKind.None
            && !string.IsNullOrWhiteSpace(targetInfo.PromptText)
            && promptShowDelayRemaining <= 0f;

        promptUI.SetVisible(visible);
        if (!visible)
            return;

        float progress = Mathf.Clamp01(holdProgressSeconds / pickupHoldDuration);
        promptUI.SetPrompt(interactionKeyLabel, targetInfo.PromptText, progress, true);
    }

    private void HandleSelectedDroppedItemChanged(DroppedItem nextItem)
    {
        interactHeld = false;
        holdProgressSeconds = 0f;
        SetHighlightedItem(nextItem);
        RestartPromptDelay();
        UpdatePromptVisual();
    }

    private void FinalizeInteraction(InteractionTargetKind currentKind)
    {
        bool completed = false;

        switch (currentKind)
        {
            case InteractionTargetKind.Pickup:
                completed = droppedItemTracker != null && droppedItemTracker.TryCollectSelected();
                break;

            case InteractionTargetKind.Farm:
                completed = farmInteractor != null && farmInteractor.TryInteract();
                break;
        }

        if (completed)
            RestartPromptDelay();
    }

    private InteractionTargetKind GetCurrentInteractionKind()
    {
        return GetCurrentInteractionTarget().Kind;
    }

    private InteractionTargetInfo GetCurrentInteractionTarget()
    {
        if (droppedItemTracker != null && droppedItemTracker.HasSelectedDroppedItem)
        {
            DroppedItem selected = droppedItemTracker.SelectedDroppedItem;
            return new InteractionTargetInfo(
                InteractionTargetKind.Pickup,
                droppedItemTracker.CurrentPromptText,
                selected != null ? $"pickup:{selected.GetInstanceID()}" : string.Empty
            );
        }

        if (farmInteractor != null && !string.IsNullOrWhiteSpace(farmInteractor.CurrentPromptText))
        {
            FarmTile currentTile = farmInteractor.CurrentTile;
            return new InteractionTargetInfo(
                InteractionTargetKind.Farm,
                farmInteractor.CurrentPromptText,
                currentTile != null
                    ? $"farm:{currentTile.GetInstanceID()}:{farmInteractor.CurrentPromptText}"
                    : $"farm:{farmInteractor.CurrentPromptText}"
            );
        }

        return new InteractionTargetInfo(InteractionTargetKind.None, string.Empty, string.Empty);
    }

    private void UpdatePromptDelay(InteractionTargetInfo targetInfo)
    {
        if (targetInfo.Key == currentPromptKey)
            return;

        currentPromptKey = targetInfo.Key;
        promptShowDelayRemaining = string.IsNullOrWhiteSpace(targetInfo.Key)
            ? 0f
            : promptShowDelay;
    }

    private void RestartPromptDelay()
    {
        currentPromptKey = string.Empty;
        promptShowDelayRemaining = promptShowDelay;
    }

    private void DecayHoldProgress()
    {
        holdProgressSeconds = Mathf.Max(
            0f,
            holdProgressSeconds - GetReleaseDecayRate() * Time.deltaTime
        );
    }

    private void SetHighlightedItem(DroppedItem nextItem)
    {
        if (highlightedItem == nextItem)
            return;

        if (highlightVisual != null)
            Destroy(highlightVisual);

        highlightedItem = nextItem;
        highlightVisual = null;

        if (highlightedItem != null)
            highlightVisual = highlightedItem.gameObject.AddComponent<DroppedItemSelectionHighlight>();
    }

    private InteractionPromptUI GetOrCreatePromptUI()
    {
        InteractionPromptUI existingPrompt = GetComponent<InteractionPromptUI>();
        if (existingPrompt != null)
            return existingPrompt;

        existingPrompt = GetComponentInChildren<InteractionPromptUI>(true);
        if (existingPrompt != null)
            return existingPrompt;

        return gameObject.AddComponent<InteractionPromptUI>();
    }

    private float GetReleaseDecayRate()
    {
        return pickupHoldDuration / pickupReleaseDecayDuration;
    }

    private bool IsInventoryOpen()
    {
        return InventorySystem.Instance != null && InventorySystem.Instance.IsInventoryOpen;
    }

    private readonly struct InteractionTargetInfo
    {
        public readonly InteractionTargetKind Kind;
        public readonly string PromptText;
        public readonly string Key;

        public InteractionTargetInfo(InteractionTargetKind kind, string promptText, string key)
        {
            Kind = kind;
            PromptText = promptText;
            Key = key;
        }
    }

    void OnDestroy()
    {
        if (droppedItemTracker != null)
            droppedItemTracker.OnSelectedDroppedItemChanged -= HandleSelectedDroppedItemChanged;

        if (highlightVisual != null)
            Destroy(highlightVisual);
    }
}
