using UnityEngine;
using TMPro;

/// <summary>
/// Handles visual atmosphere for the Magnet Puzzle Room:
/// - Adds colored point lights near key objects
/// - Pulses the magnetic core glow
/// - Shows button activation feedback
/// - Updates UI text on puzzle completion
/// </summary>
public class MagnetRoomVisuals : MonoBehaviour
{
    [Header("Scene References (auto-found if empty)")]
    public GameObject magneticCore;
    public GameObject powerDock;
    public GameObject[] magnetButtons;
    public TextMeshProUGUI instructionText;

    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float pulseMin = 0.6f;
    public float pulseMax = 1.4f;

    // Runtime state
    Light coreLight;
    Light dockLight;
    Light[] buttonLights;
    Renderer coreRenderer;
    Material coreMaterialInstance;
    bool puzzleComplete = false;

    // Colors
    static readonly Color CoreColor    = new Color(0.15f, 0.75f, 1f);
    static readonly Color DockColor    = new Color(0f, 1f, 0.4f);
    static readonly Color ButtonColor  = new Color(0.1f, 0.3f, 1f);
    static readonly Color ButtonActive = new Color(0f, 1f, 0.5f);

    void Start()
    {
        AutoFindReferences();
        SetupCoreMaterial();
        CreateAtmosphericLights();
        if (instructionText != null)
            UpdateInstructionText(false);
    }

    void AutoFindReferences()
    {
        if (magneticCore == null)
            magneticCore = GameObject.Find("MagneticCore");
        if (powerDock == null)
            powerDock = GameObject.Find("PowerDock");

        if (magnetButtons == null || magnetButtons.Length == 0)
        {
            magnetButtons = new GameObject[]
            {
                GameObject.Find("MagnetButton_A"),
                GameObject.Find("MagnetButton_B"),
                GameObject.Find("MagnetButton_C"),
                GameObject.Find("MagnetButton_D")
            };
        }

        if (instructionText == null)
        {
            var tmp = FindFirstObjectByType<TextMeshProUGUI>();
            if (tmp != null) instructionText = tmp;
        }
    }

    void SetupCoreMaterial()
    {
        if (magneticCore == null) return;
        coreRenderer = magneticCore.GetComponent<Renderer>();
        if (coreRenderer != null)
        {
            // Instance the material so we can animate emission without affecting the asset
            coreMaterialInstance = coreRenderer.material;
        }
    }

    void CreateAtmosphericLights()
    {
        // Point light following the magnetic core
        coreLight = CreatePointLight("CoreLight", magneticCore != null ? magneticCore.transform.position : Vector3.zero,
                                     CoreColor, intensity: 2.5f, range: 5f);

        // Steady green glow at the power dock
        if (powerDock != null)
            dockLight = CreatePointLight("DockLight", powerDock.transform.position + Vector3.up * 0.5f,
                                         DockColor, intensity: 1.8f, range: 4f);

        // Small blue lights above each button
        buttonLights = new Light[magnetButtons.Length];
        for (int i = 0; i < magnetButtons.Length; i++)
        {
            if (magnetButtons[i] == null) continue;
            buttonLights[i] = CreatePointLight(
                $"ButtonLight_{i}",
                magnetButtons[i].transform.position + Vector3.up * 0.8f,
                ButtonColor, intensity: 1f, range: 2.5f);
        }
    }

    Light CreatePointLight(string lightName, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(lightName);
        go.transform.position = position;
        var lt = go.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = color;
        lt.intensity = intensity;
        lt.range = range;
        lt.shadows = LightShadows.None;
        return lt;
    }

    void Update()
    {
        if (puzzleComplete) return;

        AnimateCoreGlow();
        TrackCoreLightPosition();
        CheckPuzzleComplete();
    }

