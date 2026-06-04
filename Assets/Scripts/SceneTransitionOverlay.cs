using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime-only sci-fi scene transition. Builds a holographic shutter overlay
/// from ordinary UI Images so it works without a prefab, shader, or video asset.
/// </summary>
public class SceneTransitionOverlay : MonoBehaviour
{
    const int SortingOrder = 300;
    const float ReturnCoverDuration = 1.75f;
    const float ReturnRevealDuration = 1.25f;
    const float EnterCoverDuration = 1.45f;
    const float EnterRevealDuration = 0.85f;

    readonly List<Image> sidePanels = new List<Image>();
    readonly List<Image> topBottomPanels = new List<Image>();
    readonly List<Image> scanLines = new List<Image>();
    readonly List<Image> glitchBars = new List<Image>();
    readonly List<Image> brackets = new List<Image>();

    Image darkWash;
    Image centerBeam;
    float blackOpacity;

    public static bool IsTransitioning { get; private set; }

    public static void Show(float duration = 2f)
    {
        ShowToScene("Maze", duration, false);
    }

    public static void ShowToScene(string sceneName, float duration = 1.2f, bool compact = true)
    {
        var go = new GameObject("SceneTransitionOverlay", typeof(RectTransform));
        DontDestroyOnLoad(go);
        var overlay = go.AddComponent<SceneTransitionOverlay>();
        overlay.StartCoroutine(overlay.TransitionSequence(sceneName, duration, compact));
    }

    IEnumerator TransitionSequence(string sceneName, float duration, bool compact)
    {
        IsTransitioning = true;
        BuildOverlay();
        blackOpacity = 0f;
        SetProgress(0f);

        // Let layout settle and render the first visible frame before any scene load.
        yield return null;

        float coverDuration = compact ? Mathf.Min(duration, EnterCoverDuration) : Mathf.Min(duration * 0.55f, ReturnCoverDuration);
        float revealDuration = compact ? EnterRevealDuration : Mathf.Min(duration * 0.45f, ReturnRevealDuration);

        yield return Animate(0f, 1f, Mathf.Max(0.2f, coverDuration), 0f, 1f);

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else if (sceneName == "Maze")
        {
            Debug.LogWarning("[SceneTransitionOverlay] Maze scene name was not found in build settings; loading scene index 0.");
            SceneManager.LoadScene(0);
        }
        else
        {
            Debug.LogWarning($"[SceneTransitionOverlay] Scene '{sceneName}' was not found in build settings.");
        }

        yield return null;
        yield return Animate(1f, 0f, Mathf.Max(0.15f, revealDuration), 1f, 0f);
        IsTransitioning = false;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        IsTransitioning = false;
    }

    void BuildOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        darkWash = CreateImage("DarkWash", new Color(0.005f, 0.01f, 0.02f, 0f), Vector2.zero, Vector2.one);
        darkWash.rectTransform.offsetMin = Vector2.zero;
        darkWash.rectTransform.offsetMax = Vector2.zero;

        sidePanels.Add(CreateImage("LeftShutter", new Color(0.005f, 0.01f, 0.018f, 0.94f), new Vector2(0f, 0f), new Vector2(0f, 1f)));
        sidePanels.Add(CreateImage("RightShutter", new Color(0.005f, 0.01f, 0.018f, 0.94f), new Vector2(1f, 0f), new Vector2(1f, 1f)));
        topBottomPanels.Add(CreateImage("TopShutter", new Color(0f, 0.025f, 0.04f, 0.82f), new Vector2(0f, 1f), new Vector2(1f, 1f)));
        topBottomPanels.Add(CreateImage("BottomShutter", new Color(0f, 0.025f, 0.04f, 0.82f), new Vector2(0f, 0f), new Vector2(1f, 0f)));

        centerBeam = CreateImage("ScannerBeam", new Color(0f, 0.95f, 1f, 0.95f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));

        for (int i = 0; i < 9; i++)
            scanLines.Add(CreateImage($"ScanLine_{i}", new Color(0f, 0.85f, 1f, 0.24f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f)));

        for (int i = 0; i < 12; i++)
        {
            Color color = i % 3 == 0
                ? new Color(1f, 0.55f, 0.08f, 0.42f)
                : new Color(0f, 0.95f, 1f, 0.32f);
            glitchBars.Add(CreateImage($"GlitchBar_{i}", color, new Vector2(0f, 0f), new Vector2(0f, 0f)));
        }

