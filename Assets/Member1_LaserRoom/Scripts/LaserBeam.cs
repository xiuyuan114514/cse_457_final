using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    public LaserRoomGameManager gameManager;
    public Material laserMaterial;
    public float beamRadius = 0.11f;

    BoxCollider triggerCollider;
    LineRenderer haloLineRenderer;
    LineRenderer coreLineRenderer;
    ParticleSystem vaporParticles;
    MeshRenderer outerCylinderRenderer;
    MeshRenderer coreCylinderRenderer;
    Material haloMaterialInstance;
    Material coreMaterialInstance;
    Material outerCylinderMaterialInstance;
    Material coreCylinderMaterialInstance;
    Material vaporMaterialInstance;
    float pulsePhase;

    public void Configure(
        string beamName,
        Vector3 start,
        Vector3 end,
        float radius,
        Material material,
        LaserRoomGameManager manager)
    {
        name = beamName;
        beamRadius = radius;
        laserMaterial = material;
        gameManager = manager;
        pulsePhase = LaserVisualPulse.StablePhase(beamName);

        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.001f)
            return;

        transform.SetPositionAndRotation(
            (start + end) * 0.5f,
            Quaternion.LookRotation(delta.normalized, Vector3.up));

        EnsureComponents();

        triggerCollider.isTrigger = true;
        triggerCollider.center = Vector3.zero;
        triggerCollider.size = new Vector3(beamRadius * 2f, beamRadius * 2f, length);

        haloMaterialInstance = CreateMaterialInstance(laserMaterial, beamName + "_HaloMaterial");
        ConfigureLine(haloLineRenderer, length, beamRadius * 1.15f, haloMaterialInstance);

        coreLineRenderer = CreateCoreLineRenderer(length);
        coreMaterialInstance = CreateMaterialInstance(laserMaterial, beamName + "_CoreMaterial");
        ConfigureLine(coreLineRenderer, length, beamRadius * 0.28f, coreMaterialInstance);

        outerCylinderRenderer = CreateEnergyCylinder("RedEnergyCylinder", length, beamRadius * 0.42f, beamName + "_CylinderMaterial");
        coreCylinderRenderer = CreateEnergyCylinder("HotCoreCylinder", length, beamRadius * 0.16f, beamName + "_HotCoreCylinderMaterial");

        ConfigureVapor(length);
        ApplyPulse(1f);

        LaserHazard hazard = GetComponent<LaserHazard>();
        if (hazard == null)
            hazard = gameObject.AddComponent<LaserHazard>();

        hazard.gameManager = gameManager;
    }

    void Awake()
    {
        EnsureComponents();
    }

    void Update()
    {
        if (haloLineRenderer == null)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8.5f + pulsePhase);
        ApplyPulse(pulse);
    }

    void EnsureComponents()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        if (haloLineRenderer == null)
            haloLineRenderer = GetComponent<LineRenderer>();
    }

    LineRenderer CreateCoreLineRenderer(float length)
    {
        Transform existing = transform.Find("WhiteHotCore");
        GameObject coreObject = existing != null
            ? existing.gameObject
            : new GameObject("WhiteHotCore");

        coreObject.transform.SetParent(transform, false);
        coreObject.transform.localPosition = Vector3.zero;
        coreObject.transform.localRotation = Quaternion.identity;

        LineRenderer renderer = coreObject.GetComponent<LineRenderer>();
        if (renderer == null)
            renderer = coreObject.AddComponent<LineRenderer>();

        ConfigureLine(renderer, length, beamRadius * 0.62f, null);
        return renderer;
    }

    void ConfigureLine(LineRenderer renderer, float length, float width, Material material)
    {
        renderer.useWorldSpace = false;
        renderer.positionCount = 2;
        renderer.SetPosition(0, new Vector3(0f, 0f, -length * 0.5f));
        renderer.SetPosition(1, new Vector3(0f, 0f, length * 0.5f));
        renderer.widthMultiplier = width;
        renderer.numCapVertices = 8;
        renderer.numCornerVertices = 4;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.alignment = LineAlignment.View;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (material != null)
            renderer.material = material;
    }

    void ConfigureVapor(float length)
    {
        vaporParticles = GetComponent<ParticleSystem>();
        if (vaporParticles == null)
            vaporParticles = gameObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = vaporParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.01f, 0.004f, 0.045f),
            new Color(1f, 0.035f, 0.012f, 0.105f));
        main.maxParticles = 32;

        ParticleSystem.EmissionModule emission = vaporParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f;

        ParticleSystem.ShapeModule shape = vaporParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(beamRadius * 1.75f, beamRadius * 1.75f, length);
        shape.position = Vector3.zero;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = vaporParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.01f, 0.004f), 0f),
                new GradientColorKey(new Color(1f, 0.045f, 0.012f), 0.55f),
                new GradientColorKey(new Color(0.55f, 0.02f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.105f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = vaporParticles.GetComponent<ParticleSystemRenderer>();
        vaporMaterialInstance = CreateVaporMaterial();
        renderer.material = vaporMaterialInstance;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = -2f;

        vaporParticles.Play();
    }

    void ApplyPulse(float pulse)
    {
        float restrainedPulse = Mathf.Lerp(0.92f, 1.08f, pulse);

        Color haloColor = LaserVisualPulse.HaloColor(pulse);
        haloLineRenderer.startColor = haloColor;
        haloLineRenderer.endColor = LaserVisualPulse.HaloColor(1f - pulse * 0.25f);
        haloLineRenderer.widthMultiplier = beamRadius * Mathf.Lerp(0.95f, 1.18f, pulse);
        ApplyMaterialColor(haloMaterialInstance, haloColor, LaserVisualPulse.HaloEmission(pulse) * restrainedPulse);

        if (coreLineRenderer != null)
        {
            Color coreColor = LaserVisualPulse.CoreColor(pulse);
            coreLineRenderer.startColor = coreColor;
            coreLineRenderer.endColor = LaserVisualPulse.CoreColor(1f - pulse * 0.2f);
            coreLineRenderer.widthMultiplier = beamRadius * Mathf.Lerp(0.22f, 0.34f, pulse);
            ApplyMaterialColor(coreMaterialInstance, coreColor, LaserVisualPulse.CoreEmission(pulse) * restrainedPulse);
        }

        if (outerCylinderRenderer != null)
            ApplyMaterialColor(outerCylinderMaterialInstance, haloColor, LaserVisualPulse.HaloEmission(pulse) * restrainedPulse);

        if (coreCylinderRenderer != null)
            ApplyMaterialColor(coreCylinderMaterialInstance, LaserVisualPulse.CoreColor(pulse), LaserVisualPulse.CoreEmission(pulse) * restrainedPulse);

        if (vaporParticles != null)
        {
            ParticleSystem.EmissionModule emission = vaporParticles.emission;
            emission.rateOverTime = Mathf.Lerp(7f, 12f, pulse);
        }
    }

    MeshRenderer CreateEnergyCylinder(string cylinderName, float length, float radius, string materialName)
    {
        Transform existing = transform.Find(cylinderName);
        GameObject cylinder = existing != null
            ? existing.gameObject
            : GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        cylinder.name = cylinderName;
        cylinder.transform.SetParent(transform, false);
        cylinder.transform.localPosition = Vector3.zero;
        cylinder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        cylinder.transform.localScale = new Vector3(radius, length * 0.5f, radius);

        Collider collider = cylinder.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        MeshRenderer renderer = cylinder.GetComponent<MeshRenderer>();
        Material material = CreateMaterialInstance(laserMaterial, materialName);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (cylinderName.Contains("HotCore"))
            coreCylinderMaterialInstance = material;
        else
            outerCylinderMaterialInstance = material;

        return renderer;
    }

    Material CreateMaterialInstance(Material source, string materialName)
    {
        Shader shader = source != null ? source.shader : Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = source != null ? new Material(source) : new Material(shader);
        material.name = materialName;
        return material;
    }

    Material CreateVaporMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader)
        {
            name = name + "_VaporMaterial"
        };
        ApplyMaterialColor(material, new Color(1f, 0.02f, 0.008f, 0.12f), new Color(1.4f, 0.04f, 0.01f, 1f));
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;
        return material;
    }

    void ApplyMaterialColor(Material material, Color baseColor, Color emission)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", baseColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
        }
    }
}

