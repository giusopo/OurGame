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
    private float targetYaw = 0f;

    private float currentPitch = 10f;

    void LateUpdate()
    {
        // INPUT MOUSE (tasto destro premuto)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentYaw += mouseX;

            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            targetYaw = currentYaw;
        }
        else
        {
            // ritorno graduale dietro al player (yaw)
            float playerYaw = target.eulerAngles.y;

            targetYaw = Mathf.LerpAngle(targetYaw, playerYaw, Time.deltaTime * returnSpeed);
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * returnSpeed);

            // opzionale: ritorno leggero del pitch verso default
            float defaultPitch = 10f;
            currentPitch = Mathf.Lerp(currentPitch, defaultPitch, Time.deltaTime * returnSpeed);
        }

        // rotazione camera completa (yaw + pitch)
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // offset camera
        Vector3 offset = rotation * new Vector3(0, height, -distance);

        transform.position = target.position + offset;

        // applica rotazione direttamente
        transform.rotation = rotation;
    }
}