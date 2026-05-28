using UnityEngine;
using UnityEngine.Events;

namespace TinyRobotEscape.Member2
{
    [RequireComponent(typeof(Collider))]
    public class ChallengeGoal : MonoBehaviour
    {
        [SerializeField] private ChallengeHud challengeHud;
        [SerializeField] private UnityEvent onChallengeCompleted = new UnityEvent();

        private bool completed;

        public void Configure(ChallengeHud hud)
        {
            challengeHud = hud;
        }

        private void Reset()
        {
            Collider goalCollider = GetComponent<Collider>();
            goalCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (completed || !other.CompareTag("Player"))
            {
                return;
            }

            completed = true;
            if (challengeHud != null)
            {
                challengeHud.ShowComplete();
            }

            Debug.Log("Member 2 conveyor challenge completed.");
            onChallengeCompleted.Invoke();
        }
    }
}
