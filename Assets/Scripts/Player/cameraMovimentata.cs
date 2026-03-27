using UnityEngine;
using OurGame.Systems;

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
    private float currentPitch = 100f;
    private float lastTargetYaw;
    private Vector3 localOrbitOffset;
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

        bool inventoryOpen = BackpackInventorySystem.Instance != null && BackpackInventorySystem.Instance.IsInventoryOpen;
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

        // Use the scene-authored camera placement as the base orbit offset.
        Vector3 offset = rotation * localOrbitOffset;

        transform.position = target.position + offset;

        // applica rotazione direttamente
        transform.rotation = rotation;
    }

    private void InitializeFromTarget()
    {
        if (initialized || target == null)
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
        initialized = true;
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
}
