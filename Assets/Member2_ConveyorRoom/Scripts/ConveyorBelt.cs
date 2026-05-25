using UnityEngine;

namespace TinyRobotEscape.Member2
{
    [RequireComponent(typeof(Collider))]
    public class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] private Vector3 localDirection = Vector3.forward;
        [SerializeField] private float pushSpeed = 4f;
        [SerializeField] private float maxConveyorVelocity = 8f;

        private Vector3 WorldDirection => transform.TransformDirection(localDirection.normalized);

        public void Configure(Vector3 direction, float speed, float maxVelocity)
        {
            localDirection = direction;
            pushSpeed = speed;
            maxConveyorVelocity = maxVelocity;
        }

        private void Reset()
        {
            Collider beltCollider = GetComponent<Collider>();
            beltCollider.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (other.attachedRigidbody == null)
            {
                return;
            }

            Rigidbody playerRigidbody = other.attachedRigidbody;
            Vector3 direction = WorldDirection;
            float currentSpeed = Vector3.Dot(playerRigidbody.linearVelocity, direction);

            if (currentSpeed < maxConveyorVelocity)
            {
                playerRigidbody.AddForce(direction * pushSpeed, ForceMode.Acceleration);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = transform.position + Vector3.up * 0.25f;
            Gizmos.DrawLine(origin, origin + WorldDirection * 2f);
            Gizmos.DrawSphere(origin + WorldDirection * 2f, 0.12f);
        }
    }
}
