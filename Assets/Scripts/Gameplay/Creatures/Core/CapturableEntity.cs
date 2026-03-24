using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("OurGame/Creatures/Capturable Entity")]
[DisallowMultipleComponent]
public class CapturableEntity : MonoBehaviour
{
    private static readonly List<CapturableEntity> ActiveCapturables = new List<CapturableEntity>();

    [Header("Capture")]
    [SerializeField] private float captureDistance = 1.5f;
    [SerializeField] private string promptLabel = "Cattura";
    [SerializeField] private bool hideOnCapture = true;

    private PacificEntity pacificEntity;
    private bool isCaptured;

    public string PromptLabel => promptLabel;
    public bool IsAvailable => !isCaptured && isActiveAndEnabled && gameObject.activeInHierarchy;

    void Awake()
    {
        pacificEntity = GetComponent<PacificEntity>();
        if (pacificEntity == null)
        {
            Debug.LogWarning(
                $"CapturableEntity on '{name}' expects a component derived from PacificEntity on the same GameObject."
            );
        }
    }

    void OnEnable()
    {
        if (!ActiveCapturables.Contains(this))
            ActiveCapturables.Add(this);
    }

    void OnDisable()
    {
        ActiveCapturables.Remove(this);
    }

    public bool IsInCaptureRange(Vector3 playerPosition)
    {
        if (!IsAvailable)
            return false;

        return Vector3.Distance(transform.position, playerPosition) <= captureDistance;
    }

    public bool TryCapture()
    {
        if (!IsAvailable)
            return false;

        isCaptured = true;

        Debug.Log($"Player captured entity '{name}'.");

        // TODO: definire cosa succede dopo la cattura.
        // Per ora l'entita viene soltanto rimossa dalla scena.

        if (hideOnCapture)
            gameObject.SetActive(false);

        return true;
    }

    public static CapturableEntity GetClosestAvailable(Vector3 playerPosition)
    {
        CapturableEntity best = null;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < ActiveCapturables.Count; i++)
        {
            CapturableEntity candidate = ActiveCapturables[i];
            if (candidate == null || !candidate.IsAvailable)
                continue;

            if (!candidate.IsInCaptureRange(playerPosition))
                continue;

            float distance = (candidate.transform.position - playerPosition).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = candidate;
        }

        return best;
    }
}
