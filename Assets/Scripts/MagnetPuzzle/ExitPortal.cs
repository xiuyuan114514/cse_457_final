using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Placed on the disabled ExitZone trigger at the door opening.
/// GoalTrigger enables this object when the puzzle is solved.
/// When the player walks through, it fades to black and loads the next scene.
/// </summary>
public class ExitPortal : MonoBehaviour
{
    [Tooltip("Scene name to load. Leave empty to reload the active scene.")]
    public string nextScene = "";

    [Tooltip("Fade-to-black duration in seconds")]
    public float fadeTime = 1.5f;

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        StartCoroutine(ExitSequence());
    }

    IEnumerator ExitSequence()
    {
        // Build a full-screen black overlay at runtime so no canvas is needed in the scene
        var canvasGO = new GameObject("ExitFadeCanvas");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var imgGO = new GameObject("BlackFill");
        imgGO.transform.SetParent(canvasGO.transform, false);

        var img = imgGO.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = Vector2.zero;
        img.rectTransform.offsetMax = Vector2.zero;

        // Fade in
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            img.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }

        // Load scene
        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
