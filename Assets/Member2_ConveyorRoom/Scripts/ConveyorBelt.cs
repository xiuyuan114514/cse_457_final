using UnityEngine;

namespace TinyRobotEscape.Member2
{
    [RequireComponent(typeof(Collider))]
    public class ConveyorBelt : MonoBehaviour
    {
        [SerializeField] private Vector3 localDirection = Vector3.forward;
        [SerializeField] private float pushSpeed = 4f;

        private Vector3 WorldDirection => transform.TransformDirection(localDirection.normalized);

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

            Vector3 velocity = WorldDirection * pushSpeed;
            other.attachedRigidbody.AddForce(velocity, ForceMode.Acceleration);
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
