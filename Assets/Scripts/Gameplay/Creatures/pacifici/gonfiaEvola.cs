using UnityEngine;

public class CreatureBehavior : MonoBehaviour
{
    private Transform player;

    [Header("Distanze")]
    public float detectionDistance = 10f;

    [Header("Scala")]
    public float growScale = 1.5f;
    public float normalScale = 1f;
    public float shrinkScale = 0.5f;
    public float scaleSpeed = 3f;

    [Header("Forza")]
    public float pushForce = 10f;

    [Header("Animazione")]
    public Animator animator;

    private Vector3 originalScale;
    private Rigidbody playerRb;

    void Start()
    {
        originalScale = transform.localScale;

        if (animator == null)
            animator = GetComponent<Animator>();

        FindPlayer();
    }

    void Update()
    {
        // 🔥 sicurezza: se player non esiste ancora o è stato spawnato dopo
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        float halfDistance = detectionDistance * 0.5f;

        // 👀 guarda il player
        if (distance < detectionDistance)
        {
            Vector3 lookPos = player.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        // 🎯 DISTANZA GRANDE
        if (distance >= detectionDistance)
        {
            animator.SetInteger("playerVicino", 0);

            Vector3 targetScale = originalScale * normalScale;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
            );

            return;
        }

        // 🎯 DISTANZA MEDIA
        if (distance < detectionDistance)
        {
            animator.SetInteger("playerVicino", 1);

            Vector3 targetScale = originalScale * growScale;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
            );
        }

        // 🎯 DISTANZA VICINA
        if (distance < halfDistance)
        {
            animator.SetInteger("playerVicino", 2);
        }

        // 💥 DISTANZA MOLTO VICINA
        if (distance < halfDistance * 0.2f)
        {
            Vector3 targetScale = originalScale * shrinkScale;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
            );

            PushPlayer();
        }
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;
            playerRb = p.GetComponent<Rigidbody>();
        }
    }

    void PushPlayer()
    {
        if (playerRb == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0.5f;

        playerRb.AddForce(direction * pushForce, ForceMode.Impulse);
    }
}