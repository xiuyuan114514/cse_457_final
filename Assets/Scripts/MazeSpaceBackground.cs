using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Attach to any GameObject in the Maze scene.
/// At runtime, replaces the default skybox with a procedural Star Wars–style
/// space environment: deep-black sky, scattered stars, and colored nebula glow.
/// </summary>
[ExecuteAlways]
public class MazeSpaceBackground : MonoBehaviour
{
    [Header("Star Dome")]
    [Tooltip("Radius of the star dome sphere")]
    public float domeRadius = 500f;

    [Header("Stars")]
    public int starCount = 1200;
    public float starMinSize = 0.4f;
    public float starMaxSize = 1.6f;

    [Header("Nebula Fog")]
    // Matches LaserRoom scene: fog disabled.
    public bool enableFog = false;
    public Color fogColor = new Color(0.01f, 0.015f, 0.025f, 1f);
    public float fogDensity = 0.008f;

    // Runtime references
    GameObject domeRoot;
    GameObject visualRoot;
    Material domeMaterial;
    Material starMaterialWhite;
    Material starMaterialBlue;
    Material deckMaterial;
    Material wallMaterial;
    Material trimMaterial;
    Material frameMaterial;
    Material cyanLightMaterial;
    Material amberLightMaterial;
    Material greenLightMaterial;
    Material darkGlassMaterial;
    static Material editKeyMaterial;

    const string ImportedKeyResourcePath = "OpenGameArt/KeyLowPoly/key";
    const float ImportedKeyTargetHeight = 1.5f;
    const float ImportedKeyGroundClearance = 0.34f;

    HideFlags GeneratedHideFlags =>
        Application.isPlaying
            ? HideFlags.None
            : (HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild);

    void OnEnable()
    {
        RebuildEnvironment();
    }

    void Start()
    {
        if (Application.isPlaying && visualRoot == null)
            RebuildEnvironment();
    }

    void RebuildEnvironment()
    {
        SetupCamera();
        SetupAmbientLighting();
        SetupFog();
        ClearEnvironmentVisuals();

        // The space backdrop is purely cosmetic. Isolate it so a failure here
        // (e.g. a stripped shader in a player build) can never abort the
        // gameplay-critical maze rebuild and leave the level pitch black.
        try
        {
            CreateStarDome();
            CreateStars();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"MazeSpaceBackground: skipped space backdrop ({e.Message})");
        }

