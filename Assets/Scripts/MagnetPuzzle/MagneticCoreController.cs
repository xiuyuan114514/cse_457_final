using UnityEngine;

public class MagneticCoreController : MonoBehaviour
{
    public Rigidbody coreRb;
    public Transform currentTarget;

    public float magnetForce = 25f;
    public float maxSpeed = 6f;
    public float stopDistance = 0.5f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        if (coreRb == null)
            coreRb = GetComponent<Rigidbody>();

        startPosition = transform.position;
        startRotation = transform.rotation;

        // Prevent player physics from pushing the core
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            Collider[] myCols    = GetComponents<Collider>();
            Collider[] playerCols = playerGO.GetComponents<Collider>();
            foreach (Collider pc in playerCols)
                foreach (Collider mc in myCols)
                    Physics.IgnoreCollision(pc, mc);
        }
    }

    void FixedUpdate()
    {
        if (currentTarget == null || coreRb == null)
        {
            return;
        }

        Vector3 toTarget = currentTarget.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance < stopDistance)
        {
            coreRb.linearVelocity = Vector3.zero;
            coreRb.angularVelocity = Vector3.zero;
            currentTarget = null;
            return;
        }

        Vector3 direction = toTarget.normalized;
        coreRb.AddForce(direction * magnetForce, ForceMode.Force);

        if (coreRb.linearVelocity.magnitude > maxSpeed)
        {
            coreRb.linearVelocity = coreRb.linearVelocity.normalized * maxSpeed;
        }
    }

    public void SetMagnetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void ResetCore()
    {
        currentTarget = null;
        coreRb.linearVelocity = Vector3.zero;
        coreRb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}