using UnityEngine;

public class BirdFly : MonoBehaviour
{
    [Header("Velocità")]
    public float moveSpeed = 3f;        // velocità orizzontale
    public float verticalSpeed = 1.5f;  // velocità verticale
    public float changeDirTime = 4f;    // ogni quanti secondi cambia direzione

    [Header("Limiti di volo")]
    public float minHeight = 2f;
    public float maxHeight = 8f;
    public float wanderRadius = 5f;     // raggio di movimento orizzontale

    private Vector3 targetPos;
    private float timer;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        ChooseNewTarget();
        timer = changeDirTime;
    }

    void Update()
    {
        // sempre muovi = 1
        anim.SetInteger("muovi", 1);

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ChooseNewTarget();
        }

        // direzione verso target
        Vector3 dir = targetPos - transform.position;
        dir.Normalize();

        // movimento orizzontale e verticale
        Vector3 move = dir * moveSpeed * Time.deltaTime;

        // controlla altezza
        float nextY = transform.position.y + move.y;
        nextY = Mathf.Clamp(nextY, minHeight, maxHeight);
        move.y = nextY - transform.position.y;

        transform.position += move;

        // rotazione verso il target (solo orizzontale per più naturale)
        Vector3 lookDir = targetPos - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }
    }

    void ChooseNewTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        float randomHeight = Random.Range(minHeight, maxHeight);
        targetPos = new Vector3(transform.position.x + randomCircle.x, randomHeight, transform.position.z + randomCircle.y);
        timer = changeDirTime;
    }
}