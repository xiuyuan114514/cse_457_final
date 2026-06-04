using UnityEngine;
using TMPro;

/// <summary>
/// Builds the magnet room as a previewable modular space-station lab.
/// The generated set is marked DontSave so it appears in edit/play mode
/// without permanently cluttering the scene file.
/// </summary>
[ExecuteAlways]
public class MagnetRoomVisuals : MonoBehaviour
{
    [Header("Lab Preview")]
    public bool buildExpandedLab = true;

    [Header("Scene References (auto-found if empty)")]
    public GameObject magneticCore;
    public GameObject powerDock;
    public GameObject[] magnetButtons;
    public TextMeshProUGUI instructionText;

    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float pulseMin = 0.6f;
    public float pulseMax = 1.4f;

    const string GeneratedRootName = "Runtime_MagnetLab_Set";

    // In edit mode we flag generated preview objects DontSave so they stay out of the
    // saved scene file. At runtime we must NOT use those flags: DontSave objects survive
    // scene loads, which would leave the magnet lab overlapping the Maze scene after exit.
    HideFlags GeneratedHideFlags =>
        Application.isPlaying
            ? HideFlags.None
            : (HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild);

    Light coreLight;
    Light dockLight;
    Light[] buttonLights;
    Renderer coreRenderer;
    Material coreMaterialInstance;
    bool puzzleComplete;
    bool showCompletionPanel;
    GUIStyle completionTitleStyle;
    GUIStyle completionMessageStyle;
    Texture2D completionPanelTexture;
    Texture2D completionLineTexture;

    Material floorMat;
    Material wallMat;
    Material trimMat;
    Material panelMat;
    Material creamFrameMat;
    Material cyanGlowMat;
    Material amberGlowMat;
    Material greenGlowMat;
    Material hazardMat;
    Material glassMat;
    Transform labRoot;

    static readonly Color CoreColor = new Color(0.22f, 0.88f, 1f);
    static readonly Color DockColor = new Color(0.1f, 1f, 0.5f);
    static readonly Color ButtonColor = new Color(0.18f, 0.65f, 1f);
    static readonly Color ButtonActive = new Color(0f, 1f, 0.55f);

    void OnEnable()
    {
        if (!Application.isPlaying)
            RebuildPreview();
    }

    void OnValidate()
    {
        // Avoid creating preview objects inside Unity's validation pass.
        // OnEnable/Start rebuild the preview when the scene or scripts reload.
    }

    void Start()
    {
        RebuildPreview();
    }

    void RebuildPreview()
    {
        AutoFindReferences();
        ClearGeneratedSet();

        if (buildExpandedLab)
            BuildExpandedMagnetLab();

        AutoFindReferences();
        SetupCoreMaterial();
        CreateAtmosphericLights();
        ApplySpaceStationLighting();
        ConfigureInstructionText();
        UpdateInstructionText(false);
    }

