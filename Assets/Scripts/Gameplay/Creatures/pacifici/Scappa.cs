using UnityEngine;

public class ScappaAI : PacificEntity
{
    [Header("Distanze")]
    public float nearDistance = 10f;
    public float veryNearDistance = 4f;

    [Header("Velocita")]
    public float slowSpeed = 2f;
    public float runSpeed = 6f;
    public float wanderSpeed = 2f;

    [Header("Wander")]
    public float wanderRadius = 5f;
    public float wanderTime = 3f;

    private Vector3 wanderTarget;
    private float wanderTimer;

    public Vector3 Direction { get; private set; }
    public float Speed { get; private set; }
    public int ScappaState { get; private set; }

    void Start()
    {
        wanderTimer = wanderTime;
        ChooseNewWanderTarget();
    }

    void Update()
    {
        if (!TryGetDistanceToPlayer(out float distance))
            return;

        if (distance < veryNearDistance)
        {
            ScappaState = 2;
            Speed = runSpeed;
            Direction = GetDirectionAwayFromPlayer();
            return;
        }

        if (distance < nearDistance)
        {
            ScappaState = 1;
            Speed = slowSpeed;
            Direction = GetDirectionAwayFromPlayer();
            return;
        }

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

    void ChooseNewWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        wanderTarget = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        wanderTimer = wanderTime;
    }
}
