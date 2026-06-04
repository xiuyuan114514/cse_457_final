using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TinyRobotEscape.Member2
{
    [RequireComponent(typeof(Collider))]
    public class ChallengeGoal : MonoBehaviour
    {
        [SerializeField] private ChallengeHud challengeHud;
        [SerializeField] private UnityEvent onChallengeCompleted = new UnityEvent();

        private bool completed;
        private bool showReturnButton;
        private bool returningToMaze;

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
            showReturnButton = true;
            if (challengeHud != null)
            {
                challengeHud.ShowComplete();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("Member 2 conveyor challenge completed.");
            onChallengeCompleted.Invoke();
        }

        private GUIStyle btnStyle;

        private void OnGUI()
        {
            if (!showReturnButton) return;

            if (btnStyle == null)
            {
                btnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
            }

            float w = 250f, h = 50f;
            float x = Screen.width * 0.5f - w * 0.5f;
            float y = Screen.height * 0.5f + 60f;

            if (GUI.Button(new Rect(x, y, w, h), "RETURN TO MAZE", btnStyle) && !returningToMaze)
            {
                returningToMaze = true;
                showReturnButton = false;
                if (challengeHud != null)
                {
                    challengeHud.ShowCenterMessage(string.Empty);
                    challengeHud.ShowStatus(string.Empty);
                    challengeHud.ShowObjective(string.Empty);
                }
                SubSceneReturnHandler.ReturnToMaze(0.35f);
            }
        }
    }
}
