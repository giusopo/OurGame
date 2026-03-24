using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    [Header("Entity")]
    [SerializeField] private Animator animator;

    protected Animator Animator => animator;

    protected virtual void Awake()
    {
        CacheAnimatorReference();
    }

    protected void CacheAnimatorReference()
    {
        if (animator != null)
            return;

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    protected Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction;
    }

    protected void MoveInDirection(Vector3 direction, float speed)
    {
        Vector3 planarDirection = FlattenDirection(direction);
        if (planarDirection.sqrMagnitude <= Mathf.Epsilon || speed <= 0f)
            return;

        transform.position += planarDirection.normalized * speed * Time.deltaTime;
    }

    protected void RotateTowards(Vector3 direction, float rotationSpeed)
    {
        Vector3 planarDirection = FlattenDirection(direction);
        if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    protected void SetAnimatorInt(string parameterName, int value)
    {
        if (animator != null)
            animator.SetInteger(parameterName, value);
    }
}
