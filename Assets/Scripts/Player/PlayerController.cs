using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 100f;

    private Rigidbody rb;
    private Animator anim;

    float moveForward;
    float turn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        moveForward = Input.GetAxis("Vertical");   // W S
        turn = Input.GetAxis("Horizontal");        // A D

        if (Mathf.Abs(moveForward) > 0.1f)
            anim.SetInteger("AnimationPar", 1);
        else
            anim.SetInteger("AnimationPar", 0);
    }

    void FixedUpdate()
    {
        // Movimento avanti / indietro
        Vector3 move = transform.forward * moveForward * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // Rotazione SOLO con A/D
        float rotation = turn * rotationSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0, rotation, 0);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
}