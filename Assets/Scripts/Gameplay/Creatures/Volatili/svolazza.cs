using UnityEngine;

public class Fly : MonoBehaviour
{
    [Header("Velocità")]
    public float moveSpeed = 3f;
    public float changeDirTime = 4f;

    [Header("Limiti di volo")]
    public float minHeight = 70f;
    public float maxHeight = 100f;
    public float wanderRadius = 5f;

    private Vector3 centerPoint;
    private Vector3 targetPos;
    private float timer;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();

        centerPoint = transform.position; // 🔥 base stabile
        ChooseNewTarget();

        timer = changeDirTime;
    }

    void Update()
    {
        anim.SetInteger("muovi", 1);

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ChooseNewTarget();
        }

        Vector3 dir = (targetPos - transform.position);

        // movimento stabile
        transform.position += dir.normalized * moveSpeed * Time.deltaTime;

        // clamp altezza SOLO dopo movimento
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;

        // rotazione naturale
        Vector3 lookDir = targetPos - transform.position;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 2f);
        }
    }

    void ChooseNewTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;

        targetPos = centerPoint + new Vector3(
            randomCircle.x,
            Random.Range(minHeight, maxHeight),
            randomCircle.y
        );

        timer = changeDirTime;
    }
}