    void AnimateCoreGlow()
    {
        if (coreMaterialInstance == null || coreLight == null) return;

        float pulse = Mathf.Lerp(pulseMin, pulseMax,
                                 (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        coreLight.intensity = 2.5f * pulse;

        // Animate HDR emission intensity
        Color baseEmit = new Color(0.2f, 1.5f, 3.0f);
        coreMaterialInstance.SetColor("_EmissionColor", baseEmit * pulse);
    }

    void TrackCoreLightPosition()
    {
        if (coreLight != null && magneticCore != null)
            coreLight.transform.position = magneticCore.transform.position;
    }

    void CheckPuzzleComplete()
    {
        var goal = FindFirstObjectByType<GoalTrigger>();
        if (goal == null) return;
        if (goal.IsComplete)
            OnPuzzleComplete();
    }

    void OnPuzzleComplete()
    {
        puzzleComplete = true;

        if (dockLight != null)
        {
            dockLight.color = Color.white;
            dockLight.intensity = 5f;
            dockLight.range = 8f;
        }

        if (coreMaterialInstance != null)
            coreMaterialInstance.SetColor("_EmissionColor", new Color(0f, 4f, 1.5f));

        if (coreLight != null)
        {
            coreLight.color = DockColor;
            coreLight.intensity = 4f;
        }

        UpdateInstructionText(true);
        StartCoroutine(ShowSuccessOverlay());
    }

    System.Collections.IEnumerator ShowSuccessOverlay()
    {
        var canvasGO = new GameObject("SuccessOverlay");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        var cg = canvasGO.AddComponent<CanvasGroup>();

        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg = bgGO.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0.12f, 0.08f, 0.82f);
        bg.rectTransform.anchorMin = new Vector2(0.15f, 0.38f);
        bg.rectTransform.anchorMax = new Vector2(0.85f, 0.62f);
        bg.rectTransform.offsetMin = Vector2.zero;
        bg.rectTransform.offsetMax = Vector2.zero;

        var txtGO = new GameObject("SuccessText");
        txtGO.transform.SetParent(canvasGO.transform, false);
        var txt = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text      = "<b>CHALLENGE COMPLETE</b>\n<size=62%>Core docked — exit door is open</size>";
        txt.fontSize  = 42;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color     = new Color(0.15f, 1f, 0.55f);
        txt.rectTransform.anchorMin = new Vector2(0.1f, 0.35f);
        txt.rectTransform.anchorMax = new Vector2(0.9f, 0.65f);
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;

        // Fade in
        cg.alpha = 0f;
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 2f; cg.alpha = t; yield return null; }

        yield return new WaitForSeconds(3.5f);

        // Fade out
        t = 1f;
        while (t > 0f) { t -= Time.deltaTime * 1.5f; cg.alpha = t; yield return null; }

        Destroy(canvasGO);
    }

    /// <summary>Called by MagnetButton to flash the corresponding button light.</summary>
    public void FlashButton(int buttonIndex)
    {
        if (buttonLights == null || buttonIndex >= buttonLights.Length) return;
        var lt = buttonLights[buttonIndex];
        if (lt != null)
            StartCoroutine(FlashRoutine(lt));
    }

    System.Collections.IEnumerator FlashRoutine(Light lt)
    {
        Color original = lt.color;
        float originalIntensity = lt.intensity;

        lt.color = ButtonActive;
        lt.intensity = 3f;
        yield return new WaitForSeconds(0.15f);
        lt.color = original;
        lt.intensity = originalIntensity;
    }

    void UpdateInstructionText(bool complete)
    {
        if (instructionText == null) return;

        if (complete)
        {
            instructionText.text =
                "<color=#00FF88><b>CORE DOCKED — SYSTEM UNLOCKED</b></color>  " +
                "<size=85%>Exit door is open. Escape now!</size>";
        }
        else
        {
            instructionText.text =
                "<b>MAGNETIC CORE PROTOCOL</b>  " +
                "<size=85%><color=#44AAFF>Stand on a floor panel</color> to redirect the core.  " +
                "<color=#FF4444>WARNING:</color> Unstable field detected. " +
                "The core travels in straight lines — find a safe path to the <color=#00FF88>Power Dock</color>.</size>";
        }
    }
}
