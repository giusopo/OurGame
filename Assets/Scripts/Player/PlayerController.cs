using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float rotationSpeed = 100f; // gradi al secondo per ruotare

    private CharacterController controller;
    private Animator anim;
    private Vector2 moveInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Movimento in avanti/indietro relativo alla direzione del player
        Vector3 forward = transform.forward;
        Vector3 move = forward * moveInput.y; // solo asse Y del moveInput per avanti/indietro

        controller.Move(move * speed * Time.deltaTime);

        // Rotazione solo asse X del moveInput (A/D o frecce)
        if (moveInput.x != 0)
        {
            transform.Rotate(Vector3.up, moveInput.x * rotationSpeed * Time.deltaTime);
        }

        // Animazioni
        anim.SetInteger("AnimationPar", moveInput.y != 0 ? 1 : 0);
    }

    // Input System
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}