using UnityEngine;

public abstract class PacificEntity : Entity
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    protected Transform Player { get; private set; }
    protected Rigidbody PlayerRigidbody { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        FindPlayer();
    }

    protected bool EnsurePlayerReference()
    {
        if (Player != null)
            return true;

        FindPlayer();
        return Player != null;
    }

    protected bool TryGetDistanceToPlayer(out float distance)
    {
        distance = float.PositiveInfinity;
        if (!EnsurePlayerReference())
            return false;

        distance = Vector3.Distance(transform.position, Player.position);
        return true;
    }

    protected Vector3 GetDirectionAwayFromPlayer()
    {
        return Player == null ? Vector3.zero : transform.position - Player.position;
    }

    protected void FacePlayerOnPlane()
    {
        if (Player == null)
            return;

        Vector3 lookPosition = Player.position;
        lookPosition.y = transform.position.y;
        transform.LookAt(lookPosition);
    }

    protected void AddForceToPlayer(Vector3 direction, float force, ForceMode forceMode = ForceMode.Impulse)
    {
        if (PlayerRigidbody == null || direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        PlayerRigidbody.AddForce(direction * force, forceMode);
    }

    private void FindPlayer()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
            return;

        Player = playerObject.transform;
        PlayerRigidbody = playerObject.GetComponent<Rigidbody>();
    }
}