        CreateMazeVisualPolish();
    }

    void ClearEnvironmentVisuals()
    {
        DestroyGenerated(domeRoot);
        DestroyGenerated(visualRoot);
        DestroyGenerated(GameObject.Find("StarDome"));
        DestroyGenerated(GameObject.Find("Stars"));
        DestroyGenerated(GameObject.Find("Runtime_MazeVisualPolish"));
        domeRoot = null;
        visualRoot = null;
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.002f, 0.004f, 0.01f, 1f);
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, domeRadius * 2f);
    }

    void SetupAmbientLighting()
    {
        // Matches LaserRoom scene's gradient ambient lighting.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.035f, 0.05f, 0.085f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.014f, 0.02f, 0.035f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.005f, 0.006f, 0.012f, 1f);
        RenderSettings.ambientIntensity = 0.45f;
        RenderSettings.reflectionIntensity = 0.45f;
    }

    void SetupFog()
    {
        if (!enableFog) return;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
    }

    void CreateStarDome()
    {
        domeRoot = new GameObject("StarDome");
        domeRoot.hideFlags = GeneratedHideFlags;
        domeRoot.transform.SetParent(transform, false);

        // Create an inverted sphere (renders on the inside)
        var meshFilter = domeRoot.AddComponent<MeshFilter>();
        var meshRenderer = domeRoot.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateInvertedSphereMesh(domeRadius, 32, 32);

        // Very dark emissive material for the dome
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        domeMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        // Matches LaserRoom M1_StarDome material: near-black deep blue.
        domeMaterial.SetColor("_BaseColor", new Color(0.002f, 0.004f, 0.012f, 1f));
        domeMaterial.SetColor("_Color", new Color(0.002f, 0.004f, 0.012f, 1f));
        // Render behind everything
        domeMaterial.renderQueue = (int)RenderQueue.Background;

        meshRenderer.material = domeMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    void CreateStars()
    {
        // Prepare star materials matching LaserRoom (M1_StarWhite / M1_StarBlue emission).
        starMaterialWhite = CreateStarMaterial(new Color(2.2f, 2.4f, 2.6f, 1f), 1f);
        starMaterialBlue = CreateStarMaterial(new Color(0.85f, 1.45f, 2.8f, 1f), 1f);

        var starsParent = new GameObject("Stars");
        starsParent.hideFlags = GeneratedHideFlags;
        starsParent.transform.SetParent(transform, false);

        // Shared quad mesh for all stars
        Mesh quadMesh = CreateQuadMesh();

        int count = Application.isPlaying ? starCount : Mathf.Min(starCount, 350);
        for (int i = 0; i < count; i++)
        {
            var starGO = new GameObject("Star");
            starGO.hideFlags = GeneratedHideFlags;
            starGO.transform.SetParent(starsParent.transform, false);

            // Random position on the dome interior
            Vector3 dir = Random.onUnitSphere;
            starGO.transform.localPosition = dir * (domeRadius * 0.95f);
            // Face the center
            starGO.transform.LookAt(transform.position);

            float size = Random.Range(starMinSize, starMaxSize);
            starGO.transform.localScale = Vector3.one * size;

            var mf = starGO.AddComponent<MeshFilter>();
            var mr = starGO.AddComponent<MeshRenderer>();
            mf.sharedMesh = quadMesh;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Color distribution: 75% white, 25% blue (matches LaserRoom palette)
            float roll = Random.value;
            if (roll < 0.25f)
                mr.sharedMaterial = starMaterialBlue;
            else
                mr.sharedMaterial = starMaterialWhite;
        }
    }

    Material CreateStarMaterial(Color baseColor, float emissionIntensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        var mat = new Material(shader) { hideFlags = HideFlags.DontSave };
        Color bright = baseColor * emissionIntensity;
        mat.SetColor("_BaseColor", bright);
        mat.SetColor("_Color", bright);
        mat.renderQueue = (int)RenderQueue.Background + 1;
        return mat;
    }

    Mesh CreateInvertedSphereMesh(float radius, int longitudeSegments, int latitudeSegments)
    {
        var mesh = new Mesh();
        mesh.name = "InvertedSphere";
        mesh.hideFlags = HideFlags.DontSave;

        int vertCount = (longitudeSegments + 1) * (latitudeSegments + 1);
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        int idx = 0;
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float theta = Mathf.PI * lat / latitudeSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longitudeSegments;
                float x = sinTheta * Mathf.Cos(phi);
                float y = cosTheta;
                float z = sinTheta * Mathf.Sin(phi);

                vertices[idx] = new Vector3(x, y, z) * radius;
                normals[idx] = -new Vector3(x, y, z); // Inward-facing
                uvs[idx] = new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments);
                idx++;
            }
        }

        // Triangles — wound in reverse for inside-facing
        int triCount = longitudeSegments * latitudeSegments * 6;
        var triangles = new int[triCount];
        int ti = 0;
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int current = lat * (longitudeSegments + 1) + lon;
                int next = current + longitudeSegments + 1;

                // Reversed winding
                triangles[ti++] = current;
                triangles[ti++] = current + 1;
                triangles[ti++] = next;

                triangles[ti++] = next;
                triangles[ti++] = current + 1;
                triangles[ti++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        return mesh;
    }

    Mesh CreateQuadMesh()
    {
        var mesh = new Mesh();
        mesh.name = "StarQuad";
        mesh.hideFlags = HideFlags.DontSave;

        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1)
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }

    void CreateMazeVisualPolish()
    {
        ClearVisualPolish();
        CreatePolishMaterials();

        visualRoot = new GameObject("Runtime_MazeVisualPolish");
        visualRoot.hideFlags = GeneratedHideFlags;
        visualRoot.transform.SetParent(transform, false);

        BuildFloorDeck();
        BuildWallShells();
        BuildMazeSetPieces();
        TuneMazeLighting();
    }

    void ClearVisualPolish()
    {
        var existing = GameObject.Find("Runtime_MazeVisualPolish");
        if (existing != null)
            DestroyGenerated(existing);
    }

    void CreatePolishMaterials()
    {
        deckMaterial = CreateLitMaterial("Maze deck gunmetal", new Color(0.12f, 0.13f, 0.13f), Color.black, 0.65f, 0.42f);
        wallMaterial = CreateLitMaterial("Maze dark ship wall", new Color(0.075f, 0.08f, 0.082f), Color.black, 0.58f, 0.36f);
        trimMaterial = CreateLitMaterial("Maze black mechanical trim", new Color(0.018f, 0.019f, 0.02f), Color.black, 0.78f, 0.35f);
        frameMaterial = CreateLitMaterial("Maze warm armored frame", new Color(0.48f, 0.43f, 0.34f), new Color(0.018f, 0.013f, 0.004f), 0.35f, 0.45f);
        cyanLightMaterial = CreateLitMaterial("Maze cyan light strip", new Color(0.02f, 0.17f, 0.2f), new Color(0f, 1.05f, 1.55f), 0.08f, 0.7f);
        amberLightMaterial = CreateLitMaterial("Maze amber light strip", new Color(0.28f, 0.16f, 0.035f), new Color(1.9f, 0.78f, 0.09f), 0.08f, 0.66f);
        greenLightMaterial = CreateLitMaterial("Maze exit green light", new Color(0.035f, 0.2f, 0.08f), new Color(0.05f, 1.35f, 0.32f), 0.08f, 0.66f);
        darkGlassMaterial = CreateLitMaterial("Maze dark blue glass", new Color(0.025f, 0.09f, 0.12f, 0.7f), new Color(0f, 0.28f, 0.42f), 0.0f, 0.82f);
    }

    Material CreateLitMaterial(string materialName, Color baseColor, Color emission, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = materialName, hideFlags = HideFlags.DontSave };
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color", baseColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetColor("_EmissionColor", emission);
        if (emission.maxColorComponent > 0f)
            mat.EnableKeyword("_EMISSION");
        return mat;
    }

    void BuildFloorDeck()
    {
        var ground = GameObject.Find("Ground");
        if (ground == null)
            return;

        HideRenderer(ground);
        Vector3 center = ground.transform.position + Vector3.up * 0.085f;
        Vector3 size = ground.transform.localScale;

        CreateBox("Deck_MainPlating", center, new Vector3(size.x, 0.08f, size.z + 1f), deckMaterial);
        CreateBox("Deck_Understructure", ground.transform.position + Vector3.down * 0.1f, new Vector3(size.x + 0.4f, 0.18f, size.z + 1.4f), trimMaterial);

        for (int x = 0; x < 6; x++)
        {
            for (int z = 0; z < 6; z++)
            {
                Vector3 panelCenter = new Vector3(1.2f + x * 2.55f, center.y + 0.04f, 1.2f + z * 2.55f);
                CreateBox($"Deck_Panel_{x}_{z}", panelCenter, new Vector3(2.15f, 0.035f, 2.15f), x == z ? wallMaterial : trimMaterial);
                CreateBox($"Deck_PanelLip_N_{x}_{z}", panelCenter + new Vector3(0f, 0.04f, 1.13f), new Vector3(1.95f, 0.025f, 0.055f), frameMaterial);
                CreateBox($"Deck_PanelLip_E_{x}_{z}", panelCenter + new Vector3(1.13f, 0.04f, 0f), new Vector3(0.055f, 0.025f, 1.95f), frameMaterial);
            }
        }

    }

    void CreateFloorGuide(string name, Vector3 center, bool alongX, float length, Material mat)
    {
        int segments = Mathf.Max(3, Mathf.RoundToInt(length / 1.6f));
        for (int i = 0; i < segments; i++)
        {
            float offset = -length * 0.5f + i * (length / Mathf.Max(1, segments - 1));
            Vector3 pos = center + (alongX ? Vector3.right : Vector3.forward) * offset;
            Vector3 size = alongX ? new Vector3(0.75f, 0.025f, 0.07f) : new Vector3(0.07f, 0.025f, 0.75f);
            CreateBox($"{name}_{i}", pos, size, mat);
        }
    }

    void BuildWallShells()
    {
        foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            GameObject wall = renderer.gameObject;
            if (!IsMazeWall(wall.name))
                continue;

            HideRenderer(wall);
            BuildWallModule(wall);
        }
    }

    bool IsMazeWall(string objectName)
    {
        return objectName.StartsWith("Wall") || objectName.StartsWith("InnerH") || objectName.StartsWith("InnerV");
    }

    void BuildWallModule(GameObject source)
    {
        Vector3 pos = source.transform.position;
        Vector3 scale = source.transform.localScale;
        bool horizontal = scale.x >= scale.z;
        float length = horizontal ? scale.x : scale.z;
        Vector3 coreSize = horizontal
            ? new Vector3(length, 1.75f, 0.34f)
            : new Vector3(0.34f, 1.75f, length);

        CreateWallBox($"{source.name}_ArmoredCore", pos + Vector3.up * 0.03f, coreSize, wallMaterial, horizontal);
        CreateWallBox($"{source.name}_TopCap", pos + Vector3.up * 0.93f, horizontal ? new Vector3(length + 0.18f, 0.18f, 0.5f) : new Vector3(0.5f, 0.18f, length + 0.18f), frameMaterial, horizontal);
        CreateWallBox($"{source.name}_BaseRail", pos + Vector3.down * 0.78f, horizontal ? new Vector3(length + 0.1f, 0.18f, 0.46f) : new Vector3(0.46f, 0.18f, length + 0.1f), trimMaterial, horizontal);

        int bays = Mathf.Max(1, Mathf.RoundToInt(length / 2.2f));
        for (int i = 0; i < bays; i++)
        {
            float offset = -length * 0.5f + (i + 0.5f) * (length / bays);
            Vector3 bayPos = pos + (horizontal ? Vector3.right : Vector3.forward) * offset;
            CreateWallBay(source.name, bayPos, horizontal, i);
        }
    }

    void CreateWallBay(string prefix, Vector3 center, bool horizontal, int index)
    {
        Vector3 faceOffset = horizontal ? Vector3.forward * 0.19f : Vector3.right * 0.19f;
        Vector3 panelSize = horizontal ? new Vector3(1.25f, 0.82f, 0.055f) : new Vector3(0.055f, 0.82f, 1.25f);
        Vector3 trimSizeA = horizontal ? new Vector3(1.42f, 0.065f, 0.07f) : new Vector3(0.07f, 0.065f, 1.42f);
        Vector3 trimSizeB = horizontal ? new Vector3(0.065f, 0.82f, 0.07f) : new Vector3(0.07f, 0.82f, 0.065f);
        Vector3 tangent = horizontal ? Vector3.right : Vector3.forward;

        CreateWallBox($"{prefix}_InsetPanel_{index}", center + faceOffset, panelSize, index % 2 == 0 ? trimMaterial : wallMaterial, horizontal);
        CreateWallBox($"{prefix}_PanelTop_{index}", center + faceOffset + Vector3.up * 0.47f, trimSizeA, frameMaterial, horizontal);
        CreateWallBox($"{prefix}_PanelBottom_{index}", center + faceOffset + Vector3.down * 0.47f, trimSizeA, frameMaterial, horizontal);
        CreateWallBox($"{prefix}_PanelLeft_{index}", center + faceOffset - tangent * 0.72f, trimSizeB, frameMaterial, horizontal);
        CreateWallBox($"{prefix}_PanelRight_{index}", center + faceOffset + tangent * 0.72f, trimSizeB, frameMaterial, horizontal);

        Material lightMat = index % 3 == 0 ? cyanLightMaterial : amberLightMaterial;
        Vector3 lightSize = horizontal ? new Vector3(0.55f, 0.045f, 0.075f) : new Vector3(0.075f, 0.045f, 0.55f);
        CreateWallBox($"{prefix}_DataLight_{index}", center + faceOffset + Vector3.up * 0.16f, lightSize, lightMat, horizontal);
    }

    void BuildMazeSetPieces()
    {
        CreateExitGate();
        CreateKeyPedestals();
        CreateCornerProps();
    }

    void CreateExitGate()
    {
        var exit = GameObject.Find("Exit");
        if (exit == null)
            return;

        HideRenderer(exit);
        Vector3 p = exit.transform.position;
        CreateBox("Exit_DockingPad", p + Vector3.up * 0.02f, new Vector3(2.9f, 0.12f, 1.25f), trimMaterial);
        CreateBox("Exit_GreenPad", p + Vector3.up * 0.11f, new Vector3(2.25f, 0.045f, 0.88f), greenLightMaterial);
        CreateBox("Exit_LeftPylon", p + new Vector3(-1.45f, 0.85f, 0f), new Vector3(0.28f, 1.75f, 0.42f), frameMaterial);
        CreateBox("Exit_RightPylon", p + new Vector3(1.45f, 0.85f, 0f), new Vector3(0.28f, 1.75f, 0.42f), frameMaterial);
        CreateBox("Exit_Header", p + new Vector3(0f, 1.72f, 0f), new Vector3(3.15f, 0.22f, 0.46f), frameMaterial);
        CreateBox("Exit_StatusStrip", p + new Vector3(0f, 1.43f, -0.22f), new Vector3(2.2f, 0.08f, 0.07f), greenLightMaterial);
        CreateBox("Exit_HoloPanel", p + new Vector3(0f, 0.95f, -0.32f), new Vector3(1.25f, 0.72f, 0.045f), darkGlassMaterial);
        CreatePointLight("Exit_GreenGlow", p + new Vector3(0f, 1.1f, -0.3f), new Color(0.12f, 1f, 0.45f), 2.0f, 4.5f);
    }

    void CreateKeyPedestals()
    {
        for (int i = 1; i <= 3; i++)
        {
            var key = GameObject.Find($"Key{i}");
            if (key == null)
                continue;

            Vector3 p = key.transform.position;
            CreateBox($"Key{i}_PedestalBase", new Vector3(p.x, 0.13f, p.z), new Vector3(1.05f, 0.24f, 1.05f), trimMaterial);
            CreateBox($"Key{i}_PedestalGlow", new Vector3(p.x, 0.28f, p.z), new Vector3(0.72f, 0.06f, 0.72f), amberLightMaterial);
            CreatePointLight($"Key{i}_Beacon", p + Vector3.up * 1.3f, new Color(1f, 0.68f, 0.12f), 1.2f, 3.2f);
            if (!Application.isPlaying && CreateEditModeKeyPreview(key, $"Key{i}_EditPreview"))
                HideRenderersInChildren(key);
        }
    }

    bool CreateEditModeKeyPreview(GameObject keyObject, string name)
    {
        GameObject importedKeyPrefab = Resources.Load<GameObject>(ImportedKeyResourcePath);
        if (importedKeyPrefab == null)
            return false;

        GameObject preview = Instantiate(importedKeyPrefab);
        preview.name = name;
        SetGeneratedFlagsRecursive(preview);
        preview.transform.SetParent(visualRoot.transform, false);
        preview.transform.position = keyObject.transform.position;
        preview.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        preview.transform.localScale = Vector3.one;
        ApplyEditKeyMaterial(preview);
        FitEditModeKeyPreview(keyObject, preview);
        return true;
    }

    void ApplyEditKeyMaterial(GameObject preview)
    {
        foreach (Renderer keyRenderer in preview.GetComponentsInChildren<Renderer>(true))
            keyRenderer.sharedMaterial = GetEditKeyMaterial();
    }

    Material GetEditKeyMaterial()
    {
        if (editKeyMaterial != null)
            return editKeyMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        editKeyMaterial = new Material(shader) { name = "Maze edit preview gold key", hideFlags = HideFlags.DontSave };
        editKeyMaterial.SetColor("_BaseColor", new Color(1f, 0.78f, 0.08f, 1f));
        editKeyMaterial.SetColor("_Color", new Color(1f, 0.78f, 0.08f, 1f));
        editKeyMaterial.SetColor("_EmissionColor", new Color(0.85f, 0.45f, 0.02f, 1f));
        editKeyMaterial.EnableKeyword("_EMISSION");
        if (editKeyMaterial.HasProperty("_Metallic"))
            editKeyMaterial.SetFloat("_Metallic", 0.85f);
        if (editKeyMaterial.HasProperty("_Smoothness"))
            editKeyMaterial.SetFloat("_Smoothness", 0.68f);
        return editKeyMaterial;
    }

    void FitEditModeKeyPreview(GameObject keyObject, GameObject preview)
    {
        if (!TryGetRendererBounds(preview, out Bounds bounds))
            return;

        if (bounds.size.y > 0.001f)
        {
            float scale = ImportedKeyTargetHeight / bounds.size.y;
            preview.transform.localScale *= scale;
        }

        if (!TryGetRendererBounds(preview, out bounds))
            return;

        Collider keyCollider = keyObject.GetComponent<Collider>();
        float groundY = keyCollider != null
            ? keyCollider.bounds.min.y
            : keyObject.transform.position.y - 0.5f;
        float lift = groundY + ImportedKeyGroundClearance - bounds.min.y;
        preview.transform.position += Vector3.up * lift;
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

    void CreateCornerProps()
    {
        CreateCrateStack("CargoStack_SW", new Vector3(1.2f, 0.28f, 13.7f), 8f);
        CreateCrateStack("CargoStack_NE", new Vector3(13.7f, 0.28f, 1.2f), -12f);
        CreateServerColumn("ServerColumn_NW", new Vector3(1.1f, 0.85f, 1.1f), -90f);
    }

    void CreateCrateStack(string prefix, Vector3 pos, float yaw)
    {
        for (int i = 0; i < 3; i++)
        {
            var crate = CreateBox($"{prefix}_Crate_{i}", pos + Vector3.up * (i * 0.42f), new Vector3(0.9f, 0.36f, 0.72f), i == 1 ? wallMaterial : trimMaterial);
            crate.transform.rotation = Quaternion.Euler(0f, yaw + i * 6f, 0f);
            CreateBox($"{prefix}_Latch_{i}", pos + new Vector3(0f, i * 0.42f + 0.02f, -0.38f), new Vector3(0.48f, 0.055f, 0.055f), amberLightMaterial).transform.rotation = crate.transform.rotation;
        }
    }

    void CreateServerColumn(string prefix, Vector3 pos, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        CreateBox($"{prefix}_Body", pos, new Vector3(0.72f, 1.55f, 0.72f), trimMaterial).transform.rotation = rotation;
        for (int i = 0; i < 5; i++)
            CreateBox($"{prefix}_Status_{i}", pos + rotation * new Vector3(0f, -0.55f + i * 0.26f, -0.38f), new Vector3(0.48f, 0.045f, 0.055f), i % 2 == 0 ? cyanLightMaterial : greenLightMaterial).transform.rotation = rotation;
    }

    void TuneMazeLighting()
    {
        var sun = GameObject.Find("Directional Light");
        if (sun != null && sun.TryGetComponent(out Light light))
        {
            light.color = new Color(0.62f, 0.74f, 1f);
            light.intensity = 0.72f;
        }

        CreatePointLight("Maze_CyanWash", new Vector3(4.2f, 3.1f, 4.2f), new Color(0.16f, 0.7f, 1f), 1.4f, 7f);
        CreatePointLight("Maze_AmberWash", new Vector3(11.5f, 3.0f, 10.5f), new Color(1f, 0.62f, 0.18f), 1.1f, 6f);
    }

    GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.hideFlags = GeneratedHideFlags;
        go.transform.SetParent(visualRoot.transform, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateChamferedBoxMesh();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        return go;
    }

    GameObject CreateWallBox(string name, Vector3 position, Vector3 worldScale, Material mat, bool horizontal)
    {
        if (horizontal)
            return CreateBox(name, position, worldScale, mat);

        var go = CreateBox(name, position, new Vector3(worldScale.z, worldScale.y, worldScale.x), mat);
        go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        return go;
    }

    GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material mat)
    {
        var go = new GameObject(name);
        go.hideFlags = GeneratedHideFlags;
        go.transform.SetParent(visualRoot.transform, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateDecagonalPrismMesh();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        return go;
    }

    void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.hideFlags = GeneratedHideFlags;
        go.transform.SetParent(visualRoot.transform, false);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    void HideRenderer(GameObject go)
    {
        if (go != null && go.TryGetComponent(out Renderer renderer))
            renderer.enabled = false;
    }

    void HideRenderersInChildren(GameObject go)
    {
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    void SetGeneratedFlagsRecursive(GameObject go)
    {
        go.hideFlags = GeneratedHideFlags;
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = GeneratedHideFlags;
    }

    Mesh CreateChamferedBoxMesh()
    {
        float u = 0.5f;
        float v = 0.5f;
        float e = 0.5f;
        float c = 0.09f;
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
            vertices[i] = new Vector3(outline[i].x, outline[i].y, e);
            vertices[i + 8] = new Vector3(outline[i].x, outline[i].y, -e);
        }
        vertices[16] = new Vector3(0f, 0f, e);
        vertices[17] = new Vector3(0f, 0f, -e);

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

        var mesh = new Mesh { name = "Maze chamfered hard surface box", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Mesh CreateDecagonalPrismMesh()
    {
        const int sides = 10;
        var vertices = new Vector3[sides * 2 + 2];
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides;
            vertices[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0.5f, Mathf.Sin(a) * 0.5f);
            vertices[i + sides] = new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f);
        }
        vertices[sides * 2] = new Vector3(0f, 0.5f, 0f);
        vertices[sides * 2 + 1] = new Vector3(0f, -0.5f, 0f);

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

        var mesh = new Mesh { name = "Maze decagonal hard surface cylinder", hideFlags = HideFlags.DontSave };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void DestroyGenerated(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
