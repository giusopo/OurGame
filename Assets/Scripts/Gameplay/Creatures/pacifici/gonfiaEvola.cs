using UnityEngine;

public class CreatureBehavior : PacificEntity
{
    [Header("Distanze")]
    public float detectionDistance = 10f;

    [Header("Scala")]
    public float growScale = 1.5f;
    public float normalScale = 1f;
    public float shrinkScale = 0.5f;
    public float scaleSpeed = 3f;

    [Header("Forza")]
    public float pushForce = 10f;

    private Vector3 originalScale;

    protected override void Awake()
    {
        base.Awake();
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (!TryGetDistanceToPlayer(out float distance))
            return;

        float halfDistance = detectionDistance * 0.5f;

        if (distance < detectionDistance)
            FacePlayerOnPlane();

        if (distance >= detectionDistance)
        {
            SetPlayerState(0);
            LerpScale(normalScale);
            return;
        }

        SetPlayerState(distance < halfDistance ? 2 : 1);
        LerpScale(growScale);

        if (distance < halfDistance * 0.2f)
        {
            LerpScale(shrinkScale);
            PushPlayer();
        }
    }

    private void SetPlayerState(int state)
    {
        SetAnimatorInt("playerVicino", state);
    }

    private void LerpScale(float multiplier)
    {
        Vector3 targetScale = originalScale * multiplier;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    private void PushPlayer()
    {
        if (Player == null)
            return;

        Vector3 direction = (Player.position - transform.position).normalized;
        direction.y = 0.5f;
        AddForceToPlayer(direction, pushForce);
    }
}
