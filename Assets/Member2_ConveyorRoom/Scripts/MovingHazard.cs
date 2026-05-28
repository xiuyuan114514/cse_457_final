using UnityEngine;

namespace TinyRobotEscape.Member2
{
    [RequireComponent(typeof(Collider))]
    public class MovingHazard : MonoBehaviour
    {
        [SerializeField] private Vector3 localEndOffset = new Vector3(3f, 0f, 0f);
        [SerializeField] private float moveDuration = 1.5f;
        [SerializeField] private ChallengeHud challengeHud;

        private Vector3 startPosition;
        private Vector3 endPosition;
        private float timer;

        public void Configure(Vector3 endOffset, float duration, ChallengeHud hud = null)
        {
            localEndOffset = endOffset;
            moveDuration = duration;
            challengeHud = hud;
        }

        private void Reset()
        {
            Collider hazardCollider = GetComponent<Collider>();
            hazardCollider.isTrigger = true;
        }

        private void Awake()
        {
            startPosition = transform.position;
            endPosition = startPosition + transform.TransformDirection(localEndOffset);
        }

        private void FixedUpdate()
        {
            if (moveDuration <= 0f)
            {
                return;
            }

            timer += Time.fixedDeltaTime;
            float t = Mathf.PingPong(timer / moveDuration, 1f);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.Respawn();
            }

            if (challengeHud != null)
            {
                challengeHud.ShowHazardFailure();
            }
        }
    }
}