        BuildCornerBrackets();
    }

    Image CreateImage(string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = anchorMin;
        image.rectTransform.anchorMax = anchorMax;
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
        return image;
    }

    void BuildCornerBrackets()
    {
        Vector2[] anchors =
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f)
        };

        for (int i = 0; i < anchors.Length; i++)
        {
            Color c = i < 2 ? new Color(0f, 0.95f, 1f, 0.9f) : new Color(1f, 0.55f, 0.08f, 0.8f);
            brackets.Add(CreateImage($"BracketH_{i}", c, anchors[i], anchors[i]));
            brackets.Add(CreateImage($"BracketV_{i}", c, anchors[i], anchors[i]));
        }
    }

    IEnumerator Animate(float from, float to, float duration, float blackFrom, float blackTo)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            blackOpacity = Mathf.Lerp(blackFrom, blackTo, eased);
            SetProgress(Mathf.Lerp(from, to, eased));
            yield return null;
        }

        blackOpacity = blackTo;
        SetProgress(to);
    }

    void SetProgress(float progress)
    {
        float width = Mathf.Max(Screen.width, 1f);
        float height = Mathf.Max(Screen.height, 1f);
        float shutter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 18f) * 0.5f;

        SetAlpha(darkWash, Mathf.Max(blackOpacity, shutter));

        SetRect(sidePanels[0].rectTransform, new Vector2(0f, 0.5f), new Vector2(width * 0.56f * shutter, height), new Vector2(width * 0.28f * shutter, 0f));
        SetRect(sidePanels[1].rectTransform, new Vector2(1f, 0.5f), new Vector2(width * 0.56f * shutter, height), new Vector2(-width * 0.28f * shutter, 0f));
        SetRect(topBottomPanels[0].rectTransform, new Vector2(0.5f, 1f), new Vector2(width, height * 0.16f * shutter), new Vector2(0f, -height * 0.08f * shutter));
        SetRect(topBottomPanels[1].rectTransform, new Vector2(0.5f, 0f), new Vector2(width, height * 0.14f * shutter), new Vector2(0f, height * 0.07f * shutter));

        float beamY = Mathf.Lerp(-height * 0.2f, height * 1.2f, shutter);
        SetRect(centerBeam.rectTransform, new Vector2(0.5f, 0f), new Vector2(width, 10f + pulse * 6f), new Vector2(0f, beamY));
        SetAlpha(centerBeam, 0.95f * shutter);

        for (int i = 0; i < scanLines.Count; i++)
        {
            float y = Mathf.Repeat(beamY + i * 54f, height);
            SetRect(scanLines[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(width * (0.35f + 0.05f * i), 2.5f), new Vector2(0f, y));
            SetAlpha(scanLines[i], 0.28f * shutter);
        }

        for (int i = 0; i < glitchBars.Count; i++)
        {
            float hash = Hash01(i * 13.77f + Time.unscaledTime * 9f);
            float w = Mathf.Lerp(width * 0.08f, width * 0.32f, Hash01(i * 2.2f + 4.8f));
            float h = Mathf.Lerp(8f, 30f, hash);
            float x = Mathf.Lerp(-width * 0.38f, width * 0.38f, Hash01(i * 7.1f + 1.2f));
            float y = Mathf.Lerp(-height * 0.42f, height * 0.42f, Hash01(i * 5.4f + Time.unscaledTime));
            SetRect(glitchBars[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(w, h), new Vector2(x, y));
            SetAlpha(glitchBars[i], (0.18f + 0.28f * hash) * shutter);
        }

        UpdateBrackets(width, height, shutter);
    }

    void UpdateBrackets(float width, float height, float shutter)
    {
        float inset = 44f;
        float length = Mathf.Lerp(26f, 140f, shutter);
        float thick = 7f;
        for (int i = 0; i < 4; i++)
        {
            bool right = i == 1 || i == 3;
            bool top = i < 2;
            Vector2 anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
            Vector2 corner = new Vector2((right ? -inset : inset), (top ? -inset : inset));
            int baseIndex = i * 2;

            SetRect(brackets[baseIndex].rectTransform, anchor, new Vector2(length, thick), corner + new Vector2((right ? -length * 0.5f : length * 0.5f), 0f));
            SetRect(brackets[baseIndex + 1].rectTransform, anchor, new Vector2(thick, length), corner + new Vector2(0f, (top ? -length * 0.5f : length * 0.5f)));
            SetAlpha(brackets[baseIndex], shutter);
            SetAlpha(brackets[baseIndex + 1], shutter);
        }
    }

    void SetRect(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
    }

    void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    float Hash01(float value)
    {
        return Mathf.Repeat(Mathf.Sin(value * 12.9898f) * 43758.5453f, 1f);
    }
}
