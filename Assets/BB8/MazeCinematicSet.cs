using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeCinematicSet
{
    public const int CinematicLayer = 31;

    readonly List<Material> materials = new List<Material>();
    readonly Transform root;
    readonly Color accent;
    Camera cinematicCamera;
    Scene cinematicScene;
    bool ownsScene;

    public Transform Root => root;

    public MazeCinematicSet(string name, Color accentColor, Transform parent = null, bool createCamera = false)
    {
        accent = accentColor;
        if (Application.isPlaying)
        {
            cinematicScene = SceneManager.CreateScene($"{name}_Scene");
            ownsScene = true;
        }

        root = new GameObject(name).transform;
        MoveToCinematicScene(root.gameObject);
        root.SetParent(parent, false);
        Build();
        if (Application.isPlaying)
            root.gameObject.AddComponent<MazeCinematicAnimator>();
        SetLayerRecursive(root.gameObject, CinematicLayer);
        if (createCamera)
            BuildCamera();
    }

    public void Dispose()
    {
        if (root != null)
        {
            if (Application.isPlaying) Object.Destroy(root.gameObject);
            else Object.DestroyImmediate(root.gameObject);
        }

        if (cinematicCamera != null)
        {
            if (Application.isPlaying) Object.Destroy(cinematicCamera.gameObject);
            else Object.DestroyImmediate(cinematicCamera.gameObject);
        }

        foreach (Material material in materials)
        {
            if (material == null) continue;
            if (Application.isPlaying) Object.Destroy(material);
            else Object.DestroyImmediate(material);
        }

        if (ownsScene && cinematicScene.IsValid() && cinematicScene.isLoaded)
            SceneManager.UnloadSceneAsync(cinematicScene);
    }

    void Build()
    {
        var darkHull = MakeMat("Cine_DarkHull", new Color(0.045f, 0.048f, 0.052f), new Color(0.002f, 0.003f, 0.004f), 0.78f, 0.55f);
        var midHull = MakeMat("Cine_MidHull", new Color(0.18f, 0.19f, 0.18f), new Color(0.012f, 0.011f, 0.009f), 0.82f, 0.65f);
        var warmTrim = MakeMat("Cine_WarmTrim", new Color(0.52f, 0.43f, 0.28f), new Color(0.04f, 0.026f, 0.008f), 0.86f, 0.7f);
        var cyan = MakeMat("Cine_CyanGlow", new Color(0.06f, 0.78f, 1f), new Color(0f, 2.8f, 4f), 0.92f, 0.2f);
        var amber = MakeMat("Cine_AmberGlow", new Color(1f, 0.64f, 0.2f), new Color(2.6f, 1.35f, 0.28f), 0.9f, 0.2f);
        var glass = MakeMat("Cine_Glass", new Color(0.035f, 0.17f, 0.22f, 0.64f), new Color(0f, 0.55f, 0.75f), 0.96f, 0.05f);
        var planetMat = MakeMat("Cine_Planet", new Color(0.035f, 0.15f, 0.24f), new Color(0f, 0.18f, 0.32f), 0.55f, 0.2f);
        var starMat = MakeMat("Cine_Star", new Color(0.7f, 0.9f, 1f), new Color(0.9f, 1.2f, 1.6f), 0.5f, 0f);

        BuildCorridor(darkHull, midHull, warmTrim, cyan, amber);
        BuildWindowScene(planetMat, starMat, cyan);
        BuildReactor(midHull, warmTrim, cyan, glass);
        BuildShip(darkHull, midHull, warmTrim, cyan, glass);
        BuildLights(cyan, amber);
    }

    void BuildCamera()
    {
        cinematicCamera = new GameObject("MazeCinematicCamera").AddComponent<Camera>();
        MoveToCinematicScene(cinematicCamera.gameObject);
        cinematicCamera.clearFlags = CameraClearFlags.SolidColor;
        cinematicCamera.backgroundColor = new Color(0.002f, 0.004f, 0.01f);
        cinematicCamera.cullingMask = 1 << CinematicLayer;
        cinematicCamera.depth = 80f;
        cinematicCamera.fieldOfView = 46f;
        cinematicCamera.nearClipPlane = 0.03f;
        cinematicCamera.farClipPlane = 120f;
        cinematicCamera.transform.position = new Vector3(0f, 0.68f, -8.6f);
        cinematicCamera.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);
    }

    void BuildCorridor(Material darkHull, Material midHull, Material warmTrim, Material cyan, Material amber)
    {
        AddBox("DeckBase", new Vector3(0f, -1.05f, 1.6f), new Vector3(7.6f, 0.14f, 9.2f), darkHull);
        AddBox("CeilingBase", new Vector3(0f, 2.65f, 1.6f), new Vector3(7.6f, 0.14f, 9.2f), darkHull);

        for (int i = 0; i < 8; i++)
        {
            float z = -3.2f + i * 1.08f;
            AddBox($"DeckPanel_{i}", new Vector3(0f, -0.94f, z), new Vector3(4.8f, 0.045f, 0.76f), i % 2 == 0 ? midHull : darkHull);
            AddBox($"DeckInset_{i}", new Vector3(0f, -0.885f, z), new Vector3(1.6f, 0.028f, 0.58f), darkHull);
            AddBox($"DeckCyanL_{i}", new Vector3(-1.82f, -0.84f, z), new Vector3(0.045f, 0.022f, 0.58f), cyan);
            AddBox($"DeckCyanR_{i}", new Vector3(1.82f, -0.84f, z), new Vector3(0.045f, 0.022f, 0.58f), cyan);
            AddBox($"DeckAmber_{i}", new Vector3(0f, -0.82f, z + 0.33f), new Vector3(0.72f, 0.018f, 0.04f), amber);
        }

        for (int i = 0; i < 7; i++)
        {
            float z = -3.35f + i * 1.2f;
            Transform leftRib = AddBox($"LeftAngledRib_{i}", new Vector3(-3.0f, 0.64f, z), new Vector3(0.24f, 2.9f, 0.24f), warmTrim);
            Transform rightRib = AddBox($"RightAngledRib_{i}", new Vector3(3.0f, 0.64f, z), new Vector3(0.24f, 2.9f, 0.24f), warmTrim);
            leftRib.localRotation = Quaternion.Euler(0f, 0f, -13f);
            rightRib.localRotation = Quaternion.Euler(0f, 0f, 13f);

            AddBox($"LeftWallPanel_{i}", new Vector3(-3.55f, 0.68f, z), new Vector3(0.12f, 1.38f, 0.75f), darkHull);
            AddBox($"RightWallPanel_{i}", new Vector3(3.55f, 0.68f, z), new Vector3(0.12f, 1.38f, 0.75f), darkHull);
            AddCylinder($"PipeTopA_{i}", new Vector3(-2.25f, 2.42f, z), new Vector3(0.07f, 0.72f, 0.07f), midHull, Quaternion.Euler(90f, 0f, 0f));
            AddCylinder($"PipeTopB_{i}", new Vector3(2.25f, 2.42f, z), new Vector3(0.07f, 0.72f, 0.07f), midHull, Quaternion.Euler(90f, 0f, 0f));
            AddBox($"OverheadLight_{i}", new Vector3(0f, 2.42f, z), new Vector3(1.95f, 0.045f, 0.09f), i % 2 == 0 ? cyan : amber);
            AddBox($"WallStripL_{i}", new Vector3(-3.62f, 0.92f, z), new Vector3(0.025f, 0.08f, 0.46f), i % 2 == 0 ? amber : cyan);
            AddBox($"WallStripR_{i}", new Vector3(3.62f, 0.92f, z), new Vector3(0.025f, 0.08f, 0.46f), i % 2 == 0 ? amber : cyan);
        }

        AddMesh("AirlockOuterRing", MakeTorusMesh(2.35f, 0.08f, 128, 12), new Vector3(0f, 0.72f, 4.6f), new Vector3(1f, 0.58f, 1f), warmTrim);
        AddMesh("AirlockInnerGlow", MakeTorusMesh(1.88f, 0.035f, 128, 8), new Vector3(0f, 0.72f, 4.52f), new Vector3(1f, 0.58f, 1f), cyan);
    }

    void BuildWindowScene(Material planetMat, Material starMat, Material cyan)
    {
        Transform planet = AddPrimitive("DistantPlanet", PrimitiveType.Sphere, new Vector3(-2.85f, 0.45f, 7.2f), new Vector3(1.8f, 1.8f, 1.8f), planetMat);
        planet.localRotation = Quaternion.Euler(0f, -22f, 0f);

        for (int i = 0; i < 95; i++)
        {
            float x = Mathf.Lerp(-5.2f, 5.2f, Hash01(i * 14.77f));
            float y = Mathf.Lerp(-0.8f, 3.0f, Hash01(i * 7.31f));
            float z = Mathf.Lerp(5.6f, 9.5f, Hash01(i * 21.9f));
            float s = Mathf.Lerp(0.015f, 0.06f, Hash01(i * 4.2f));
            AddPrimitive($"Star_{i}", PrimitiveType.Sphere, new Vector3(x, y, z), Vector3.one * s, starMat);
        }
    }

    void BuildReactor(Material midHull, Material warmTrim, Material cyan, Material glass)
    {
        AddCylinder("ReactorPedestal", new Vector3(0f, -0.55f, 0.35f), new Vector3(0.92f, 0.38f, 0.92f), midHull, Quaternion.identity);
        AddMesh("ReactorOuterTrim", MakeTorusMesh(0.94f, 0.045f, 96, 8), new Vector3(0f, -0.31f, 0.35f), Vector3.one, warmTrim);
        AddCylinder("ReactorGlassColumn", new Vector3(0f, 0.22f, 0.35f), new Vector3(0.34f, 1.08f, 0.34f), glass, Quaternion.identity);
        AddPrimitive("ReactorEnergyCore", PrimitiveType.Sphere, new Vector3(0f, 0.35f, 0.35f), Vector3.one * 0.32f, cyan);
        AddMesh("ReactorEnergyRingA", MakeTorusMesh(0.48f, 0.018f, 96, 8), new Vector3(0f, 0.35f, 0.35f), Vector3.one, cyan);
        AddMesh("ReactorEnergyRingB", MakeTorusMesh(0.36f, 0.014f, 96, 8), new Vector3(0f, 0.35f, 0.35f), new Vector3(1f, 0.62f, 1f), cyan).localRotation = Quaternion.Euler(70f, 0f, 0f);
    }

    void BuildShip(Material darkHull, Material midHull, Material warmTrim, Material cyan, Material glass)
    {
        var ship = new GameObject("ApproachingShip").transform;
        ship.SetParent(root, false);
        ship.localPosition = new Vector3(-3.1f, 0.58f, 6.9f);
        ship.localRotation = Quaternion.Euler(-3f, 12f, 0f);
        ship.localScale = Vector3.one * 0.86f;

        AddBox("ShipBody", new Vector3(0f, 0f, 0f), new Vector3(0.62f, 0.26f, 1.05f), midHull, ship);
        AddBox("ShipNose", new Vector3(0f, 0.03f, 0.64f), new Vector3(0.36f, 0.2f, 0.34f), warmTrim, ship);
        AddBox("ShipLeftWing", new Vector3(-0.56f, -0.04f, -0.02f), new Vector3(0.78f, 0.08f, 0.42f), darkHull, ship);
        AddBox("ShipRightWing", new Vector3(0.56f, -0.04f, -0.02f), new Vector3(0.78f, 0.08f, 0.42f), darkHull, ship);
        AddPrimitive("ShipCanopy", PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0.12f), new Vector3(0.34f, 0.16f, 0.28f), glass, ship);
        AddPrimitive("ShipEngineL", PrimitiveType.Cylinder, new Vector3(-0.23f, -0.04f, -0.58f), new Vector3(0.12f, 0.22f, 0.12f), cyan, ship).localRotation = Quaternion.Euler(90f, 0f, 0f);
        AddPrimitive("ShipEngineR", PrimitiveType.Cylinder, new Vector3(0.23f, -0.04f, -0.58f), new Vector3(0.12f, 0.22f, 0.12f), cyan, ship).localRotation = Quaternion.Euler(90f, 0f, 0f);
        AddBox("ShipNoseTrim", new Vector3(0f, 0.16f, 0.42f), new Vector3(0.16f, 0.035f, 0.32f), cyan, ship);
        AddBox("ShipEngineTrailL", new Vector3(-0.23f, -0.04f, -1.05f), new Vector3(0.09f, 0.05f, 0.68f), cyan, ship);
        AddBox("ShipEngineTrailR", new Vector3(0.23f, -0.04f, -1.05f), new Vector3(0.09f, 0.05f, 0.68f), cyan, ship);
    }

    void BuildLights(Material cyan, Material amber)
    {
        AddLight("KeyCyan", new Vector3(-1.8f, 2.1f, -1.5f), new Color(0.35f, 0.85f, 1f), 2.4f, 6f);
        AddLight("WarmSide", new Vector3(2.6f, 1.1f, -0.5f), new Color(1f, 0.58f, 0.24f), 1.8f, 5f);
        AddLight("ReactorLight", new Vector3(0f, 0.55f, 0.35f), accent, 3.2f, 4.2f);
    }

    Transform AddBox(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null)
        => AddPrimitive(name, PrimitiveType.Cube, pos, scale, mat, parent);

    Transform AddCylinder(string name, Vector3 pos, Vector3 scale, Material mat, Quaternion rotation, Transform parent = null)
    {
        Transform t = AddPrimitive(name, PrimitiveType.Cylinder, pos, scale, mat, parent);
        t.localRotation = rotation;
        return t;
    }

    Transform AddPrimitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(type);
        MoveToCinematicScene(go);
        go.name = name;
        go.transform.SetParent(parent != null ? parent : root, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }
        go.GetComponent<Renderer>().sharedMaterial = mat;
        SetLayerRecursive(go, CinematicLayer);
        return go.transform;
    }

    Transform AddMesh(string name, Mesh mesh, Vector3 pos, Vector3 scale, Material mat, Transform parent = null)
    {
        var go = new GameObject(name);
        MoveToCinematicScene(go);
        go.transform.SetParent(parent != null ? parent : root, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        SetLayerRecursive(go, CinematicLayer);
        return go.transform;
    }

    void AddLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        MoveToCinematicScene(go);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        SetLayerRecursive(go, CinematicLayer);
    }

    Material MakeMat(string name, Color baseColor, Color emission, float smoothness, float metallic)
    {
        Shader shader = FindSupportedShader();
        var mat = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", emission);
            mat.EnableKeyword("_EMISSION");
        }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        materials.Add(mat);
        return mat;
    }

    Shader FindSupportedShader()
    {
        string[] candidates =
        {
            "Universal Render Pipeline/Lit",
            "Standard",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Sprites/Default"
        };

        foreach (string candidate in candidates)
        {
            Shader shader = Shader.Find(candidate);
            if (shader != null && shader.isSupported)
                return shader;
        }

        return Shader.Find("Standard");
    }

    void MoveToCinematicScene(GameObject go)
    {
        if (ownsScene && cinematicScene.IsValid())
            SceneManager.MoveGameObjectToScene(go, cinematicScene);
    }

    Mesh MakeTorusMesh(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
    {
        var mesh = new Mesh();
        Vector3[] vertices = new Vector3[majorSegments * minorSegments];
        int[] triangles = new int[majorSegments * minorSegments * 6];
        for (int i = 0; i < majorSegments; i++)
        {
            float u = i / (float)majorSegments * Mathf.PI * 2f;
            Vector3 ringCenter = new Vector3(Mathf.Cos(u) * majorRadius, Mathf.Sin(u) * majorRadius, 0f);
            Vector3 radial = ringCenter.normalized;
            for (int j = 0; j < minorSegments; j++)
            {
                float v = j / (float)minorSegments * Mathf.PI * 2f;
                vertices[i * minorSegments + j] = ringCenter + radial * (Mathf.Cos(v) * minorRadius) + Vector3.forward * (Mathf.Sin(v) * minorRadius);
            }
        }
        int index = 0;
        for (int i = 0; i < majorSegments; i++)
        {
            int nextI = (i + 1) % majorSegments;
            for (int j = 0; j < minorSegments; j++)
            {
                int nextJ = (j + 1) % minorSegments;
                int a = i * minorSegments + j;
                int b = nextI * minorSegments + j;
                int c = nextI * minorSegments + nextJ;
                int d = i * minorSegments + nextJ;
                triangles[index++] = a; triangles[index++] = b; triangles[index++] = c;
                triangles[index++] = a; triangles[index++] = c; triangles[index++] = d;
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    static float Hash01(float value) => Mathf.Repeat(Mathf.Sin(value * 12.9898f) * 43758.5453f, 1f);

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}

public class MazeCinematicAnimator : MonoBehaviour
{
    Transform reactorRingA;
    Transform reactorRingB;
    Transform airlockGlow;
    Transform planet;
    Transform ship;
    Vector3 shipStartPosition;
    Quaternion shipStartRotation;
    Light[] lights;
    float[] baseIntensities;
    float startTime;

    void Awake()
    {
        reactorRingA = transform.Find("ReactorEnergyRingA");
        reactorRingB = transform.Find("ReactorEnergyRingB");
        airlockGlow = transform.Find("AirlockInnerGlow");
        planet = transform.Find("DistantPlanet");
        ship = transform.Find("ApproachingShip");

        if (ship != null)
        {
            shipStartPosition = ship.localPosition;
            shipStartRotation = ship.localRotation;
        }

        lights = GetComponentsInChildren<Light>(true);
        baseIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            baseIntensities[i] = lights[i].intensity;

        startTime = Time.realtimeSinceStartup;
    }

    void Update()
    {
        float delta = Application.isPlaying ? Time.unscaledDeltaTime : 0.016f;
        float t = Time.realtimeSinceStartup - startTime;

        if (reactorRingA != null)
            reactorRingA.Rotate(Vector3.forward, 42f * delta, Space.Self);
        if (reactorRingB != null)
            reactorRingB.Rotate(Vector3.right, -58f * delta, Space.Self);
        if (airlockGlow != null)
            airlockGlow.Rotate(Vector3.forward, 12f * delta, Space.Self);
        if (planet != null)
            planet.Rotate(Vector3.up, 2.8f * delta, Space.Self);

        if (ship != null)
        {
            float moveDuration = 3.65f;
            float holdDuration = 1.05f;
            float cycle = moveDuration * 2f + holdDuration * 2f;
            float phase = Mathf.Repeat(t, cycle);
            float pass;
            float direction;

            if (phase < moveDuration)
            {
                pass = Mathf.SmoothStep(0f, 1f, phase / moveDuration);
                direction = 1f;
            }
            else if (phase < moveDuration + holdDuration)
            {
                pass = 1f;
                direction = 1f;
            }
            else if (phase < moveDuration * 2f + holdDuration)
            {
                pass = Mathf.SmoothStep(1f, 0f, (phase - moveDuration - holdDuration) / moveDuration);
                direction = -1f;
            }
            else
            {
                pass = 0f;
                direction = -1f;
            }

            float sweep = Mathf.Lerp(-4.8f, 4.8f, pass);
            float depth = Mathf.Sin(pass * Mathf.PI) * -1.35f;
            ship.localPosition = new Vector3(
                sweep,
                shipStartPosition.y + Mathf.Sin(t * 2.6f) * 0.16f,
                shipStartPosition.z + depth);
            ship.localRotation = shipStartRotation * Quaternion.Euler(
                Mathf.Sin(t * 2.1f) * 5.5f,
                direction > 0f ? 18f : -18f,
                direction * 8f + Mathf.Sin(t * 2.8f) * 8f);
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;
            float pulse = 0.88f + Mathf.Sin(t * (1.6f + i * 0.27f) + i) * 0.12f;
            lights[i].intensity = baseIntensities[i] * pulse;
        }
    }
}
