using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays a full-screen transition video before loading the Maze scene.
/// Creates itself at runtime — no prefab or scene setup required.
/// </summary>
public class SceneTransitionOverlay : MonoBehaviour
{
    // Video clip in Assets/Resources (loaded by name, without extension).
    const string VideoResourceName = "transition_video";

    /// <param name="duration">
    /// Fallback duration (seconds) used only if the video clip can't be loaded.
    /// When the video plays, the transition lasts for the full length of the clip.
    /// </param>
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

        // Load the transition video
        var clip = Resources.Load<VideoClip>(VideoResourceName);
        if (clip != null)
        {
            // Render the video into a RenderTexture sized to the clip.
            int vw = (int)clip.width;
            int vh = (int)clip.height;
            var renderTex = new RenderTexture(vw, vh, 0);
            renderTex.Create();

            // VideoPlayer drives playback into the RenderTexture.
            var videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = clip;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTex;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;

            // Route audio through an AudioSource.
            var audioSource = gameObject.AddComponent<AudioSource>();
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);

            // Display the RenderTexture full-screen, preserving aspect ratio.
            var imgGO = new GameObject("TransitionVideo");
            imgGO.transform.SetParent(transform, false);
            var rawImg = imgGO.AddComponent<RawImage>();
            rawImg.texture = renderTex;

            var rt = rawImg.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float aspect = (float)vw / vh;
            float screenAspect = (float)Screen.width / Screen.height;
            if (aspect > screenAspect)
                rt.sizeDelta = new Vector2(Screen.width, Screen.width / aspect);
            else
                rt.sizeDelta = new Vector2(Screen.height * aspect, Screen.height);

            // Prepare, then play and wait for the clip to finish.
            videoPlayer.Prepare();
            float prepareTimeout = Time.realtimeSinceStartup + 5f;
            while (!videoPlayer.isPrepared && Time.realtimeSinceStartup < prepareTimeout)
                yield return null;

            videoPlayer.Play();

            // Wait until the video reaches its end (give one frame for isPlaying to latch).
            yield return null;
            while (videoPlayer.isPlaying)
                yield return null;

            renderTex.Release();
        }
        else
        {
            Debug.LogWarning($"[SceneTransitionOverlay] Video '{VideoResourceName}' not found in Resources; " +
                             $"waiting {duration}s before transition.");
            yield return new WaitForSecondsRealtime(duration);
        }

        // Load Maze scene and clean up
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (Application.CanStreamedLevelBeLoaded("Maze"))
        {
            SceneManager.LoadScene("Maze");
        }
        else
        {
            Debug.LogWarning("[SceneTransitionOverlay] Maze scene name was not found in build settings; loading scene index 0.");
            SceneManager.LoadScene(0);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Destroy(gameObject);
    }
}
