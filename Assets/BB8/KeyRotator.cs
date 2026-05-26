using UnityEngine;

public class KeyRotator : MonoBehaviour
{
    public float rotateSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        Vector3 pos = startPos;
        pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = pos;
    }
}
