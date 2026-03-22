using UnityEngine;

public class ScappaAI : MonoBehaviour
{
    [Header("Player")]
    private Transform player;

    [Header("Distanze")]
    public float nearDistance = 10f;
    public float veryNearDistance = 4f;

    [Header("Velocità")]
    public float slowSpeed = 2f;
    public float runSpeed = 6f;
    public float wanderSpeed = 2f;

    [Header("Wander")]
    public float wanderRadius = 5f;
    public float wanderTime = 3f;

    private Vector3 wanderTarget;
    private float wanderTimer;

    // OUTPUT
    public Vector3 Direction { get; private set; }
    public float Speed { get; private set; }
    public int ScappaState { get; private set; }

    void Start()
    {
        wanderTimer = wanderTime;
        ChooseNewWanderTarget();
        FindPlayer();
    }

    void Update()
    {
        // 🔥 sicurezza: se player sparisce o non è ancora stato trovato
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // PLAYER MOLTO VICINO
        if (distance < veryNearDistance)
        {
            ScappaState = 2;
            Speed = runSpeed;
            Direction = transform.position - player.position;
        }
        // PLAYER VICINO
        else if (distance < nearDistance)
        {
            ScappaState = 1;
            Speed = slowSpeed;
            Direction = transform.position - player.position;
        }
        // WANDER
        else
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0f)
                ChooseNewWanderTarget();

            Direction = wanderTarget - transform.position;

            if (Direction.magnitude > 0.5f)
            {
                ScappaState = 1;
                Speed = wanderSpeed;
            }
            else
            {
                ScappaState = 0;
                Speed = 0f;
            }
        }
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;
    }

    void ChooseNewWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        wanderTarget = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        wanderTimer = wanderTime;
    }
}