using UnityEngine;

public abstract class FlyingEntity : PacificEntity
{
    [Header("Flight")]
    public float moveSpeed = 3f;
    public float changeDirTime = 4f;
    [SerializeField] private float verticalMoveSpeed = 2f;

    [Header("Limiti di volo")]
    public float minHeight = 70f;
    public float maxHeight = 100f;
    public float wanderRadius = 5f;

    [Header("Flight Tuning")]
    [SerializeField] private float horizontalArrivalDistance = 0.35f;
    [SerializeField] private float verticalArrivalDistance = 0.2f;
    [SerializeField] private float rotationSpeed = 2f;

    protected Vector3 centerPoint;
    protected Vector3 targetPos;

    private Rigidbody cachedRigidbody;
    private float timer;

    protected override void Awake()
    {
        base.Awake();
        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody != null)
            cachedRigidbody.useGravity = false;
    }

    protected virtual void Start()
    {
        centerPoint = transform.position;
        ChooseNewTarget();
        timer = changeDirTime;
    }

    protected virtual void Update()
    {
        SetAnimatorInt("muovi", 1);

        timer -= Time.deltaTime;
        if (timer <= 0f || HasReachedTarget())
            ChooseNewTarget();

        Vector3 horizontalDirection = GetHorizontalDirectionToTarget();
        RotateTowards(horizontalDirection, rotationSpeed);
    }

    protected virtual void FixedUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition;

        Vector3 horizontalDirection = GetHorizontalDirectionToTarget();
        if (horizontalDirection.sqrMagnitude > Mathf.Epsilon)
        {
            nextPosition += horizontalDirection.normalized * moveSpeed * Time.fixedDeltaTime;
        }

        float clampedTargetHeight = Mathf.Clamp(targetPos.y, minHeight, maxHeight);
        nextPosition.y = Mathf.MoveTowards(
            currentPosition.y,
            clampedTargetHeight,
            verticalMoveSpeed * Time.fixedDeltaTime
        );
        nextPosition.y = Mathf.Clamp(nextPosition.y, minHeight, maxHeight);

        if (cachedRigidbody != null)
            cachedRigidbody.MovePosition(nextPosition);
        else
            transform.position = nextPosition;
    }

    protected virtual void ChooseNewTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPos = centerPoint + new Vector3(
            randomCircle.x,
            Random.Range(minHeight, maxHeight),
            randomCircle.y
        );
        timer = changeDirTime;
    }

    private Vector3 GetHorizontalDirectionToTarget()
    {
        Vector3 horizontalDirection = targetPos - transform.position;
        horizontalDirection.y = 0f;
        return horizontalDirection;
    }

    private bool HasReachedTarget()
    {
        Vector3 horizontalOffset = GetHorizontalDirectionToTarget();
        float targetHeight = Mathf.Clamp(targetPos.y, minHeight, maxHeight);
        float verticalOffset = Mathf.Abs(transform.position.y - targetHeight);

        return horizontalOffset.sqrMagnitude <= horizontalArrivalDistance * horizontalArrivalDistance
            && verticalOffset <= verticalArrivalDistance;
    }
}
