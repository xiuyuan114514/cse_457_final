using UnityEngine;

public class BounceWall : MonoBehaviour
{
    public float bounceMultiplier = 1f;
    public float minimumExitSpeed = 4f;

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null || rb.GetComponentInParent<RobotController>() == null)
            return;

        Vector3 reflectedVelocity = -rb.linearVelocity * bounceMultiplier;

        if (reflectedVelocity.sqrMagnitude < minimumExitSpeed * minimumExitSpeed)
        {
            Vector3 fallbackDirection = collision.contactCount > 0
                ? collision.GetContact(0).normal
                : -transform.forward;

            reflectedVelocity = fallbackDirection.normalized * minimumExitSpeed;
        }

        rb.linearVelocity = reflectedVelocity;
    }
}
