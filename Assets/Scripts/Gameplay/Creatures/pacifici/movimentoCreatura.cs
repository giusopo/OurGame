using UnityEngine;

public class ScappaMovement : Entity
{
    private ScappaAI ai;

    public float rotationSpeed = 5f;

    protected override void Awake()
    {
        base.Awake();
        ai = GetComponent<ScappaAI>();
    }

    void Update()
    {
        if (ai == null)
            return;

        SetAnimatorInt("scappa", ai.ScappaState);

        if (ai.Speed <= 0f || ai.Direction == Vector3.zero)
            return;

        MoveInDirection(ai.Direction, ai.Speed);
        RotateTowards(ai.Direction, rotationSpeed);
    }
}
