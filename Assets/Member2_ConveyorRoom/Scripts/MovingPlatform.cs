using UnityEngine;

namespace TinyRobotEscape.Member2
{
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 localEndOffset = new Vector3(0f, 0f, 4f);
        [SerializeField] private float moveDuration = 2f;
        [SerializeField] private bool startMoving = true;

        private Rigidbody platformRigidbody;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private float timer;

        public void Configure(Vector3 endOffset, float duration)
        {
            localEndOffset = endOffset;
            moveDuration = duration;
        }

        private void Awake()
        {
            platformRigidbody = GetComponent<Rigidbody>();
            startPosition = transform.position;
            endPosition = startPosition + transform.TransformDirection(localEndOffset);
        }

        private void FixedUpdate()
        {
            if (!startMoving || moveDuration <= 0f)
            {
                return;
            }

            timer += Time.fixedDeltaTime;
            float t = Mathf.PingPong(timer / moveDuration, 1f);
            Vector3 nextPosition = Vector3.Lerp(startPosition, endPosition, t);

            if (platformRigidbody != null)
            {
                platformRigidbody.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                collision.collider.transform.SetParent(transform);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.collider.CompareTag("Player"))
            {
                collision.collider.transform.SetParent(null);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 previewStart = Application.isPlaying ? startPosition : transform.position;
            Vector3 previewEnd = previewStart + transform.TransformDirection(localEndOffset);
            Gizmos.DrawLine(previewStart, previewEnd);
            Gizmos.DrawWireCube(previewEnd, transform.localScale);
        }
    }
}
