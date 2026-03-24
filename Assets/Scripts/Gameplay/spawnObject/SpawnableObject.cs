using UnityEngine;
using UnityEngine.Serialization;

[AddComponentMenu("OurGame/World/Spawnable Object")]
[RequireComponent(typeof(Collider))]
public class SpawnableObject : MonoBehaviour
{
    [Header("Collision State")]
    [FormerlySerializedAs("FornisciCollisioni")]
    [SerializeField] private bool startWithCollisionEnabled;
    protected Collider col;

    protected virtual void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
            col.enabled = startWithCollisionEnabled;
    }

    public virtual void Activate()
    {
        if (col != null)
            col.enabled = true;
    }

    public virtual void Deactivate()
    {
        if (col != null)
            col.enabled = false;
    }
}
