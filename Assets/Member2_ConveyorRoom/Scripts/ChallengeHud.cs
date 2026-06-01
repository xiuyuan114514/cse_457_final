using UnityEngine;
using UnityEngine.UI;

namespace TinyRobotEscape.Member2
{
    public class ChallengeHud : MonoBehaviour
    {
        private const string DefaultStatus = "Avoid red security blocks. Stay on the route.";

        [SerializeField] private Text statusText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text centerMessageText;
        [SerializeField] private float temporaryMessageDuration = 1.6f;

        private float clearTemporaryAt;
        private Image centerMessagePanel;
        private Image objectivePanel;

        private void Awake()
        {
            StyleHudText(objectiveText, 20, FontStyle.Bold, new Color(0.74f, 0.95f, 1f));
            StyleHudText(statusText, 16, FontStyle.Normal, new Color(0.86f, 0.9f, 0.98f));
            StyleHudText(centerMessageText, 34, FontStyle.Bold, new Color(0.93f, 1f, 0.94f));

            objectivePanel = CreatePanel("ObjectivePanel", objectiveText, new Vector2(34f, 58f), new Color(0.01f, 0.025f, 0.055f, 0.64f));
            centerMessagePanel = CreatePanel("CenterMessagePanel", centerMessageText, new Vector2(56f, 36f), new Color(0.01f, 0.025f, 0.055f, 0.78f));
            if (centerMessagePanel != null)
            {
                centerMessagePanel.enabled = false;
            }

            KeepTextAbovePanels();
        }

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
            ShowObjective("CONVEYOR ROUTE // REACH THE GREEN EXIT");
            ShowStatus("WASD / Arrows move   Mouse looks   Q/E turns");
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

            RefreshObjectivePanel();
        }

        public void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            RefreshObjectivePanel();
        }

        public void ShowCenterMessage(string message)
        {
            if (centerMessageText != null)
            {
                centerMessageText.text = message;
                centerMessageText.enabled = !string.IsNullOrEmpty(message);
            }

            if (centerMessagePanel != null)
            {
                centerMessagePanel.enabled = !string.IsNullOrEmpty(message);
            }

            KeepTextAbovePanels();
        }

        public void ShowFailure()
        {
            ShowFailure("Restarting from the start.");
        }

        public void ShowHazardFailure()
        {
            ShowFailure("SYSTEM RESET\nRed obstacle contact detected.");
        }

        public void ShowFallFailure()
        {
            ShowFailure("SYSTEM RESET\nRoute boundary lost.");
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
            ShowCenterMessage("ROOM CLEARED\nReturn route unlocked.");
            clearTemporaryAt = 0f;
        }

        private static void StyleHudText(Text text, int size, FontStyle style, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
        }

        private static Image CreatePanel(string name, Text targetText, Vector2 padding, Color color)
        {
            if (targetText == null || targetText.transform.parent == null)
            {
                return null;
            }

            RectTransform targetRect = targetText.rectTransform;
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(targetText.transform.parent, false);
            panelObject.transform.SetSiblingIndex(targetText.transform.GetSiblingIndex());

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = targetRect.anchorMin;
            panelRect.anchorMax = targetRect.anchorMax;
            panelRect.pivot = targetRect.pivot;
            panelRect.anchoredPosition = targetRect.anchoredPosition;
            panelRect.sizeDelta = targetRect.sizeDelta + padding;

            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            targetText.transform.SetAsLastSibling();
            return image;
        }

        private void KeepTextAbovePanels()
        {
            if (objectivePanel != null)
            {
                objectivePanel.transform.SetAsFirstSibling();
            }

            if (centerMessagePanel != null)
            {
                centerMessagePanel.transform.SetAsFirstSibling();
            }

            if (objectiveText != null)
            {
                objectiveText.transform.SetAsLastSibling();
            }

            if (statusText != null)
            {
                statusText.transform.SetAsLastSibling();
            }

            if (centerMessageText != null)
            {
                centerMessageText.transform.SetAsLastSibling();
            }
        }

        private void RefreshObjectivePanel()
        {
            if (objectivePanel == null)
            {
                return;
            }

            bool hasObjective = objectiveText != null && !string.IsNullOrEmpty(objectiveText.text);
            bool hasStatus = statusText != null && !string.IsNullOrEmpty(statusText.text);
            objectivePanel.enabled = hasObjective || hasStatus;
        }
    }
}
