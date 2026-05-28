using UnityEngine;
using UnityEngine.UI;

namespace TinyRobotEscape.Member2
{
    public class ChallengeHud : MonoBehaviour
    {
        private const string DefaultStatus = "Avoid red hazards and use moving platforms.";

        [SerializeField] private Text statusText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text centerMessageText;
        [SerializeField] private float temporaryMessageDuration = 1.6f;

        private float clearTemporaryAt;

        public void Configure(Text status, Text objective)
        {
            statusText = status;
            objectiveText = objective;
        }

        public void Configure(Text status, Text objective, Text centerMessage)
        {
            statusText = status;
            objectiveText = objective;
            centerMessageText = centerMessage;
        }

        private void Start()
        {
            ShowCenterMessage(string.Empty);
            ShowObjective("Reach the green exit. WASD / Arrow Keys move, mouse looks, Q/E turns.");
            ShowStatus("First-person conveyor challenge started.");
        }

        private void Update()
        {
            if (clearTemporaryAt > 0f && Time.time >= clearTemporaryAt)
            {
                ShowCenterMessage(string.Empty);
                ShowStatus(DefaultStatus);
                clearTemporaryAt = 0f;
            }
        }

        public void ShowObjective(string message)
        {
            if (objectiveText != null)
            {
                objectiveText.text = message;
            }
        }

        public void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void ShowCenterMessage(string message)
        {
            if (centerMessageText != null)
            {
                centerMessageText.text = message;
                centerMessageText.enabled = !string.IsNullOrEmpty(message);
            }
        }

        public void ShowFailure()
        {
            ShowFailure("Restarting from the start.");
        }

        public void ShowHazardFailure()
        {
            ShowFailure("Hit a red obstacle.\nRestarting from the start.");
        }

        public void ShowFallFailure()
        {
            ShowFailure("Fell off the course.\nRestarting from the start.");
        }

        private void ShowFailure(string message)
        {
            ShowStatus(string.Empty);
            ShowCenterMessage(message);
            clearTemporaryAt = Time.time + temporaryMessageDuration;
        }

        public void ShowComplete()
        {
            ShowStatus(string.Empty);
            ShowObjective(string.Empty);
            ShowCenterMessage("Goal reached.\nReady to connect this room to the main maze.");
            clearTemporaryAt = 0f;
        }
    }
}