public class LaserPulseVisual : MonoBehaviour
{
    Renderer targetRenderer;
    Material materialInstance;
    float pulsePhase;

    public void Configure(string visualName, Material sourceMaterial)
    {
        pulsePhase = LaserVisualPulse.StablePhase(visualName);
        targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null && sourceMaterial != null)
        {
            materialInstance = new Material(sourceMaterial)
            {
                name = visualName + "_PulseMaterial"
            };
            targetRenderer.sharedMaterial = materialInstance;
        }

        ApplyPulse(1f);
    }

    void Update()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8.5f + pulsePhase);
        ApplyPulse(pulse);
    }

    void ApplyPulse(float pulse)
    {
        if (materialInstance == null)
            return;

        Color color = LaserVisualPulse.HaloColor(pulse);
        Color emission = LaserVisualPulse.HaloEmission(pulse);

        if (materialInstance.HasProperty("_BaseColor"))
            materialInstance.SetColor("_BaseColor", color);
        if (materialInstance.HasProperty("_Color"))
            materialInstance.SetColor("_Color", color);
        if (materialInstance.HasProperty("_EmissionColor"))
        {
            materialInstance.SetColor("_EmissionColor", emission);
            materialInstance.EnableKeyword("_EMISSION");
        }
    }
}

static class LaserVisualPulse
{
    static readonly Color HaloLow = new Color(0.86f, 0.005f, 0.002f, 0.88f);
    static readonly Color HaloHigh = new Color(1f, 0.025f, 0.006f, 0.98f);
    static readonly Color CoreLow = new Color(1f, 0.18f, 0.045f, 0.98f);
    static readonly Color CoreHigh = new Color(1f, 0.36f, 0.09f, 1f);
    static readonly Color HaloEmissionLow = new Color(7.2f, 0.025f, 0.008f, 1f);
    static readonly Color HaloEmissionHigh = new Color(10.5f, 0.12f, 0.018f, 1f);
    static readonly Color CoreEmissionLow = new Color(8.4f, 0.55f, 0.09f, 1f);
    static readonly Color CoreEmissionHigh = new Color(11.5f, 1.05f, 0.18f, 1f);

    public static Color HaloColor(float pulse)
    {
        return Color.Lerp(HaloLow, HaloHigh, pulse);
    }

    public static Color CoreColor(float pulse)
    {
        return Color.Lerp(CoreLow, CoreHigh, pulse);
    }

    public static Color HaloEmission(float pulse)
    {
        return Color.Lerp(HaloEmissionLow, HaloEmissionHigh, pulse);
    }

    public static Color CoreEmission(float pulse)
    {
        return Color.Lerp(CoreEmissionLow, CoreEmissionHigh, pulse);
    }

    public static float StablePhase(string value)
    {
        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619u;
            }

            return (hash % 6283u) * 0.001f;
        }
    }
}
