using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float velocita = 5f;
    public float rotationSpeed = 100f;

    private float speed;

    private Rigidbody rb;
    private Animator anim;

    float moveForward;
    float turn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (InventorySystem.Instance.IsInventoryOpen)
        {
            moveForward = 0f;
            turn = 0f;
            speed = 0f;
            anim.SetInteger("Movimento", 0);
            anim.SetFloat("Rotazione", 0f);
            return;
        }

        moveForward = Input.GetAxis("Vertical");
        turn = Input.GetAxis("Horizontal");

        bool isMovingForward = Input.GetKey(KeyCode.W);
        bool isRunning = isMovingForward && Input.GetKey(KeyCode.LeftShift);

        // 🎮 MOVIMENTO (Idle / Walk / Run)
        if (isRunning)
        {
            anim.SetInteger("Movimento", 2); // CORSA
            speed = velocita * 2;
        }
        else if (isMovingForward)
        {
            anim.SetInteger("Movimento", 1); // CAMMINA
            speed = velocita;
        }
        else
        {
            anim.SetInteger("Movimento", 0); // IDLE
            speed = 0;
        }

        // 🔄 ROTAZIONE (animazione)
        anim.SetFloat("Rotazione", turn);

        // 🌫️ CADUTA (usa linearVelocity aggiornato)
        float yVel = rb.linearVelocity.y;

        if (yVel < -11f)
        {
            anim.SetInteger("Caduta", 1);
        }
        else
        {
            anim.SetInteger("Caduta", 0);
        }
    }

    void FixedUpdate()
    {
        // Collisions should never be allowed to spin the player body.
        rb.angularVelocity = Vector3.zero;

        float currentYaw = rb.rotation.eulerAngles.y;
        float targetYaw = currentYaw;

        if (InventorySystem.Instance.IsInventoryOpen)
        {
            rb.MoveRotation(Quaternion.Euler(0f, targetYaw, 0f));
            return;
        }

        // movimento orizzontale stabile
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 move = forward * moveForward * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // rotazione controllata SOLO su Y
        float rotation = turn * rotationSpeed * Time.fixedDeltaTime;
        targetYaw += rotation;
        rb.MoveRotation(Quaternion.Euler(0f, targetYaw, 0f));
    }
}
