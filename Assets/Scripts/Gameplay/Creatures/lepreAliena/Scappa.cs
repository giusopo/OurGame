using UnityEngine;

public class Scappa : MonoBehaviour
{
    public Transform player;

    [Header("Distanze per scappare")]
    public float nearDistance = 10f;
    public float veryNearDistance = 4f;

    [Header("Velocità")]
    public float slowSpeed = 2f;     // scappa piano
    public float runSpeed = 6f;      // scappa veloce
    public float wanderSpeed = 2f;   // velocità movimento autonomo

    [Header("Movimento autonomo")]
    public float wanderRadius = 5f;
    public float wanderTime = 3f;

    private Animator anim;
    private Vector3 wanderTarget;
    private float wanderTimer;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        wanderTimer = wanderTime;
        ChooseNewWanderTarget();
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        int scappa;
        float speed;
        Vector3 dir = Vector3.zero;

        // ---------- COMPORTAMENTO SCAPPA ----------
        if (distance < veryNearDistance)
        {
            scappa = 2;
            speed = runSpeed;
            dir = transform.position - player.position;
        }
        else
        {
            // scappa piano sia se il player è vicino sia durante il movimento autonomo
            scappa = 1;
            speed = slowSpeed;

            if (distance < nearDistance)
            {
                // vicino al player, cammina/scappa piano
                dir = transform.position - player.position;
            }
            else
            {
                // lontano dal player → movimento autonomo
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                {
                    ChooseNewWanderTarget();
                }
                dir = wanderTarget - transform.position;
            }
        }

        anim.SetInteger("scappa", scappa);

        // movimento
        if (dir != Vector3.zero)
        {
            dir.y = 0;
            dir.Normalize();

            transform.position += dir * speed * Time.deltaTime;

            // rotazione solo orizzontale
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void ChooseNewWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        wanderTarget = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        wanderTimer = wanderTime;
    }
}