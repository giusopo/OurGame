using UnityEngine;
using OurGame.Systems;

public class CameraController : MonoBehaviour
{
    private enum InventoryFocusState
    {
        Orbit,
        Entering,
        Focused,
        Exiting
    }

    public Transform target; // player

    public float distance = 10f;
    public float height = 3f;

    public float mouseSensitivity = 3f;
    public float returnSpeed = 4f;

    public float minPitch = -20f;
    public float maxPitch = 60f;

    [Header("Inventory Focus")]
    [SerializeField] private float inventoryFocusDuration = 0.35f;
    [SerializeField] private float inventoryReturnDuration = 0.3f;
    [SerializeField] private float inventoryFocusFov = 38f;

    private float currentYaw;
    private float currentPitch = 100f;
    private float lastTargetYaw;
    private Vector3 localOrbitOffset;
    private bool initialized;

    private Camera attachedCamera;
    private BackpackInventorySystem inventorySystem;
    private InventoryFocusState inventoryFocusState = InventoryFocusState.Orbit;
    private bool hasSavedCameraState;
    private float transitionElapsed;
    private float activeTransitionDuration;
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private float transitionStartFov;
    private Vector3 transitionTargetPosition;
    private Quaternion transitionTargetRotation;
    private float transitionTargetFov;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float savedFov;

    void Start()
    {
        InitializeFromTarget();
        CacheCamera();
        TryBindInventorySystem();
    }

    void OnEnable()
    {
        TryBindInventorySystem();
    }

    void OnDisable()
    {
        UnbindInventorySystem();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        InitializeFromTarget();
        CacheCamera();
        TryBindInventorySystem();

        bool inventoryOpen = BackpackInventorySystem.Instance != null && BackpackInventorySystem.Instance.IsInventoryOpen;
        bool freeCursorMode = inventoryOpen || Input.GetMouseButton(1) || inventoryFocusState != InventoryFocusState.Orbit;

        ApplyCursorState(freeCursorMode);

        if (inventoryFocusState != InventoryFocusState.Orbit)
        {
            UpdateInventoryFocusTransition();
            return;
        }

        float playerYaw = target.eulerAngles.y;
        float playerYawDelta = Mathf.DeltaAngle(lastTargetYaw, playerYaw);
        currentYaw += playerYawDelta;
        lastTargetYaw = playerYaw;

        if (!freeCursorMode)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentYaw += mouseX;

            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 offset = rotation * localOrbitOffset;

        transform.position = target.position + offset;
        transform.rotation = rotation;
    }

    private void InitializeFromTarget()
    {
        if (initialized || target == null)
            return;

        CaptureOrbitStateFromCurrentPose();
        initialized = true;
    }

    private void CaptureOrbitStateFromCurrentPose()
    {
        if (target == null)
            return;

        Vector3 worldOffset = transform.position - target.position;
        Vector3 euler = transform.rotation.eulerAngles;

        currentYaw = euler.y;
        currentPitch = NormalizeAngle(euler.x);
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        lastTargetYaw = target.eulerAngles.y;

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        localOrbitOffset = Quaternion.Inverse(rotation) * worldOffset;

        if (localOrbitOffset.sqrMagnitude < 0.0001f)
            localOrbitOffset = new Vector3(0f, height, -Mathf.Max(0.01f, distance));

        height = localOrbitOffset.y;
        distance = Mathf.Max(0.01f, Mathf.Abs(localOrbitOffset.z));
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return angle;
    }