    void ClearGeneratedSet()
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var existing in allObjects)
        {
            if (existing == null || existing.name != GeneratedRootName) continue;

            DestroyImmediate(existing);
        }
    }

    void AutoFindReferences()
    {
        if (magneticCore == null)
            magneticCore = GameObject.Find("MagneticCore");
        if (powerDock == null)
            powerDock = GameObject.Find("PowerDock");

        var buttons = FindObjectsByType<MagnetButton>(FindObjectsSortMode.None);
        if (buttons.Length > 0)
        {
            magnetButtons = new GameObject[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
                magnetButtons[i] = buttons[i].gameObject;
        }

        if (instructionText == null)
        {
            var tmp = FindFirstObjectByType<TextMeshProUGUI>();
            if (tmp != null) instructionText = tmp;
        }
    }

    void BuildExpandedMagnetLab()
    {
        CreateRuntimeMaterials();
        var rootGO = new GameObject(GeneratedRootName);
        rootGO.hideFlags = GeneratedHideFlags;
        labRoot = rootGO.transform;

        var coreController = magneticCore != null ? magneticCore.GetComponent<MagneticCoreController>() : null;
        if (coreController != null)
        {
            coreController.magnetForce = 34f;
            coreController.maxSpeed = 7.5f;
            coreController.stopDistance = 0.42f;
        }

        ConfigureRoomShell();
        ConfigurePlayerStart();
        ConfigureCoreAndDock();
        ConfigureMagnetRoute(coreController);
        BuildStationArchitecture(coreController);
    }

    void CreateRuntimeMaterials()
    {
        floorMat = CreateMaterial("MagLab floor plating", new Color(0.18f, 0.17f, 0.145f), new Color(0f, 0f, 0f), 0.55f, 0.55f);
        wallMat = CreateMaterial("MagLab dark wall panels", new Color(0.105f, 0.105f, 0.095f), new Color(0f, 0f, 0f), 0.45f, 0.48f);
        trimMat = CreateMaterial("MagLab black trim", new Color(0.025f, 0.025f, 0.024f), new Color(0f, 0f, 0f), 0.75f, 0.45f);
        panelMat = CreateMaterial("MagLab recessed panels", new Color(0.075f, 0.075f, 0.066f), new Color(0f, 0f, 0f), 0.65f, 0.38f);
        creamFrameMat = CreateMaterial("MagLab armored cream frames", new Color(0.62f, 0.56f, 0.46f), new Color(0.015f, 0.012f, 0.006f), 0.28f, 0.5f);
        cyanGlowMat = CreateMaterial("MagLab cyan magnetic glass", new Color(0.02f, 0.2f, 0.24f), new Color(0f, 1.15f, 1.7f), 0.15f, 0.85f);
        amberGlowMat = CreateMaterial("MagLab warm warning lights", new Color(0.34f, 0.2f, 0.04f), new Color(2.2f, 1.05f, 0.12f), 0.15f, 0.8f);
        greenGlowMat = CreateMaterial("MagLab green status lights", new Color(0.03f, 0.24f, 0.08f), new Color(0.05f, 1.4f, 0.35f), 0.15f, 0.75f);
        hazardMat = CreateMaterial("MagLab red unstable field", new Color(0.32f, 0.035f, 0.02f), new Color(2.4f, 0.08f, 0.04f), 0.05f, 0.55f);
        glassMat = CreateMaterial("MagLab blue translucent tanks", new Color(0.03f, 0.24f, 0.28f, 0.55f), new Color(0f, 0.45f, 0.65f), 0f, 0.9f);
    }

    Material CreateMaterial(string name, Color baseColor, Color emission, float metallic, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color", baseColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetColor("_EmissionColor", emission);
        if (emission.maxColorComponent > 0f)
            mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    void ConfigureRoomShell()
    {
        ConfigureBox("Floor", new Vector3(0f, -0.06f, 0.6f), new Vector3(24f, 0.16f, 29f), floorMat);
        ConfigureBox("LeftWall", new Vector3(-12.1f, 2.05f, 0.6f), new Vector3(0.38f, 4.1f, 29f), wallMat);
        ConfigureBox("RightWall", new Vector3(12.1f, 2.05f, 0.6f), new Vector3(0.38f, 4.1f, 29f), wallMat);
        ConfigureBox("BackWall", new Vector3(0f, 2.05f, 14.9f), new Vector3(24.5f, 4.1f, 0.38f), wallMat);
        ConfigureBox("FrontWall", new Vector3(0f, 2.05f, -13.7f), new Vector3(24.5f, 4.1f, 0.38f), wallMat);
        HideSceneRenderer("Floor");
        HideSceneRenderer("LeftWall");
        HideSceneRenderer("RightWall");
        HideSceneRenderer("BackWall");
        HideSceneRenderer("FrontWall");

        BuildFloorPanels();
        BuildWallBays();
        BuildCeilingPanels();
    }

    void BuildFloorPanels()
    {
        CreateBox("StructuralDeck_FullRoomSlab", new Vector3(0f, -0.12f, 0.6f), new Vector3(24.1f, 0.22f, 29f), floorMat, false);
        CreateBox("StructuralDeck_LeftSkirt", new Vector3(-11.75f, 0.24f, 0.6f), new Vector3(0.48f, 0.72f, 29f), trimMat, false);
        CreateBox("StructuralDeck_RightSkirt", new Vector3(11.75f, 0.24f, 0.6f), new Vector3(0.48f, 0.72f, 29f), trimMat, false);
        CreateBox("StructuralDeck_BackSkirt", new Vector3(0f, 0.24f, 14.55f), new Vector3(24.1f, 0.72f, 0.48f), trimMat, false);
        CreateBox("StructuralDeck_FrontSkirt", new Vector3(0f, 0.24f, -13.35f), new Vector3(24.1f, 0.72f, 0.48f), trimMat, false);

        for (int x = -3; x <= 3; x++)
        {
            for (int z = -4; z <= 4; z++)
            {
                float clampedX = Mathf.Clamp(x * 3.65f, -10.05f, 10.05f);
                float clampedZ = Mathf.Clamp(z * 3.15f + 0.6f, -12.0f, 13.2f);
                Vector3 center = new Vector3(clampedX, 0.035f, clampedZ);
                CreateBox($"FloorPlate_{x}_{z}", center, new Vector3(3.35f, 0.045f, 2.65f), panelMat, false);
                CreateBox($"FloorPlateLipA_{x}_{z}", center + new Vector3(0f, 0.055f, 1.4f), new Vector3(3.22f, 0.035f, 0.08f), trimMat, false);
                CreateBox($"FloorPlateLipB_{x}_{z}", center + new Vector3(0f, 0.055f, -1.4f), new Vector3(3.22f, 0.035f, 0.08f), trimMat, false);
            }
        }

        CreatePerimeterDeckDetail();
        CreateFloorTrack("MainAmberGuide", 0f, -11.2f, 12.1f, amberGlowMat);
        CreateFloorTrack("LeftServiceTrench", -6.4f, -10.4f, 9.5f, trimMat);
        CreateFloorTrack("RightServiceTrench", 6.4f, -8.2f, 11.3f, trimMat);
    }

    void CreatePerimeterDeckDetail()
    {
        for (int i = 0; i < 9; i++)
        {
            float z = -12.0f + i * 3.15f;
            CreateBox($"LeftEdgeDeckPlate_{i}", new Vector3(-11.1f, 0.07f, z), new Vector3(1.05f, 0.06f, 2.45f), panelMat, false);
            CreateBox($"RightEdgeDeckPlate_{i}", new Vector3(11.1f, 0.07f, z), new Vector3(1.05f, 0.06f, 2.45f), panelMat, false);
            CreateBox($"LeftEdgeGlow_{i}", new Vector3(-10.55f, 0.12f, z), new Vector3(0.08f, 0.045f, 1.75f), i % 2 == 0 ? amberGlowMat : greenGlowMat, false);
            CreateBox($"RightEdgeGlow_{i}", new Vector3(10.55f, 0.12f, z), new Vector3(0.08f, 0.045f, 1.75f), i % 2 == 0 ? amberGlowMat : greenGlowMat, false);
        }

        for (int i = 0; i < 7; i++)
        {
            float x = -9.9f + i * 3.3f;
            CreateBox($"FrontEdgeDeckPlate_{i}", new Vector3(x, 0.07f, -12.7f), new Vector3(2.65f, 0.06f, 0.95f), panelMat, false);
            CreateBox($"BackEdgeDeckPlate_{i}", new Vector3(x, 0.07f, 14.0f), new Vector3(2.65f, 0.06f, 0.95f), panelMat, false);
            CreateBox($"FrontEdgeWarning_{i}", new Vector3(x, 0.13f, -12.18f), new Vector3(1.45f, 0.045f, 0.08f), amberGlowMat, false);
            CreateBox($"BackEdgeWarning_{i}", new Vector3(x, 0.13f, 13.48f), new Vector3(1.45f, 0.045f, 0.08f), amberGlowMat, false);
        }
    }

    void CreateFloorTrack(string prefix, float x, float zMin, float zMax, Material mat)
    {
        float z = zMin;
        int i = 0;
        while (z < zMax)
        {
            CreateBox($"{prefix}_{i}", new Vector3(x, 0.08f, z), new Vector3(0.42f, 0.04f, 1.35f), mat, false);
            z += 1.8f;
            i++;
        }
    }

    void BuildWallBays()
    {
        for (int i = 0; i < 6; i++)
        {
            float z = -10.8f + i * 4.45f;
            CreateWallBay($"L{i}", -11.83f, z, true);
            CreateWallBay($"R{i}", 11.83f, z, false);
            CreateArchRib($"Arch_{i}", z + 1.9f);
        }

        CreateDoorAssembly("ExitDoorSet", new Vector3(0f, 1.95f, 14.55f), true);
        CreateDoorAssembly("EntryDoorSet", new Vector3(0f, 1.95f, -13.35f), false);
    }

    void CreateWallBay(string id, float x, float z, bool left)
    {
        float side = left ? -1f : 1f;
        CreateBox($"WallBay_{id}_Frame", new Vector3(x, 2.05f, z), new Vector3(0.18f, 3.1f, 3.3f), creamFrameMat, false);
        CreateBox($"WallBay_{id}_Inset", new Vector3(x - side * 0.06f, 2.1f, z), new Vector3(0.12f, 2.2f, 2.35f), panelMat, false);
        CreateBox($"WallBay_{id}_LowerConsole", new Vector3(x - side * 0.45f, 0.72f, z), new Vector3(0.82f, 0.55f, 2.2f), trimMat, true);
        CreateBox($"WallBay_{id}_StatusGreen", new Vector3(x - side * 0.9f, 0.98f, z - 0.65f), new Vector3(0.06f, 0.16f, 0.55f), greenGlowMat, false);
        CreateBox($"WallBay_{id}_StatusAmber", new Vector3(x - side * 0.9f, 1.24f, z + 0.55f), new Vector3(0.06f, 0.12f, 0.42f), amberGlowMat, false);

        for (int slat = 0; slat < 5; slat++)
        {
            CreateBox($"WallBay_{id}_Vent_{slat}", new Vector3(x - side * 0.16f, 2.95f - slat * 0.18f, z + 1.15f), new Vector3(0.08f, 0.045f, 0.95f), trimMat, false);
        }

        CreatePipeRun($"WallBay_{id}_PipeA", new Vector3(x - side * 0.28f, 3.35f, z - 1.15f), left ? 90f : -90f, 1.5f, amberGlowMat);
        CreatePipeRun($"WallBay_{id}_PipeB", new Vector3(x - side * 0.34f, 1.65f, z + 0.95f), left ? 90f : -90f, 1.25f, trimMat);
    }

    void CreateArchRib(string name, float z)
    {
        CreateBox($"{name}_LeftPillar", new Vector3(-9.65f, 2.05f, z), new Vector3(0.62f, 3.55f, 0.32f), creamFrameMat, true);
        CreateBox($"{name}_RightPillar", new Vector3(9.65f, 2.05f, z), new Vector3(0.62f, 3.55f, 0.32f), creamFrameMat, true);
        CreateBox($"{name}_Header", new Vector3(0f, 3.82f, z), new Vector3(19.6f, 0.48f, 0.32f), creamFrameMat, false);
        CreateBox($"{name}_LeftBrace", new Vector3(-8.6f, 3.3f, z), new Vector3(2.0f, 0.35f, 0.34f), creamFrameMat, false).transform.rotation = Quaternion.Euler(0f, 0f, 24f);
        CreateBox($"{name}_RightBrace", new Vector3(8.6f, 3.3f, z), new Vector3(2.0f, 0.35f, 0.34f), creamFrameMat, false).transform.rotation = Quaternion.Euler(0f, 0f, -24f);
        CreateBox($"{name}_WarningLeft", new Vector3(-6.4f, 3.55f, z - 0.03f), new Vector3(1.4f, 0.08f, 0.36f), amberGlowMat, false);
        CreateBox($"{name}_WarningRight", new Vector3(6.4f, 3.55f, z - 0.03f), new Vector3(1.4f, 0.08f, 0.36f), amberGlowMat, false);
    }

    void BuildCeilingPanels()
    {
        for (int z = 0; z < 7; z++)
        {
            float zPos = -11.0f + z * 4.0f;
            CreateBox($"CeilingPanel_{z}", new Vector3(0f, 4.16f, zPos), new Vector3(8.5f, 0.16f, 2.6f), panelMat, false);
            CreateBox($"CeilingLight_{z}", new Vector3(0f, 4.03f, zPos), new Vector3(5.6f, 0.08f, 0.22f), z % 2 == 0 ? cyanGlowMat : amberGlowMat, false);
            CreateCylinder($"CeilingPipeL_{z}", new Vector3(-7.8f, 3.95f, zPos), new Vector3(0.055f, 2.0f, 0.055f), Quaternion.Euler(0f, 0f, 90f), trimMat);
            CreateCylinder($"CeilingPipeR_{z}", new Vector3(7.8f, 3.95f, zPos), new Vector3(0.055f, 2.0f, 0.055f), Quaternion.Euler(0f, 0f, 90f), trimMat);
        }
    }

    void CreateDoorAssembly(string prefix, Vector3 center, bool exit)
    {
        CreateBox($"{prefix}_Bulkhead", center, new Vector3(5.4f, 3.0f, 0.28f), trimMat, false);
        CreateBox($"{prefix}_PanelA", center + new Vector3(-1.25f, 0f, exit ? -0.05f : 0.05f), new Vector3(1.95f, 2.25f, 0.16f), panelMat, false);
        CreateBox($"{prefix}_PanelB", center + new Vector3(1.25f, 0f, exit ? -0.05f : 0.05f), new Vector3(1.95f, 2.25f, 0.16f), panelMat, false);
        CreateBox($"{prefix}_TopLamp", center + new Vector3(0f, 1.48f, exit ? -0.1f : 0.1f), new Vector3(3.4f, 0.18f, 0.16f), exit ? greenGlowMat : amberGlowMat, false);
        CreateBox($"{prefix}_LowerStripe", center + new Vector3(0f, -1.25f, exit ? -0.1f : 0.1f), new Vector3(4.7f, 0.12f, 0.16f), amberGlowMat, false);
    }

    void CreatePipeRun(string name, Vector3 pos, float yaw, float length, Material mat)
    {
        var pipe = CreateCylinder(name, pos, new Vector3(0.045f, length, 0.045f), Quaternion.Euler(0f, 0f, 90f), mat);
        pipe.transform.rotation = Quaternion.Euler(0f, yaw, 90f);
        CreateCylinder($"{name}_JointA", pos + pipe.transform.right * (length * 0.5f), new Vector3(0.08f, 0.08f, 0.08f), Quaternion.identity, trimMat);
        CreateCylinder($"{name}_JointB", pos - pipe.transform.right * (length * 0.5f), new Vector3(0.08f, 0.08f, 0.08f), Quaternion.identity, trimMat);
    }

    void ConfigurePlayerStart()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        player.transform.position = new Vector3(0f, 1.05f, -11.4f);
        player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        var mover = player.GetComponent<SimplePlayerMovement>();
        if (mover != null)
            mover.moveSpeed = 6.2f;
    }

    void ConfigureCoreAndDock()
    {
        if (magneticCore != null)
        {
            magneticCore.transform.position = new Vector3(0f, 0.75f, -8.2f);
            magneticCore.transform.localScale = Vector3.one * 1.18f;
            ApplyMaterial(magneticCore, cyanGlowMat);

            var coreController = magneticCore.GetComponent<MagneticCoreController>();
            if (coreController != null)
                coreController.SetHomeTransform(magneticCore.transform.position, magneticCore.transform.rotation);
        }

        if (powerDock != null)
        {
            powerDock.transform.position = new Vector3(0f, 0.14f, 12.15f);
            powerDock.transform.localScale = new Vector3(2.2f, 0.12f, 2.2f);
            ApplyMaterial(powerDock, greenGlowMat);
        }

        ConfigureBox("ExitDoor", new Vector3(0f, 1.7f, 14.2f), new Vector3(4.8f, 3.0f, 0.32f), panelMat);
        CreateBox("ExitPortal_StopperWall", new Vector3(0f, 1.45f, 14.86f), new Vector3(5.4f, 2.8f, 0.38f), trimMat, true);
        CreateBox("ExitPortal_StopperGlow", new Vector3(0f, 1.52f, 14.64f), new Vector3(4.6f, 0.1f, 0.06f), greenGlowMat, false);

        var goal = FindFirstObjectByType<GoalTrigger>();
        if (goal != null && goal.exitZone != null)
            ConfigureObject(goal.exitZone, new Vector3(0f, 1.2f, 13.7f), new Vector3(4.6f, 2.3f, 1.4f), greenGlowMat);
    }

    void ConfigureMagnetRoute(MagneticCoreController coreController)
    {
        string[] ids = { "A", "B", "C", "D", "E", "F" };
        Vector3[] platePositions =
        {
            new Vector3(-8.2f, 0.16f, -9.1f),
            new Vector3( 8.2f, 0.16f, -8.4f),
            new Vector3(-8.4f, 0.16f, -1.7f),
            new Vector3( 8.4f, 0.16f,  2.2f),
            new Vector3(-7.4f, 0.16f,  8.7f),
            new Vector3( 2.75f, 0.16f, 10.15f)
        };
        Vector3[] targetPositions =
        {
            new Vector3(-7.1f, 0.78f, -5.4f),
            new Vector3( 6.9f, 0.78f, -4.2f),
            new Vector3(-6.7f, 0.78f,  1.6f),
            new Vector3( 6.7f, 0.78f,  5.2f),
            new Vector3(-4.9f, 0.78f,  8.9f),
            new Vector3( 0.0f, 0.78f, 12.15f)
        };

        for (int i = 0; i < ids.Length; i++)
            ConfigureMagnetStation(ids[i], i, platePositions[i], targetPositions[i], coreController);
    }

    void ConfigureMagnetStation(string id, int index, Vector3 platePosition, Vector3 targetPosition, MagneticCoreController coreController)
    {
        var target = GameObject.Find($"MagnetPoint_{id}");
        if (target == null)
        {
            target = new GameObject($"MagnetPoint_{id}");
            target.hideFlags = GeneratedHideFlags;
            target.transform.SetParent(labRoot);
        }
        target.transform.position = targetPosition;

        var plate = GameObject.Find($"MagnetButton_{id}");
        if (plate == null)
            plate = CreateBox($"MagnetButton_{id}", platePosition, new Vector3(2.1f, 0.12f, 1.65f), cyanGlowMat, true);

        ConfigureObject(plate, platePosition, new Vector3(2.1f, 0.12f, 1.65f), cyanGlowMat);

        var button = plate.GetComponent<MagnetButton>();
        if (button == null) button = plate.AddComponent<MagnetButton>();
        button.RefreshRestPosition();
        button.magneticCore = coreController;
        button.magnetTarget = target.transform;
        button.buttonIndex = index;
        button.detectionRadius = 1.12f;
        button.detectionHeight = 2.3f;

        CreateMagnetDevice($"MagnetRelay_{id}", targetPosition, index);
        CreateButtonFrame($"PlateFrame_{id}", platePosition);
        CreatePlateLabel($"PlateLabel_{id}", id, platePosition);
    }

    void CreatePlateLabel(string name, string label, Vector3 platePosition)
    {
        var go = new GameObject(name);
        go.hideFlags = GeneratedHideFlags;
        go.transform.SetParent(labRoot);
        go.transform.position = platePosition + new Vector3(0f, 0.13f, 0f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = Vector3.one * 0.42f;

        var text = go.AddComponent<TextMeshPro>();
        text.text = label;
        text.fontSize = 4.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.02f, 0.03f, 0.035f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(3f, 1f);
    }

    void CreateMagnetDevice(string name, Vector3 basePos, int index)
    {
        Material glow = index == 5 ? greenGlowMat : cyanGlowMat;
        Vector3 floorCenter = new Vector3(basePos.x, 0.08f, basePos.z);
        CreateCylinder($"{name}_Base", floorCenter, new Vector3(0.42f, 0.08f, 0.42f), Quaternion.identity, trimMat);
        CreateCylinder($"{name}_Column", floorCenter + Vector3.up * 0.68f, new Vector3(0.12f, 0.68f, 0.12f), Quaternion.identity, trimMat);
        CreateCylinder($"{name}_EmitterDisk", floorCenter + Vector3.up * 1.38f, new Vector3(0.52f, 0.09f, 0.52f), Quaternion.identity, glow);
        CreateCylinder($"{name}_CoilRingLower", floorCenter + Vector3.up * 0.98f, new Vector3(0.36f, 0.045f, 0.36f), Quaternion.identity, amberGlowMat);
        CreateCylinder($"{name}_CoilRingUpper", floorCenter + Vector3.up * 1.18f, new Vector3(0.32f, 0.045f, 0.32f), Quaternion.identity, amberGlowMat);

        var lt = CreatePointLight($"{name}_Light", floorCenter + Vector3.up * 1.38f, index == 5 ? DockColor : CoreColor, 1.0f, 3.2f);
        lt.transform.SetParent(labRoot);
    }

    void CreateButtonFrame(string name, Vector3 pos)
    {
        CreateBox($"{name}_Base", pos + Vector3.down * 0.055f, new Vector3(2.6f, 0.08f, 2.08f), trimMat, false);
        CreateBox($"{name}_FrontLip", pos + new Vector3(0f, 0.04f, -0.94f), new Vector3(2.45f, 0.08f, 0.12f), amberGlowMat, false);
        CreateBox($"{name}_BackLip", pos + new Vector3(0f, 0.04f, 0.94f), new Vector3(2.45f, 0.08f, 0.12f), trimMat, false);
    }

    void BuildStationArchitecture(MagneticCoreController coreController)
    {
        DisableOriginalDangerZone();

        CreateHazardField("UnstableField_Center", new Vector3(-1.2f, 0.3f, -1.9f), new Vector3(3.4f, 0.5f, 0.8f), coreController);
        CreateHazardField("UnstableField_West", new Vector3(-3.3f, 0.3f, 3.5f), new Vector3(0.74f, 0.5f, 2.8f), coreController);
        CreateHazardField("UnstableField_East", new Vector3(5.9f, 0.3f, 6.5f), new Vector3(1.0f, 0.5f, 2.4f), coreController);

        CreateResearchTank("CryoMagTank_Left", new Vector3(-9.9f, 1.2f, 9.0f));
        CreateResearchTank("CryoMagTank_Right", new Vector3(9.9f, 1.2f, -6.7f));
        CreateServerRack("ServerStack_L", new Vector3(-10.0f, 1.25f, 1.7f), -90f);
        CreateServerRack("ServerStack_R", new Vector3(10.0f, 1.25f, -0.9f), 90f);
        CreateControlConsole("ControlConsole_A", new Vector3(-10.1f, 0.55f, 5.2f), 90f);
        CreateControlConsole("ControlConsole_B", new Vector3(10.1f, 0.55f, 7.6f), -90f);
        CreateContainmentRing(new Vector3(0f, 0.08f, -8.2f), 3.2f, "StartContainment", cyanGlowMat);
        CreateContainmentRing(new Vector3(0f, 0.08f, 12.15f), 3.8f, "DockContainment", greenGlowMat);
    }

    void CreateResearchTank(string name, Vector3 pos)
    {
        CreateCylinder($"{name}_Glass", pos + Vector3.up * 0.65f, new Vector3(0.72f, 1.2f, 0.72f), Quaternion.identity, glassMat);
        CreateCylinder($"{name}_TopCap", pos + Vector3.up * 1.9f, new Vector3(0.82f, 0.12f, 0.82f), Quaternion.identity, trimMat);
        CreateCylinder($"{name}_BottomCap", pos - Vector3.up * 0.45f, new Vector3(0.82f, 0.12f, 0.82f), Quaternion.identity, trimMat);
        CreateCylinder($"{name}_Pipe", pos + new Vector3(0.95f, 1.1f, 0f), new Vector3(0.045f, 1.2f, 0.045f), Quaternion.Euler(0f, 0f, 90f), trimMat);
        CreatePointLight($"{name}_Light", pos + Vector3.up * 1.1f, CoreColor, 1.2f, 3.5f).transform.SetParent(labRoot);
    }

    void CreateServerRack(string name, Vector3 pos, float yaw)
    {
        var rack = CreateBox($"{name}_Body", pos, new Vector3(0.75f, 2.4f, 2.0f), trimMat, true);
        rack.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        for (int i = 0; i < 6; i++)
        {
            var strip = CreateBox($"{name}_DataStrip_{i}", pos + new Vector3(0f, -0.82f + i * 0.32f, 0.78f), new Vector3(0.8f, 0.08f, 0.08f), i % 2 == 0 ? greenGlowMat : cyanGlowMat, false);
            strip.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    void CreateControlConsole(string name, Vector3 pos, float yaw)
    {
        var baseBox = CreateBox($"{name}_Base", pos, new Vector3(2.2f, 0.85f, 0.8f), trimMat, true);
        baseBox.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var screen = CreateBox($"{name}_Screen", pos + new Vector3(0f, 0.55f, -0.28f), new Vector3(1.7f, 0.12f, 0.58f), cyanGlowMat, false);
        screen.transform.rotation = Quaternion.Euler(-25f, yaw, 0f);
        var keys = CreateBox($"{name}_KeyLights", pos + new Vector3(0f, 0.48f, 0.2f), new Vector3(1.45f, 0.08f, 0.16f), amberGlowMat, false);
        keys.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void CreateHazardField(string name, Vector3 position, Vector3 scale, MagneticCoreController coreController)
    {
        var zone = CreateBox(name, position, scale, hazardMat, true);
        var col = zone.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
        var danger = zone.GetComponent<DangerZone>();
        if (danger == null) danger = zone.AddComponent<DangerZone>();
        danger.magneticCore = coreController;

        CreateBox($"{name}_StripeA", position + new Vector3(0f, 0.28f, scale.z * 0.54f), new Vector3(scale.x, 0.06f, 0.1f), amberGlowMat, false);
        CreateBox($"{name}_StripeB", position - new Vector3(0f, -0.28f, scale.z * 0.54f), new Vector3(scale.x, 0.06f, 0.1f), amberGlowMat, false);
    }

    void CreateContainmentRing(Vector3 center, float radius, string prefix, Material glow)
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0.08f, Mathf.Sin(angle) * radius);
            Quaternion rot = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            CreateBox($"{prefix}_Segment_{i}", pos, new Vector3(0.78f, 0.08f, 0.16f), i % 2 == 0 ? glow : trimMat, false).transform.rotation = rot;
        }
    }

    void ConfigureBox(string name, Vector3 position, Vector3 scale, Material mat)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        ConfigureObject(go, position, scale, mat);
    }

    void HideSceneRenderer(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    void ConfigureObject(GameObject go, Vector3 position, Vector3 scale, Material mat)
    {
        go.transform.position = position;
        go.transform.localScale = scale;
        ReplaceSceneBoxMesh(go);
        ApplyMaterial(go, mat);
    }

    void ReplaceSceneBoxMesh(GameObject go)
    {
        if (go == null) return;
        string n = go.name;
        if (!n.StartsWith("MagnetButton_") && n != "PowerDock" && n != "ExitDoor") return;

        var filter = go.GetComponent<MeshFilter>();
        if (filter == null) return;
        var mesh = CreateChamferedBoxMesh(Vector3.one);
        mesh.hideFlags = HideFlags.DontSave;
        filter.sharedMesh = mesh;
    }

    void DisableOriginalDangerZone()
    {
        var original = GameObject.Find("DangerZone");
        if (original == null) return;

        var renderer = original.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;

        foreach (var col in original.GetComponents<Collider>())
            col.enabled = false;

        var danger = original.GetComponent<DangerZone>();
        if (danger != null)
            danger.enabled = false;
    }

    GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material mat, bool solid)
    {
        var go = new GameObject(name);
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = CreateChamferedBoxMesh(Vector3.one);
        go.AddComponent<MeshRenderer>();
        PrepareGenerated(go, name, position, scale, Quaternion.identity, mat);
        if (solid)
        {
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
        }
        return go;
    }

    GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material mat)
    {
        var go = new GameObject(name);
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = CreateOctagonalPrismMesh(Vector3.one);
        go.AddComponent<MeshRenderer>();
        PrepareGenerated(go, name, position, scale, rotation, mat);
        return go;
    }

    void PrepareGenerated(GameObject go, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material mat)
    {
        go.name = name;
        go.hideFlags = GeneratedHideFlags;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.transform.rotation = rotation;
        go.transform.SetParent(labRoot);
        ApplyMaterial(go, mat);
    }

    Mesh CreateChamferedBoxMesh(Vector3 size)
    {
        int axisE = SmallestAxis(size);
        int axisU = axisE == 0 ? 1 : 0;
        int axisV = axisE == 2 ? 1 : 2;
        if (axisE == 1) { axisU = 0; axisV = 2; }

        float u = GetAxis(size, axisU) * 0.5f;
        float v = GetAxis(size, axisV) * 0.5f;
        float e = GetAxis(size, axisE) * 0.5f;
        float c = Mathf.Min(u, v) * 0.18f;
        c = Mathf.Clamp(c, 0.025f, 0.22f);

        Vector2[] outline =
        {
            new Vector2(-u + c, -v), new Vector2(u - c, -v),
            new Vector2(u, -v + c), new Vector2(u, v - c),
            new Vector2(u - c, v), new Vector2(-u + c, v),
            new Vector2(-u, v - c), new Vector2(-u, -v + c)
        };

        var vertices = new Vector3[18];
        for (int i = 0; i < outline.Length; i++)
        {
            vertices[i] = Compose(axisU, outline[i].x, axisV, outline[i].y, axisE, e);
            vertices[i + 8] = Compose(axisU, outline[i].x, axisV, outline[i].y, axisE, -e);
        }
        vertices[16] = Compose(axisU, 0f, axisV, 0f, axisE, e);
        vertices[17] = Compose(axisU, 0f, axisV, 0f, axisE, -e);

        var triangles = new int[8 * 3 * 2 + 8 * 6];
        int t = 0;
        for (int i = 0; i < 8; i++)
        {
            int next = (i + 1) % 8;
            triangles[t++] = 16; triangles[t++] = i; triangles[t++] = next;
            triangles[t++] = 17; triangles[t++] = next + 8; triangles[t++] = i + 8;
            triangles[t++] = i; triangles[t++] = i + 8; triangles[t++] = next + 8;
            triangles[t++] = i; triangles[t++] = next + 8; triangles[t++] = next;
        }

        var mesh = new Mesh { name = "Chamfered modular hard-surface panel" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh CreateOctagonalPrismMesh(Vector3 size)
    {
        const int sides = 10;
        float radiusX = size.x * 0.5f;
        float radiusZ = size.z * 0.5f;
        float halfY = size.y * 0.5f;

        var vertices = new Vector3[sides * 2 + 2];
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides;
            float x = Mathf.Cos(a) * radiusX;
            float z = Mathf.Sin(a) * radiusZ;
            vertices[i] = new Vector3(x, halfY, z);
            vertices[i + sides] = new Vector3(x, -halfY, z);
        }
        vertices[sides * 2] = new Vector3(0f, halfY, 0f);
        vertices[sides * 2 + 1] = new Vector3(0f, -halfY, 0f);

        var triangles = new int[sides * 12];
        int t = 0;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            triangles[t++] = sides * 2; triangles[t++] = next; triangles[t++] = i;
            triangles[t++] = sides * 2 + 1; triangles[t++] = i + sides; triangles[t++] = next + sides;
            triangles[t++] = i; triangles[t++] = next; triangles[t++] = next + sides;
            triangles[t++] = i; triangles[t++] = next + sides; triangles[t++] = i + sides;
        }

        var mesh = new Mesh { name = "Decagonal hard-surface pipe" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    int SmallestAxis(Vector3 v)
    {
        if (v.x <= v.y && v.x <= v.z) return 0;
        if (v.y <= v.x && v.y <= v.z) return 1;
        return 2;
    }

    float GetAxis(Vector3 v, int axis)
    {
        if (axis == 0) return v.x;
        if (axis == 1) return v.y;
        return v.z;
    }

    Vector3 Compose(int axisA, float valueA, int axisB, float valueB, int axisC, float valueC)
    {
        var v = Vector3.zero;
        SetAxis(ref v, axisA, valueA);
        SetAxis(ref v, axisB, valueB);
        SetAxis(ref v, axisC, valueC);
        return v;
    }

    void SetAxis(ref Vector3 v, int axis, float value)
    {
        if (axis == 0) v.x = value;
        else if (axis == 1) v.y = value;
        else v.z = value;
    }

    void ApplyMaterial(GameObject go, Material mat)
    {
        if (go == null || mat == null) return;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = mat;
    }

    void SetupCoreMaterial()
    {
        if (magneticCore == null) return;
        coreRenderer = magneticCore.GetComponent<Renderer>();
        if (coreRenderer != null)
            coreMaterialInstance = Application.isPlaying ? coreRenderer.material : coreRenderer.sharedMaterial;
    }

    void CreateAtmosphericLights()
    {
        coreLight = CreatePointLight("CoreLight", magneticCore != null ? magneticCore.transform.position : Vector3.zero, CoreColor, 2.5f, 5f);

        if (powerDock != null)
            dockLight = CreatePointLight("DockLight", powerDock.transform.position + Vector3.up * 0.8f, DockColor, 1.8f, 4.5f);

        int lightCount = Mathf.Max(1, magnetButtons != null ? magnetButtons.Length : 0);
        if (magnetButtons != null)
        {
            for (int i = 0; i < magnetButtons.Length; i++)
            {
                var button = magnetButtons[i] != null ? magnetButtons[i].GetComponent<MagnetButton>() : null;
                if (button != null)
                    lightCount = Mathf.Max(lightCount, button.buttonIndex + 1);
            }
        }

        buttonLights = new Light[lightCount];
        if (magnetButtons == null) return;

        for (int i = 0; i < magnetButtons.Length; i++)
        {
            if (magnetButtons[i] == null) continue;
            var button = magnetButtons[i].GetComponent<MagnetButton>();
            int lightIndex = button != null ? button.buttonIndex : i;
            buttonLights[lightIndex] = CreatePointLight($"ButtonLight_{i}", magnetButtons[i].transform.position + Vector3.up * 0.7f, ButtonColor, 0.85f, 2.6f);
        }
    }

    void ApplySpaceStationLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.025f, 0.022f, 0.018f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.032f, 0.03f);
        RenderSettings.fogDensity = 0.012f;

        CreatePointLight("Lab_Warm_Key_Left", new Vector3(-6.5f, 3.2f, -7.5f), new Color(1f, 0.58f, 0.18f), 2.3f, 9f);
        CreatePointLight("Lab_Cyan_Core_Wash", new Vector3(5.4f, 2.8f, 2.5f), CoreColor, 1.45f, 8f);
        CreatePointLight("Lab_Dock_Green_Wash", new Vector3(0f, 2.8f, 12f), DockColor, 2.0f, 7f);
    }

    Light CreatePointLight(string lightName, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(lightName);
        go.hideFlags = GeneratedHideFlags;
        go.transform.position = position;
        if (labRoot != null)
            go.transform.SetParent(labRoot);

        var lt = go.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = color;
        lt.intensity = intensity;
        lt.range = range;
        lt.shadows = LightShadows.None;
        return lt;
    }

    void ConfigureInstructionText()
    {
        if (instructionText == null) return;
        instructionText.fontSize = 25;
        instructionText.textWrappingMode = TextWrappingModes.Normal;
        instructionText.alignment = TextAlignmentOptions.Left;
        instructionText.margin = new Vector4(18f, 10f, 18f, 10f);
        instructionText.rectTransform.anchorMin = new Vector2(0f, 0f);
        instructionText.rectTransform.anchorMax = new Vector2(1f, 0f);
        instructionText.rectTransform.pivot = new Vector2(0.5f, 0f);
        instructionText.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        instructionText.rectTransform.sizeDelta = new Vector2(0f, 118f);
    }

    void Update()
    {
        if (!Application.isPlaying || puzzleComplete) return;

        AnimateCoreGlow();
        TrackCoreLightPosition();
        CheckPuzzleComplete();
    }

    void AnimateCoreGlow()
    {
        if (coreMaterialInstance == null || coreLight == null) return;

        float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        coreLight.intensity = 2.5f * pulse;
        coreMaterialInstance.SetColor("_EmissionColor", new Color(0.2f, 1.2f, 2.0f) * pulse);
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

        showCompletionPanel = true;
        UpdateInstructionText(true);
        StartCoroutine(ShowSuccessOverlay());
    }

    System.Collections.IEnumerator ShowSuccessOverlay()
    {
        yield return new WaitForSeconds(2.2f);
        showCompletionPanel = false;
    }

    public void HideCompletionOverlay()
    {
        showCompletionPanel = false;
        if (instructionText != null)
            instructionText.enabled = false;
    }

    void OnGUI()
    {
        if (!Application.isPlaying || !showCompletionPanel)
            return;

        InitCompletionStyles();
        ImguiScale.Begin();

        float boxW = 420f;
        float boxH = 168f;
        float x = ImguiScale.Width * 0.5f - boxW * 0.5f;
        float y = ImguiScale.Height * 0.5f - boxH * 0.5f;
        var panelRect = new Rect(x, y, boxW, boxH);

        GUI.DrawTexture(panelRect, completionPanelTexture);
        DrawCompletionBorder(panelRect, new Color(0.15f, 0.92f, 1f, 0.92f), 2f);
        DrawCompletionBorder(new Rect(x + 6f, y + 6f, boxW - 12f, boxH - 12f), new Color(0.15f, 0.92f, 1f, 0.32f), 1f);

        completionTitleStyle.normal.textColor = new Color(0.2f, 1f, 0.58f);
        GUI.Label(new Rect(x, y + 28f, boxW, 48f), "ROOM CLEARED", completionTitleStyle);

        completionMessageStyle.normal.textColor = new Color(0.76f, 0.96f, 1f);
        GUI.Label(new Rect(x + 28f, y + 82f, boxW - 56f, 30f), "Return route unlocked.", completionMessageStyle);
        GUI.Label(new Rect(x + 28f, y + 112f, boxW - 56f, 26f), "Exit through the green bulkhead.", completionMessageStyle);
    }

    void InitCompletionStyles()
    {
        if (completionTitleStyle != null)
            return;

        completionPanelTexture = MakeCompletionTexture(new Color(0.005f, 0.012f, 0.028f, 0.985f));
        completionLineTexture = MakeCompletionTexture(Color.white);

        completionTitleStyle = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold
        };

        completionMessageStyle = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
    }

    void DrawCompletionBorder(Rect rect, Color color, float thickness)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), completionLineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), completionLineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), completionLineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), completionLineTexture);
        GUI.color = old;
    }

    Texture2D MakeCompletionTexture(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    public void FlashButton(int buttonIndex)
    {
        if (buttonLights == null || buttonIndex < 0 || buttonIndex >= buttonLights.Length) return;
        var lt = buttonLights[buttonIndex];
        if (lt != null && Application.isPlaying)
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
                "<color=#00FF88><b>CORE DOCKED - SYSTEM UNLOCKED</b></color>  " +
                "<size=80%>Exit door is open. Leave through the green bulkhead.</size>";
        }
        else
        {
            instructionText.text =
                "<b>MAGNETIC CORE LAB</b>  " +
                "<size=78%><color=#74DFFF>Step on relay plates A-F</color> to pull the core from emitter to emitter. " +
                "<color=#FF6A3D>Red unstable fields reset the core</color>. " +
                "Dock it at the <color=#39FF7A>green power station</color>, then exit.</size>";
        }
    }
}
