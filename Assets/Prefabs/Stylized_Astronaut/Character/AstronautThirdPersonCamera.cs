using UnityEngine;

public class AstronautThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 4.5f;

    public float mouseSensitivity = 120f;
    public float keyboardRotationSpeed = 90f;

    public float minPitch = -20f;
    public float maxPitch = 60f;

    public float followSmooth = 10f;

    float yaw;
    float pitch = 20f;

    void LateUpdate()
    {
        if (!target) return;
        bool inventoryOpen = InventorySystem.Instance.IsInventoryOpen;

        // --- MOUSE ROTATION ---
        float mouseX = inventoryOpen ? 0f : Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = inventoryOpen ? 0f : Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;

        // --- KEYBOARD ROTATION (Q/E) ---
        if (!inventoryOpen && Input.GetKey(KeyCode.Q))
            yaw -= keyboardRotationSpeed * Time.deltaTime;

        if (!inventoryOpen && Input.GetKey(KeyCode.E))
            yaw += keyboardRotationSpeed * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 desiredPosition =
            target.position - rotation * Vector3.forward * distance;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSmooth * Time.deltaTime
        );

        transform.LookAt(target.position);
    }
}
