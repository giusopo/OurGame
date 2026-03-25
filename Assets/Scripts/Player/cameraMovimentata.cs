using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // player

    public float distance = 10f;
    public float height = 3f;

    public float mouseSensitivity = 3f;
    public float returnSpeed = 4f;

    public float minPitch = -20f;
    public float maxPitch = 60f;

    private float currentYaw = 0f;
    private float currentPitch = 10f;
    private float lastTargetYaw;
    private bool initialized;

    void Start()
    {
        InitializeFromTarget();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        InitializeFromTarget();

        bool inventoryOpen = InventorySystem.Instance != null && InventorySystem.Instance.IsInventoryOpen;
        bool freeCursorMode = inventoryOpen || Input.GetMouseButton(1);

        ApplyCursorState(freeCursorMode);

        float playerYaw = target.eulerAngles.y;
        float playerYawDelta = Mathf.DeltaAngle(lastTargetYaw, playerYaw);
        currentYaw += playerYawDelta;
        lastTargetYaw = playerYaw;

        // Mouse-look always active unless the player is intentionally freeing the cursor.
        if (!freeCursorMode)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentYaw += mouseX;

            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        // rotazione camera completa (yaw + pitch)
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // offset camera
        Vector3 offset = rotation * new Vector3(0, height, -distance);

        transform.position = target.position + offset;

        // applica rotazione direttamente
        transform.rotation = rotation;
    }

    private void InitializeFromTarget()
    {
        if (initialized || target == null)
            return;

        currentYaw = target.eulerAngles.y;
        lastTargetYaw = currentYaw;
        initialized = true;
    }

    private void ApplyCursorState(bool freeCursorMode)
    {
        Cursor.visible = freeCursorMode;
        Cursor.lockState = freeCursorMode ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
