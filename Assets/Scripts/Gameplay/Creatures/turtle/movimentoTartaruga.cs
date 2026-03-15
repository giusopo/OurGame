using UnityEngine;

public class TurtleMove : MonoBehaviour
{
    public float minSpeed = 0.3f;
    public float maxSpeed = 1f;

    private float speed;

    void Start()
    {
        InvokeRepeating(nameof(ChangeSpeed), 0f, 2f);
    }

    void Update()
    {
        transform.position -= transform.right * speed * Time.deltaTime;
    }

    void ChangeSpeed()
    {
        speed = Random.Range(minSpeed, maxSpeed);
    }
}