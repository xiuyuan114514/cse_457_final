using UnityEngine;
using UnityEngine.Events;

namespace TinyRobotEscape.Member2
{
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private UnityEvent onRespawn = new UnityEvent();

        private Rigidbody playerRigidbody;
        private Vector3 fallbackPosition;
        private Quaternion fallbackRotation;

        public void SetSpawnPoint(Transform newSpawnPoint)
        {
            spawnPoint = newSpawnPoint;
        }

        private void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            fallbackPosition = transform.position;
            fallbackRotation = transform.rotation;
        }

        public void Respawn()
        {
            Vector3 targetPosition = spawnPoint != null ? spawnPoint.position : fallbackPosition;
            Quaternion targetRotation = spawnPoint != null ? spawnPoint.rotation : fallbackRotation;

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);
            transform.SetParent(null);
            onRespawn.Invoke();
        }
    }
}
