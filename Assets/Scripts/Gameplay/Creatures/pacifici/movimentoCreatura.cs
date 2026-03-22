using UnityEngine;

public class ScappaMovement : MonoBehaviour
{
    private ScappaAI ai;
    private Animator anim;

    public float rotationSpeed = 5f;

    void Start()
    {
        ai = GetComponent<ScappaAI>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (ai == null) return;

        Vector3 dir = ai.Direction;
        float speed = ai.Speed;

        anim.SetInteger("scappa", ai.ScappaState);

        if (speed > 0 && dir != Vector3.zero)
        {
            dir.y = 0;
            dir.Normalize();

            transform.position += dir * speed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }
}