    private void ApplyCursorState(bool freeCursorMode)
    {
        Cursor.visible = freeCursorMode;
        Cursor.lockState = freeCursorMode ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void CacheCamera()
    {
        attachedCamera ??= GetComponent<Camera>();
    }

    private void TryBindInventorySystem()
    {
        BackpackInventorySystem resolvedSystem = BackpackInventorySystem.Instance;
        if (resolvedSystem == null || resolvedSystem == inventorySystem)
            return;

        UnbindInventorySystem();
        inventorySystem = resolvedSystem;
        inventorySystem.OnPocketOpened += HandlePocketOpened;
        inventorySystem.OnPocketClosed += HandlePocketClosed;
    }

    private void UnbindInventorySystem()
    {
        if (inventorySystem == null)
            return;

        inventorySystem.OnPocketOpened -= HandlePocketOpened;
        inventorySystem.OnPocketClosed -= HandlePocketClosed;
        inventorySystem = null;
    }

    private void HandlePocketOpened(string pocketName)
    {
        Transform focusAnchor = ResolvePocketCameraAnchor(pocketName);
        if (focusAnchor == null)
        {
            Debug.LogWarning(
                $"CameraController could not find camera anchor '{GetPocketCameraAnchorName(pocketName)}' for pocket '{pocketName}'."
            );
            return;
        }

        BeginInventoryFocus(focusAnchor);
    }

    private void HandlePocketClosed()
    {
        EndInventoryFocus();
    }

    private void BeginInventoryFocus(Transform focusAnchor)
    {
        CacheCamera();
        if (focusAnchor == null || attachedCamera == null)
            return;

        if (!hasSavedCameraState)
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
            savedFov = attachedCamera.fieldOfView;
            hasSavedCameraState = true;
        }

        StartTransition(
            transform.position,
            transform.rotation,
            attachedCamera.fieldOfView,
            focusAnchor.position,
            focusAnchor.rotation,
            inventoryFocusFov,
            Mathf.Max(0.01f, inventoryFocusDuration),
            InventoryFocusState.Entering
        );
    }

    private void EndInventoryFocus()
    {
        CacheCamera();
        if (!hasSavedCameraState || attachedCamera == null)
            return;

        StartTransition(
            transform.position,
            transform.rotation,
            attachedCamera.fieldOfView,
            savedPosition,
            savedRotation,
            savedFov,
            Mathf.Max(0.01f, inventoryReturnDuration),
            InventoryFocusState.Exiting
        );
    }

    private void StartTransition(
        Vector3 startPosition,
        Quaternion startRotation,
        float startFov,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float targetFov,
        float duration,
        InventoryFocusState state
    )
    {
        transitionElapsed = 0f;
        activeTransitionDuration = duration;
        transitionStartPosition = startPosition;
        transitionStartRotation = startRotation;
        transitionStartFov = startFov;
        transitionTargetPosition = targetPosition;
        transitionTargetRotation = targetRotation;
        transitionTargetFov = targetFov;
        inventoryFocusState = state;
    }

    private void UpdateInventoryFocusTransition()
    {
        CacheCamera();
        if (attachedCamera == null)
            return;

        transitionElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.01f, activeTransitionDuration);
        float t = Mathf.Clamp01(transitionElapsed / duration);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(transitionStartPosition, transitionTargetPosition, smoothT);
        transform.rotation = Quaternion.Slerp(transitionStartRotation, transitionTargetRotation, smoothT);
        attachedCamera.fieldOfView = Mathf.Lerp(transitionStartFov, transitionTargetFov, smoothT);

        if (t < 1f)
            return;

        transform.position = transitionTargetPosition;
        transform.rotation = transitionTargetRotation;
        attachedCamera.fieldOfView = transitionTargetFov;

        if (inventoryFocusState == InventoryFocusState.Exiting)
        {
            inventoryFocusState = InventoryFocusState.Orbit;
            hasSavedCameraState = false;
            CaptureOrbitStateFromCurrentPose();
            return;
        }

        inventoryFocusState = InventoryFocusState.Focused;
    }

    private Transform ResolvePocketCameraAnchor(string pocketName)
    {
        string anchorName = GetPocketCameraAnchorName(pocketName);
        if (string.IsNullOrWhiteSpace(anchorName))
            return null;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != anchorName)
                continue;

            return candidate;
        }

        return null;
    }

    private static string GetPocketCameraAnchorName(string pocketName)
    {
        return string.IsNullOrWhiteSpace(pocketName)
            ? string.Empty
            : $"{pocketName}CameraAnchor";
    }
}
