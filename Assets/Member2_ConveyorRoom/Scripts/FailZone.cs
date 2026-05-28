using UnityEngine;

namespace TinyRobotEscape.Member2
{
    [RequireComponent(typeof(Collider))]
    public class FailZone : MonoBehaviour
    {
        [SerializeField] private ChallengeHud challengeHud;

        public void Configure(ChallengeHud hud)
        {
            challengeHud = hud;
        }

        private void Reset()
        {
            Collider failCollider = GetComponent<Collider>();
            failCollider.isTrigger = true;
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
                challengeHud.ShowFallFailure();
            }
        }
    }
}
