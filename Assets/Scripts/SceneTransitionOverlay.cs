using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows a full-screen image for a specified duration before loading the Maze scene.
/// Creates itself at runtime — no prefab or scene setup required.
/// </summary>
public class SceneTransitionOverlay : MonoBehaviour
{
    public static void Show(float duration = 2f)
    {
        var go = new GameObject("SceneTransitionOverlay");
        DontDestroyOnLoad(go);
        var overlay = go.AddComponent<SceneTransitionOverlay>();
        overlay.StartCoroutine(overlay.TransitionSequence(duration));
    }

    IEnumerator TransitionSequence(float duration)
    {
        // Create full-screen canvas
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Black background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        bgImg.rectTransform.anchorMin = Vector2.zero;
        bgImg.rectTransform.anchorMax = Vector2.one;
        bgImg.rectTransform.offsetMin = Vector2.zero;
        bgImg.rectTransform.offsetMax = Vector2.zero;

        // Load and display the image
        var tex = Resources.Load<Texture2D>("robot_with_key");
        if (tex != null)
        {
            var imgGO = new GameObject("TransitionImage");
            imgGO.transform.SetParent(transform, false);

            var rawImg = imgGO.AddComponent<RawImage>();
            rawImg.texture = tex;

            // Center the image and fit it on screen
            var rt = rawImg.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Scale image to fit screen while preserving aspect ratio
            float aspect = (float)tex.width / tex.height;
            float screenAspect = (float)Screen.width / Screen.height;

            if (aspect > screenAspect)
            {
                // Image is wider — fit by width
                rt.sizeDelta = new Vector2(Screen.width, Screen.width / aspect);
            }
            else
            {
                // Image is taller — fit by height
                rt.sizeDelta = new Vector2(Screen.height * aspect, Screen.height);
            }
        }

        // Wait for the specified duration
        yield return new WaitForSecondsRealtime(duration);

        // Load Maze scene and clean up
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("Maze");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Destroy(gameObject);
    }
}
