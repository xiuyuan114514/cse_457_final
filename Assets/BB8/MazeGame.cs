using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeGame : MonoBehaviour
{
    const string SkipIntroOnNextMazeStartKey = "MazeSkipIntroOnNextStart";

    [Header("Game Settings")]
    public int totalKeys = 3;
    public float fallThreshold = -5f;

    [Header("Key → Scene mapping (order matches Key1/Key2/Key3)")]
    public string[] keyScenes = new string[]
    {
        "MagnetPuzzleRoom",
        "LaserRoom",
        "ConveyorChallengeRoom"
    };

    int keysCollected = 0;
    bool gameWon = false;
    bool gameLost = false;
    bool sceneTransitionPending = false;
    bool showIntroScreen = false;
    bool winTransitionPending = false;
    float winTransitionStartRealtime = 0f;
    float introStartRealtime = 0f;
    float endScreenStartRealtime = 0f;
    MazeCinematicSet activeCinematicSet;
    string loseReason = "";
    string message = "";
    float messageTimer = 0f;

    // Key names in the scene (Key1, Key2, Key3)
    string[] keyNames = new string[] { "Key1", "Key2", "Key3" };

    // References to key GameObjects so we can hide collected ones
    GameObject[] keyObjects = new GameObject[3];

    const string ImportedKeyResourcePath = "OpenGameArt/KeyLowPoly/key";
    const string ImportedKeyVisualName = "ImportedLowPolyKeyVisual";
    const float ImportedKeyTargetHeight = 1.5f;
    const float ImportedKeyGroundClearance = 0.34f;
    static Material importedKeyMaterial;

    // True while waiting for delayed teleport — suppresses fall detection
    bool teleportPending = false;

    // UI styles
    GUIStyle scoreStyle;
    GUIStyle winStyle;
    GUIStyle loseStyle;
    GUIStyle messageStyle;
    GUIStyle buttonStyle;
    GUIStyle introTitleStyle;
    GUIStyle introHeaderStyle;
    GUIStyle introBodyStyle;
    GUIStyle introSmallStyle;
    Texture2D introPanelTexture;
    Texture2D introLineTexture;
    Texture2D introButtonTexture;
    Texture2D introGlowTexture;
    Texture2D introDimLineTexture;

    const float IntroCinematicDuration = 7.4f;

    void Start()
    {
        // Set up Star Wars–style space background
        if (FindFirstObjectByType<MazeSpaceBackground>() == null)
        {
            var bgGO = new GameObject("SpaceBackground");
            bgGO.AddComponent<MazeSpaceBackground>();
        }

        // Find all keys by name Key1, Key2, Key3
        for (int i = 0; i < 3; i++)
        {
            var key = GameObject.Find(keyNames[i]);
            if (key != null)
            {
                keyObjects[i] = key;
                InstallImportedKeyVisual(key);
            }
        }

        var session = GameSessionData.GetOrCreate();
        bool returningFromSubScene = session.ReturningFromSubScene;

        // Restore state from session
        keysCollected = session.KeysCollected;

        // Hide already-collected keys
        for (int i = 0; i < 3; i++)
        {
            if (session.KeyCollected[i] && keyObjects[i] != null)
                keyObjects[i].SetActive(false);
        }

        // If returning from a sub-scene, teleport player back to saved position
        if (returningFromSubScene)
        {
            session.ReturningFromSubScene = false;
            teleportPending = true;
            StartCoroutine(DelayedTeleport(session.ReturnPosition, session.ReturnRotation));
        }
        else if (keysCollected == 0)
        {
            bool skipIntro = PlayerPrefs.GetInt(SkipIntroOnNextMazeStartKey, 0) == 1;
            if (skipIntro)
            {
                PlayerPrefs.DeleteKey(SkipIntroOnNextMazeStartKey);
            }
            else
            {
                showIntroScreen = true;
                introStartRealtime = Time.realtimeSinceStartup;
                activeCinematicSet = new MazeCinematicSet("Runtime_MazeIntroCinematic3D", new Color(0f, 0.95f, 1f), null, true);
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    void InstallImportedKeyVisual(GameObject keyObject)
    {
        if (keyObject.transform.Find(ImportedKeyVisualName) != null)
            return;

        GameObject importedKeyPrefab = Resources.Load<GameObject>(ImportedKeyResourcePath);
        if (importedKeyPrefab == null)
        {
            Debug.LogWarning($"[MazeGame] Could not load imported key visual at Resources/{ImportedKeyResourcePath}.");
            return;
        }

        HideOriginalKeyVisuals(keyObject);

        KeyRotator keyRotator = keyObject.GetComponent<KeyRotator>();
        if (keyRotator != null)
            keyRotator.bobHeight = 0f;

        GameObject importedVisual = Instantiate(importedKeyPrefab, keyObject.transform);
        importedVisual.name = ImportedKeyVisualName;
        importedVisual.transform.localPosition = Vector3.zero;
        importedVisual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        importedVisual.transform.localScale = Vector3.one;

        ApplyImportedKeyMaterial(importedVisual);
        FitImportedKeyVisual(keyObject, importedVisual);
    }

    void HideOriginalKeyVisuals(GameObject keyObject)
    {
        Renderer[] renderers = keyObject.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer keyRenderer in renderers)
            keyRenderer.enabled = false;
    }

    void ApplyImportedKeyMaterial(GameObject importedVisual)
    {
        Renderer[] renderers = importedVisual.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer keyRenderer in renderers)
            keyRenderer.sharedMaterial = GetImportedKeyMaterial();
    }

    Material GetImportedKeyMaterial()
    {
        if (importedKeyMaterial != null)
            return importedKeyMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        importedKeyMaterial = new Material(shader)
        {
            name = "RuntimeImportedGoldKey"
        };
        importedKeyMaterial.SetColor("_BaseColor", new Color(1f, 0.78f, 0.08f, 1f));
        importedKeyMaterial.SetColor("_Color", new Color(1f, 0.78f, 0.08f, 1f));
        importedKeyMaterial.SetColor("_EmissionColor", new Color(0.85f, 0.45f, 0.02f, 1f));
        importedKeyMaterial.EnableKeyword("_EMISSION");

        if (importedKeyMaterial.HasProperty("_Metallic"))
            importedKeyMaterial.SetFloat("_Metallic", 0.85f);
        if (importedKeyMaterial.HasProperty("_Smoothness"))
            importedKeyMaterial.SetFloat("_Smoothness", 0.68f);

        return importedKeyMaterial;
    }

    void FitImportedKeyVisual(GameObject keyObject, GameObject importedVisual)
    {
        if (!TryGetRendererBounds(importedVisual, out Bounds bounds))
            return;

        if (bounds.size.y > 0.001f)
        {
            float scale = ImportedKeyTargetHeight / bounds.size.y;
            importedVisual.transform.localScale *= scale;
        }

        if (!TryGetRendererBounds(importedVisual, out bounds))
            return;

        Collider keyCollider = keyObject.GetComponent<Collider>();
        float groundY = keyCollider != null
            ? keyCollider.bounds.min.y
            : keyObject.transform.position.y - 0.5f;
        float lift = groundY + ImportedKeyGroundClearance - bounds.min.y;
        importedVisual.transform.position += Vector3.up * lift;
    }

    bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(root.transform.position, Vector3.zero);
        bool hasRenderer = false;

        foreach (Renderer keyRenderer in renderers)
        {
            if (!hasRenderer)
            {
                bounds = keyRenderer.bounds;
                hasRenderer = true;
            }
            else
            {
                bounds.Encapsulate(keyRenderer.bounds);
            }
        }

        return hasRenderer;
    }

    void Update()
    {
        if (showIntroScreen) return;
        if (gameWon || gameLost || teleportPending) return;

        if (messageTimer > 0)
            messageTimer -= Time.deltaTime;

        // Fall detection
        var body = GetComponentInChildren<Rigidbody>();
        if (body != null && body.transform.position.y < fallThreshold)
        {
            gameLost = true;
            loseReason = "You fell off the maze!";
            endScreenStartRealtime = Time.realtimeSinceStartup;
            activeCinematicSet = new MazeCinematicSet("Runtime_MazeLoseCinematic3D", new Color(1f, 0.18f, 0.1f), null, true);
            Time.timeScale = 0f;
            ShowEndScreenCursor();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameWon || gameLost || sceneTransitionPending) return;

        // Key touch → save position and teleport to sub-scene
        for (int i = 0; i < 3; i++)
        {
            if (other.gameObject.name == keyNames[i])
            {
                if (GameSessionData.Instance != null && GameSessionData.Instance.KeyCollected[i])
                    return; // already collected

                // Save the key's position + Y offset so robot spawns above the maze
                var session = GameSessionData.GetOrCreate();
                Vector3 keyWorldPos = other.transform.position;
                session.ReturnPosition = keyWorldPos + Vector3.up * 2f;
                session.ReturnRotation = Quaternion.identity;
                session.CurrentKeyIndex = i;
                session.HasReturnPose = true;
                Debug.Log($"[MazeGame] Key {keyNames[i]} touched at worldPos={keyWorldPos}, saving returnPos={session.ReturnPosition}");

                // Load the sub-scene
                if (i < keyScenes.Length && !string.IsNullOrEmpty(keyScenes[i]))
                {
                    Time.timeScale = 1f;
                    sceneTransitionPending = true;
                    SceneTransitionOverlay.ShowToScene(keyScenes[i], 1.4f, true);
                }
                return;
            }
        }

        // Exit detection
        if (other.gameObject.name == "Exit")
        {
            if (keysCollected >= totalKeys)
            {
                StartCoroutine(ShowWinAfterQuickTransition());
            }
            else
            {
                message = "Need all " + totalKeys + " keys! (" + keysCollected + "/" + totalKeys + ")";
                messageTimer = 2f;
            }
        }
    }

    void OnGUI()
    {
        InitStyles();
        ImguiScale.Begin();

        if (showIntroScreen)
        {
            DrawIntroScreen();
            return;
        }

        if (!SceneTransitionOverlay.IsTransitioning && !winTransitionPending)
        {
            GUI.Label(new Rect(20, 20, 300, 40),
                "Keys: " + keysCollected + " / " + totalKeys, scoreStyle);
        }

        if (winTransitionPending)
        {
            if (gameWon)
                DrawEndScreen("You Win!", new Color(0.2f, 1f, 0.2f), "All keys collected!");
            DrawWinQuickTransition();
            return;
        }

        // Temporary message
        if (messageTimer > 0 && !gameWon && !gameLost)
        {
            float w = 400, h = 40;
            GUI.Label(new Rect(ImguiScale.Width / 2 - w / 2, 80, w, h), message, messageStyle);
        }

        // Win screen
        if (gameWon)
        {
            DrawEndScreen("You Win!", new Color(0.2f, 1f, 0.2f), "All keys collected!");
        }

        // Lose screen
        if (gameLost)
        {
            DrawEndScreen("You Lose!", Color.red, loseReason);
        }
    }

    // Free the cursor so the player can click the end-screen buttons.
    // The maze leaves the cursor locked/hidden during gameplay (e.g. set by
    // SubSceneReturnHandler), so it must be released when the game ends.
    void ShowEndScreenCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator ShowWinAfterQuickTransition()
    {
        sceneTransitionPending = true;
        winTransitionPending = true;
        winTransitionStartRealtime = Time.realtimeSinceStartup;
        yield return new WaitForSecondsRealtime(0.34f);
        gameWon = true;
        endScreenStartRealtime = Time.realtimeSinceStartup;
        activeCinematicSet = new MazeCinematicSet("Runtime_MazeWinCinematic3D", new Color(0.15f, 1f, 0.55f), null, true);
        Time.timeScale = 0f;
        ShowEndScreenCursor();
        yield return new WaitForSecondsRealtime(0.34f);
        winTransitionPending = false;
    }

    void DrawWinQuickTransition()
    {
        float elapsed = Time.realtimeSinceStartup - winTransitionStartRealtime;
        float cover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.34f));
        float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 0.34f) / 0.34f));
        float alpha = gameWon ? 1f - reveal : cover;
        DrawTintedTexture(new Rect(0f, 0f, ImguiScale.Width, ImguiScale.Height), introPanelTexture, new Color(0f, 0f, 0f, alpha));
    }

    void DrawEndScreen(string title, Color titleColor, string subtitle)
    {
        float elapsed = Time.realtimeSinceStartup - endScreenStartRealtime;
        float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.1f));
        Color accent = gameWon ? new Color(0.15f, 1f, 0.55f, 1f) : new Color(1f, 0.18f, 0.1f, 1f);
        Color oldColor = GUI.color;

        DrawTintedTexture(new Rect(0f, 0f, ImguiScale.Width, ImguiScale.Height), introPanelTexture, new Color(0f, 0f, 0f, 0.36f * appear));

        float centerX = ImguiScale.Width * 0.5f;
        float centerY = ImguiScale.Height * 0.46f;
        float boxW = Mathf.Min(520f, ImguiScale.Width - 48f);
        float boxH = 260f;
        float boxX = centerX - boxW * 0.5f;
        float boxY = centerY - boxH * 0.5f + Mathf.Lerp(42f, 0f, appear);
        Rect panel = new Rect(boxX, boxY, boxW, boxH);

        GUI.color = new Color(1f, 1f, 1f, appear);
        GUI.DrawTexture(panel, introPanelTexture);
        DrawFrame(panel, new Color(accent.r, accent.g, accent.b, 0.88f * appear), 2f);

        var titleStyle = gameWon ? winStyle : loseStyle;
        titleStyle.normal.textColor = titleColor;
        GUI.Label(new Rect(boxX, boxY + 38f, boxW, 66f), title, titleStyle);
        GUI.Label(new Rect(boxX + 36f, boxY + 106f, boxW - 72f, 34f), subtitle, messageStyle);

        string systemLine = gameWon ? "MAZE ACCESS COMPLETE // ALL KEYS VERIFIED" : "SIGNAL LOST // ROUTE RESET REQUIRED";
        GUI.Label(new Rect(boxX + 34f, boxY + 146f, boxW - 68f, 26f), systemLine, introSmallStyle);

        GUI.color = oldColor;
        if (GUI.Button(new Rect(boxX + boxW / 2 - 86f, boxY + 188f, 172f, 46f),
            "Play Again", buttonStyle))
        {
            Time.timeScale = 1f;
            // Reset session data on restart
            if (GameSessionData.Instance != null)
                Destroy(GameSessionData.Instance.gameObject);
            if (gameLost)
            {
                PlayerPrefs.SetInt(SkipIntroOnNextMazeStartKey, 1);
                PlayerPrefs.Save();
            }
            if (activeCinematicSet != null)
            {
                activeCinematicSet.Dispose();
                activeCinematicSet = null;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void DrawIntroScreen()
    {
        float elapsed = Time.realtimeSinceStartup - introStartRealtime;
        bool showBriefing = elapsed >= IntroCinematicDuration;
        if (showBriefing)
        {
            DrawIntroBriefingBackdrop();
        }
        else
        {
            DrawIntroCinematicHud(elapsed);
        }

        if (!showBriefing)
            return;

        const float pad = 38f;
        const string missionText = "Recover 3 station keys from linked challenge rooms. Each key opens a specialized room; complete it, return to the maze, then find the next key. Bring all keys to the exit bay to finish the protocol.";
        const string controlsText = "WASD / Arrow Keys: move     Mouse: look\nTouch a key to enter a challenge room. Do not fall from the maze platform.";
        const string statusText = "Station link ready. Start when you are oriented.";

        float panelW = Mathf.Min(760f, ImguiScale.Width - 48f);
        float textW = panelW - pad * 2f;

        // Measure wrapped body blocks so the panel can be sized to its content.
        // This keeps the layout flowing top-down: nothing is anchored to the panel
        // bottom, so the controls and footer can never collide on a short window.
        float missionH = introBodyStyle.CalcHeight(new GUIContent(missionText), textW);
        float controlsH = introBodyStyle.CalcHeight(new GUIContent(controlsText), textW);

        float titleTop = 28f;
        float titleH = 50f;
        float divider1Y = titleTop + titleH + 14f;
        float missionHeaderY = divider1Y + 18f;
        float missionBodyY = missionHeaderY + 30f;
        float controlsHeaderY = missionBodyY + missionH + 22f;
        float controlsBodyY = controlsHeaderY + 30f;
        float divider2Y = controlsBodyY + controlsH + 18f;
        float footerY = divider2Y + 18f;
        float buttonH = 48f;
        float panelH = footerY + buttonH + 24f;

        float panelX = ImguiScale.Width * 0.5f - panelW * 0.5f;
        float panelY = ImguiScale.Height * 0.5f - panelH * 0.5f;
        float appear = Mathf.Clamp01((elapsed - IntroCinematicDuration) / 0.8f);
        float easedAppear = Mathf.SmoothStep(0f, 1f, appear);
        Rect panel = new Rect(panelX, panelY + Mathf.Lerp(28f, 0f, easedAppear), panelW, panelH);

        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, easedAppear);
        GUI.DrawTexture(panel, introPanelTexture);
        DrawFrame(panel, new Color(0f, 0.85f, 1f, 0.72f * easedAppear), 2f);
        GUI.DrawTexture(new Rect(panel.x + 28f, panel.y + divider1Y, panel.width - 56f, 2f), introLineTexture);
        GUI.DrawTexture(new Rect(panel.x + 28f, panel.y + divider2Y, panel.width - 56f, 2f), introLineTexture);

        GUI.Label(new Rect(panel.x + 34f, panel.y + titleTop, panel.width - 68f, titleH), "NEBULA KEY PROTOCOL", introTitleStyle);
        GUI.Label(new Rect(panel.x + pad, panel.y + missionHeaderY, textW, 24f), "MISSION", introHeaderStyle);
        GUI.Label(new Rect(panel.x + pad, panel.y + missionBodyY, textW, missionH), missionText, introBodyStyle);

        GUI.Label(new Rect(panel.x + pad, panel.y + controlsHeaderY, textW, 24f), "CONTROLS", introHeaderStyle);
        GUI.Label(new Rect(panel.x + pad, panel.y + controlsBodyY, textW, controlsH), controlsText, introBodyStyle);

        GUI.Label(new Rect(panel.x + pad, panel.y + footerY + 12f, textW - 210f, 28f),
            statusText,
            introSmallStyle);

        Rect buttonRect = new Rect(panel.x + panel.width - 238f, panel.y + footerY, 190f, buttonH);
        GUI.DrawTexture(buttonRect, introButtonTexture);
        if (GUI.Button(buttonRect, "START GAME", buttonStyle))
        {
            showIntroScreen = false;
            if (activeCinematicSet != null)
            {
                activeCinematicSet.Dispose();
                activeCinematicSet = null;
            }
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        GUI.color = oldColor;
    }

    void DrawIntroCinematicHud(float elapsed)
    {
        float width = ImguiScale.Width;
        float height = ImguiScale.Height;
        float phaseTitle = Mathf.Clamp01(elapsed / 1.2f);
        float phaseProgress = Mathf.Clamp01((elapsed - 5.0f) / 2.2f);

        DrawTintedTexture(new Rect(0f, 0f, width, height), introPanelTexture, new Color(0f, 0f, 0f, 0.22f));
        Rect titleBack = new Rect(width * 0.5f - 430f, height * 0.12f - 14f, 860f, 122f);
        DrawTintedTexture(titleBack, introPanelTexture, new Color(0f, 0f, 0f, 0.9f * phaseTitle));
        DrawFrame(titleBack, new Color(0f, 0.85f, 1f, 0.36f * phaseTitle), 2f);

        TextAnchor oldTitleAlignment = introTitleStyle.alignment;
        TextAnchor oldHeaderAlignment = introHeaderStyle.alignment;
        TextAnchor oldSmallAlignment = introSmallStyle.alignment;
        introTitleStyle.alignment = TextAnchor.MiddleCenter;
        introHeaderStyle.alignment = TextAnchor.MiddleCenter;
        introSmallStyle.alignment = TextAnchor.MiddleCenter;

        GUI.color = new Color(0.78f, 1f, 1f, phaseTitle);
        GUI.Label(new Rect(0f, height * 0.12f, width, 58f), "NEBULA KEY PROTOCOL", introTitleStyle);

        string status = elapsed < 1.6f
            ? "ORBITAL APPROACH"
            : elapsed < 3.2f
                ? "STATION LINK ESTABLISHED"
                : "MAZE BRIEFING READY";
        GUI.color = new Color(1f, 0.72f, 0.18f, Mathf.Clamp01((elapsed - 0.8f) / 0.8f));
        GUI.Label(new Rect(0f, height * 0.12f + 62f, width, 28f), status, introHeaderStyle);

        Rect progressBack = new Rect(width * 0.5f - 180f, height * 0.82f, 360f, 8f);
        Rect progressPanel = new Rect(progressBack.x - 44f, progressBack.y - 20f, progressBack.width + 88f, 66f);
        DrawTintedTexture(progressPanel, introPanelTexture, new Color(0f, 0f, 0f, 0.62f * phaseProgress));
        DrawFrame(progressPanel, new Color(1f, 0.72f, 0.18f, 0.24f * phaseProgress), 1.5f);
        DrawTintedTexture(progressBack, introDimLineTexture, new Color(0f, 0.2f, 0.28f, 0.78f * phaseProgress));
        DrawTintedTexture(new Rect(progressBack.x, progressBack.y, progressBack.width * phaseProgress, progressBack.height), introLineTexture, new Color(0f, 0.95f, 1f, 0.95f * phaseProgress));
        GUI.color = new Color(0.55f, 0.95f, 1f, phaseProgress);
        GUI.Label(new Rect(0f, progressBack.y + 18f, width, 28f), "PRESSURIZING ACCESS ROUTE", introSmallStyle);

        introTitleStyle.alignment = oldTitleAlignment;
        introHeaderStyle.alignment = oldHeaderAlignment;
        introSmallStyle.alignment = oldSmallAlignment;
        GUI.color = Color.white;
    }

    void DrawIntroBriefingBackdrop()
    {
        float width = ImguiScale.Width;
        float height = ImguiScale.Height;
        DrawTintedTexture(new Rect(0f, 0f, width, height), introGlowTexture, new Color(0f, 0.12f, 0.16f, 0.14f));
        DrawFrame(new Rect(width * 0.12f, height * 0.14f, width * 0.76f, height * 0.72f), new Color(0f, 0.65f, 0.8f, 0.16f), 2f);
        DrawFrame(new Rect(width * 0.16f, height * 0.19f, width * 0.68f, height * 0.62f), new Color(1f, 0.72f, 0.18f, 0.10f), 1f);
    }

    void DrawFrame(Rect rect, Color color, float thickness)
    {
        DrawTintedTexture(new Rect(rect.x, rect.y, rect.width, thickness), introLineTexture, color);
        DrawTintedTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), introLineTexture, color);
        DrawTintedTexture(new Rect(rect.x, rect.y, thickness, rect.height), introLineTexture, color);
        DrawTintedTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), introLineTexture, color);
    }

    void DrawTintedTexture(Rect rect, Texture2D texture, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture);
        GUI.color = oldColor;
    }

    IEnumerator DelayedTeleport(Vector3 targetPos, Quaternion targetRot)
    {
        // Make all rigidbodies kinematic immediately to freeze physics
        var allRbs = GetComponentsInChildren<Rigidbody>();
        bool[] wasKinematic = new bool[allRbs.Length];
        for (int i = 0; i < allRbs.Length; i++)
        {
            wasKinematic[i] = allRbs[i].isKinematic;
            allRbs[i].isKinematic = true;
        }

        // Wait one frame for physics to settle
        yield return new WaitForFixedUpdate();

        // Find the Body (first rigidbody child) and compute offset
        var bodyRb = allRbs.Length > 0 ? allRbs[0] : null;
        if (bodyRb != null)
        {
            Vector3 offset = targetPos - bodyRb.position;
            Debug.Log($"[MazeGame] Teleport: target={targetPos}, bodyBefore={bodyRb.position}, offset={offset}");

            // Only move the parent transform — children move with it automatically
            transform.position += offset;

            // Sync each rigidbody's physics position to match its new transform position
            foreach (var rb in allRbs)
            {
                rb.position = rb.transform.position;
            }

            Debug.Log($"[MazeGame] Teleport done: bodyAfter={bodyRb.position}, bodyTransform={bodyRb.transform.position}");
        }

        // Restore kinematic state and zero velocities
        for (int i = 0; i < allRbs.Length; i++)
        {
            allRbs[i].isKinematic = wasKinematic[i];
            allRbs[i].linearVelocity = Vector3.zero;
            allRbs[i].angularVelocity = Vector3.zero;
        }

        teleportPending = false;
    }

    void InitStyles()
    {
        if (scoreStyle != null) return;

        scoreStyle = new GUIStyle(GUI.skin.label);
        scoreStyle.fontSize = 24;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.normal.textColor = Color.white;

        winStyle = new GUIStyle(GUI.skin.label);
        winStyle.fontSize = 48;
        winStyle.fontStyle = FontStyle.Bold;
        winStyle.alignment = TextAnchor.MiddleCenter;
        winStyle.normal.textColor = new Color(0.2f, 1f, 0.2f);

        loseStyle = new GUIStyle(GUI.skin.label);
        loseStyle.fontSize = 48;
        loseStyle.fontStyle = FontStyle.Bold;
        loseStyle.alignment = TextAnchor.MiddleCenter;
        loseStyle.normal.textColor = Color.red;

        messageStyle = new GUIStyle(GUI.skin.label);
        messageStyle.fontSize = 20;
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.normal.textColor = Color.yellow;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;

        introTitleStyle = new GUIStyle(GUI.skin.label);
        introTitleStyle.fontSize = 38;
        introTitleStyle.fontStyle = FontStyle.Bold;
        introTitleStyle.alignment = TextAnchor.MiddleLeft;
        introTitleStyle.normal.textColor = new Color(0.78f, 1f, 1f);

        introHeaderStyle = new GUIStyle(GUI.skin.label);
        introHeaderStyle.fontSize = 18;
        introHeaderStyle.fontStyle = FontStyle.Bold;
        introHeaderStyle.normal.textColor = new Color(1f, 0.72f, 0.18f);

        introBodyStyle = new GUIStyle(GUI.skin.label);
        introBodyStyle.fontSize = 18;
        introBodyStyle.wordWrap = true;
        introBodyStyle.normal.textColor = new Color(0.88f, 0.96f, 1f);

        introSmallStyle = new GUIStyle(GUI.skin.label);
        introSmallStyle.fontSize = 15;
        introSmallStyle.normal.textColor = new Color(0.5f, 0.95f, 1f);

        introPanelTexture = MakeTexture(new Color(0.005f, 0.012f, 0.02f, 0.98f));
        introLineTexture = MakeTexture(new Color(0f, 0.9f, 1f, 0.65f));
        introButtonTexture = MakeTexture(new Color(0f, 0.38f, 0.5f, 0.85f));
        introGlowTexture = MakeTexture(new Color(0f, 0.3f, 0.42f, 1f));
        introDimLineTexture = MakeTexture(new Color(0f, 0.25f, 0.32f, 1f));
    }

    Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

}
