using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpawnableObject : MonoBehaviour
{

    public bool FornisciCollisioni = false;
    protected Collider col;

    protected virtual void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
            col.enabled = FornisciCollisioni; // disattivo di default
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