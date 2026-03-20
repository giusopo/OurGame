using UnityEngine;

public class Scappa : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Distanze")]
    public float nearDistance = 10f;
    public float veryNearDistance = 4f;

    [Header("Velocità")]
    public float slowSpeed = 2f;
    public float runSpeed = 6f;
    public float wanderSpeed = 2f;

    [Header("Movimento autonomo")]
    public float wanderRadius = 5f;
    public float wanderTime = 3f;
    public float stopDistance = 0.5f;

    private Animator anim;
    private Vector3 wanderTarget;
    private float wanderTimer;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        wanderTimer = wanderTime;
        ChooseNewWanderTarget();

        // 🔥 AUTO-ASSIGN PLAYER
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
            else
                Debug.LogWarning("Player non trovato! Assegna il tag 'Player'");
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        int scappa = 0;
        float speed = 0f;
        Vector3 dir = Vector3.zero;

        // -------- PLAYER MOLTO VICINO --------
        if (distance < veryNearDistance)
        {
            scappa = 2;
            speed = runSpeed;
            dir = transform.position - player.position;
        }
        // -------- PLAYER VICINO --------
        else if (distance < nearDistance)
        {
            scappa = 1;
            speed = slowSpeed;
            dir = transform.position - player.position;
        }
        // -------- MOVIMENTO AUTONOMO --------
        else
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0f)
                ChooseNewWanderTarget();

            dir = wanderTarget - transform.position;

            if (dir.magnitude > stopDistance)
            {
                scappa = 1;
                speed = wanderSpeed;
            }
            else
            {
                scappa = 0;
                speed = 0;
            }
        }

        anim.SetInteger("scappa", scappa);

        // -------- MOVIMENTO --------
        if (speed > 0)
        {
            dir.y = 0;
            dir.Normalize();

            transform.position += dir * speed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 5f
            );
        }
    }

    void ChooseNewWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        wanderTarget = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        wanderTimer = wanderTime;
    }
}