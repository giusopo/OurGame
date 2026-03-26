using cherrydev;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class NearbyDialogStarter : MonoBehaviour
{
    [Header("Dialog")]
    [SerializeField] private DialogBehaviour dialogBehaviour;
    [SerializeField] private DialogNodeGraph dialogGraph;
    [SerializeField] private bool activateDialogObjectOnStart = true;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string interactionKeyLabel = "E";
    [SerializeField] private string promptText = "Parla";
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private float promptShowDelay = 0.1f;
    [SerializeField] private Vector3 interactionOffset = Vector3.zero;
    [SerializeField] private bool showScreenPrompt = true;
    [SerializeField] private Vector3 screenPromptWorldOffset = new Vector3(0f, 2f, 0f);

    private Transform playerTransform;
    private InteractionPromptUI promptUI;
    private bool wasInteractPressedLastFrame;
    private bool isDialogRunning;
    private float promptDelayRemaining;
    private bool isShowingPrompt;
    private GUIStyle cachedPromptStyle;
    private DialogBehaviour runtimeDialogBehaviour;
    private DialogBehaviour subscribedDialogBehaviour;
    private bool cursorStateCaptured;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private void Awake()
    {
        ResolvePlayerReferences();
        HidePrompt();
    }

    private void OnEnable()
    {
        ResolvePlayerReferences();
        HidePrompt();
    }

    private void Update()
    {
        ResolvePlayerReferences();

        if (promptDelayRemaining > 0f)
            promptDelayRemaining = Mathf.Max(0f, promptDelayRemaining - Time.deltaTime);

        bool isInRange = IsPlayerInRange();
        bool canStartDialog = CanStartDialog(isInRange);
        bool isPressed = IsInteractionPressed();
        bool justPressed = isPressed && !wasInteractPressedLastFrame;

        if (!canStartDialog)
        {
            if (!isInRange)
                promptDelayRemaining = 0f;

            isShowingPrompt = false;
            HidePrompt();
            wasInteractPressedLastFrame = isPressed;
            return;
        }

        if (promptDelayRemaining > 0f)
        {
            isShowingPrompt = false;
            HidePrompt();
            if (justPressed)
                promptDelayRemaining = 0f;

            wasInteractPressedLastFrame = isPressed;
            return;
        }

        isShowingPrompt = true;
        ShowPrompt();

        if (justPressed)
            StartConfiguredDialog();

        wasInteractPressedLastFrame = isPressed;
    }

    private void StartConfiguredDialog()
    {
        DialogBehaviour activeDialogBehaviour = GetOrCreateDialogBehaviourInstance();
        if (activeDialogBehaviour == null)
        {
            Debug.LogWarning($"NearbyDialogStarter on '{name}' is missing a DialogBehaviour reference.");
            return;
        }

        if (dialogGraph == null)
        {
            Debug.LogWarning($"NearbyDialogStarter on '{name}' is missing a DialogNodeGraph reference.");
            return;
        }

        if (activateDialogObjectOnStart && activeDialogBehaviour.gameObject.activeSelf == false)
            activeDialogBehaviour.gameObject.SetActive(true);

        if (!activeDialogBehaviour.isActiveAndEnabled)
        {
            Debug.LogWarning(
                $"NearbyDialogStarter on '{name}' cannot start dialog because '{activeDialogBehaviour.gameObject.name}' is still inactive in hierarchy."
            );
            return;
        }

        CloseInventoryIfOpen();
        SetHotbarVisible(false);
        EnsureDialogSubscriptions(activeDialogBehaviour);
        CaptureCursorState();
        ApplyLockedCursorState();

        isDialogRunning = true;
        isShowingPrompt = false;
        HidePrompt();

        activeDialogBehaviour.StartDialog(dialogGraph, null, HandleDialogFinished);
    }

    private DialogBehaviour GetOrCreateDialogBehaviourInstance()
    {
        if (runtimeDialogBehaviour != null)
            return runtimeDialogBehaviour;

        if (dialogBehaviour == null)
            return null;

        if (dialogBehaviour.gameObject.scene.IsValid())
            return dialogBehaviour;

        GameObject dialogInstance = Instantiate(dialogBehaviour.gameObject);
        dialogInstance.name = dialogBehaviour.gameObject.name;
        runtimeDialogBehaviour = dialogInstance.GetComponent<DialogBehaviour>();

        if (runtimeDialogBehaviour == null)
        {
            Debug.LogWarning(
                $"NearbyDialogStarter on '{name}' instantiated '{dialogInstance.name}', but it does not contain a DialogBehaviour."
            );
            return null;
        }

        return runtimeDialogBehaviour;
    }

    private void EnsureDialogSubscriptions(DialogBehaviour activeDialogBehaviour)
    {
        if (subscribedDialogBehaviour == activeDialogBehaviour)
            return;

        if (subscribedDialogBehaviour != null)
        {
            subscribedDialogBehaviour.AnswerNodeActivated -= HandleAnswerNodeActivated;
            subscribedDialogBehaviour.SentenceNodeActivated -= HandleSentenceNodeActivated;
        }

        subscribedDialogBehaviour = activeDialogBehaviour;
        subscribedDialogBehaviour.AnswerNodeActivated += HandleAnswerNodeActivated;
        subscribedDialogBehaviour.SentenceNodeActivated += HandleSentenceNodeActivated;
    }

    private void HandleAnswerNodeActivated()
    {
        CaptureCursorState();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HandleSentenceNodeActivated()
    {
        if (!isDialogRunning)
            return;

        ApplyLockedCursorState();
    }

    private void CaptureCursorState()
    {
        if (cursorStateCaptured)
            return;

        previousCursorVisible = Cursor.visible;
        previousCursorLockState = Cursor.lockState;
        cursorStateCaptured = true;
    }

    private void ApplyLockedCursorState()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void RestoreCursorState()
    {
        if (!cursorStateCaptured)
            return;

        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockState;
        cursorStateCaptured = false;
    }

    private void CloseInventoryIfOpen()
    {
        if (InventorySystem.Instance != null && InventorySystem.Instance.IsInventoryOpen)
            InventorySystem.Instance.SetInventoryOpen(false);
    }

    private void SetHotbarVisible(bool visible)
    {
        if (InventoryUIController.Instance != null)
            InventoryUIController.Instance.SetHotbarVisible(visible);
    }

    private void HandleDialogFinished(DialogVariablesHandler _)
    {
        isDialogRunning = false;
        promptDelayRemaining = promptShowDelay;
        SetHotbarVisible(true);
        RestoreCursorState();
    }

    private bool CanStartDialog(bool isInRange)
    {
        if (!isInRange || isDialogRunning)
            return false;

        if (dialogBehaviour == null || dialogGraph == null)
            return false;

        return !IsInventoryOpen();
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null)
            return false;

        Vector3 origin = transform.position + interactionOffset;
        Vector3 playerPosition = playerTransform.position;
        origin.y = playerPosition.y;

        return Vector3.Distance(origin, playerPosition) <= Mathf.Max(0.1f, interactionDistance);
    }

    private void ResolvePlayerReferences()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        if (promptUI == null && playerTransform != null)
            promptUI = playerTransform.GetComponentInChildren<InteractionPromptUI>(true);
    }

    private void ShowPrompt()
    {
        if (promptUI == null)
            return;

        promptUI.SetVisible(true);
        promptUI.SetPrompt(
            string.IsNullOrWhiteSpace(interactionKeyLabel) ? "E" : interactionKeyLabel,
            string.IsNullOrWhiteSpace(promptText) ? "Parla" : promptText,
            0f,
            false
        );
    }

    private void HidePrompt()
    {
        if (promptUI != null)
            promptUI.SetVisible(false);
    }

    private void OnGUI()
    {
        if (!showScreenPrompt || !isShowingPrompt || isDialogRunning)
            return;

        Camera currentCamera = Camera.main;
        if (currentCamera == null)
            return;

        Vector3 worldPosition = transform.position + interactionOffset + screenPromptWorldOffset;
        Vector3 screenPosition = currentCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
            return;

        GUIStyle style = GetPromptStyle();
        string keyLabel = string.IsNullOrWhiteSpace(interactionKeyLabel) ? "E" : interactionKeyLabel;
        string label = $"[{keyLabel}] {GetPromptLabel()}";
        Vector2 size = style.CalcSize(new GUIContent(label));
        Rect rect = new Rect(
            screenPosition.x - (size.x * 0.5f),
            Screen.height - screenPosition.y - size.y,
            size.x,
            size.y
        );

        GUI.Label(rect, label, style);
    }

    private GUIStyle GetPromptStyle()
    {
        if (cachedPromptStyle != null)
            return cachedPromptStyle;

        cachedPromptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal =
            {
                textColor = Color.white
            }
        };

        return cachedPromptStyle;
    }

    private string GetPromptLabel()
    {
        return string.IsNullOrWhiteSpace(promptText) ? "Parla" : promptText;
    }

    private bool IsInteractionPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        if (!TryResolveKeyboardKey(interactionKeyLabel, out Key key))
            key = Key.E;

        return keyboard[key].isPressed;
    }

    private static bool TryResolveKeyboardKey(string keyLabel, out Key key)
    {
        key = Key.None;
        if (string.IsNullOrWhiteSpace(keyLabel))
            return false;

        string normalized = keyLabel.Trim().ToUpperInvariant();

        if (normalized.Length == 1)
        {
            char character = normalized[0];
            if (char.IsLetter(character))
                return System.Enum.TryParse(normalized, out key);

            if (char.IsDigit(character))
                return System.Enum.TryParse($"Digit{character}", out key);
        }

        switch (normalized)
        {
            case "SPACE":
                key = Key.Space;
                return true;
            case "SHIFT":
            case "LEFTSHIFT":
                key = Key.LeftShift;
                return true;
            case "RIGHTSHIFT":
                key = Key.RightShift;
                return true;
            case "CTRL":
            case "CONTROL":
            case "LEFTCTRL":
                key = Key.LeftCtrl;
                return true;
            case "RIGHTCTRL":
                key = Key.RightCtrl;
                return true;
            default:
                return false;
        }
    }

    private bool IsInventoryOpen()
    {
        return InventorySystem.Instance != null && InventorySystem.Instance.IsInventoryOpen;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position + interactionOffset, Mathf.Max(0.1f, interactionDistance));
    }

    private void OnDestroy()
    {
        if (subscribedDialogBehaviour != null)
        {
            subscribedDialogBehaviour.AnswerNodeActivated -= HandleAnswerNodeActivated;
            subscribedDialogBehaviour.SentenceNodeActivated -= HandleSentenceNodeActivated;
        }

        SetHotbarVisible(true);
        RestoreCursorState();
    }
}
