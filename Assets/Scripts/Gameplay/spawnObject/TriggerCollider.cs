using UnityEngine;

public class ProximityManager : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var obj = other.GetComponent<SpawnableObject>();
        if (obj != null)
            obj.Activate();
    }

    void OnTriggerExit(Collider other)
    {
        var obj = other.GetComponent<SpawnableObject>();
        if (obj != null)
            obj.Deactivate();
    }
}