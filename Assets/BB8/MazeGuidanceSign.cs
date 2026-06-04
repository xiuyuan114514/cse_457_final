using UnityEngine;

[ExecuteAlways]
public class MazeGuidanceSign : MonoBehaviour
{
    const string SignName = "NE_Exit_GuidanceBeacon";
    const string VersionMarkerName = "GuidanceBeacon_V10";
    Transform signRoot;
    Material cyan;
    Material amber;
    Material dark;

    void OnEnable()
    {
        BuildSign();
    }

    void OnDisable()
    {
        ClearSign();
    }

    void Update()
    {
        BuildSign();

        if (signRoot == null)
            return;

        float t = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        for (int i = 0; i < 3; i++)
        {
            Transform chevron = signRoot.Find($"Chevron_{i}");
            if (chevron == null)
                continue;

            float pulse = 1f + Mathf.Sin(t * 3.1f - i * 0.55f) * 0.08f;
            chevron.localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    void BuildSign()
    {
        if (signRoot != null)
        {
            ApplyPose();
            return;
        }

        Transform existing = transform.Find(SignName);
        if (existing != null)
        {
            if (existing.Find(VersionMarkerName) == null)
            {
                signRoot = existing;
                ClearSign();
            }
            else
            {
                signRoot = existing;
                ApplyPose();
                return;
            }
        }

        CreateMaterials();
        signRoot = new GameObject(SignName).transform;
        signRoot.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        signRoot.SetParent(transform, false);
        ApplyPose();

        var marker = new GameObject(VersionMarkerName);
        marker.transform.SetParent(signRoot, false);

        AddBox("BeaconBackPanel", new Vector3(0f, 0f, -0.03f), new Vector3(1.22f, 1.7f, 0.08f), dark);
        AddBox("TopAmberStatus", new Vector3(0f, 0.68f, 0.03f), new Vector3(0.82f, 0.07f, 0.055f), amber);
        AddBox("BottomAmberStatus", new Vector3(0f, -0.68f, 0.03f), new Vector3(0.82f, 0.07f, 0.055f), amber);
        for (int i = 0; i < 3; i++)
            CreateChevron($"Chevron_{i}", new Vector3(0.34f - i * 0.34f, 0f, 0.06f));

        var lightGo = new GameObject("BeaconLight");
        lightGo.transform.SetParent(signRoot, false);
        lightGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0f, 0.95f, 1f);
        light.intensity = 2.6f;
        light.range = 4f;
    }

    void ApplyPose()
    {
        signRoot.localPosition = new Vector3(14.25f, 1.45f, -0.95f);
        signRoot.localRotation = Quaternion.Euler(0f, -90f, 0f);
    }

    void ClearSign()
    {
        if (signRoot == null)
            return;

        if (Application.isPlaying)
            Destroy(signRoot.gameObject);
        else
            DestroyImmediate(signRoot.gameObject);
        signRoot = null;
    }

    void CreateMaterials()
    {
        cyan = MakeMaterial("MazeGuide_Cyan", new Color(0.02f, 0.85f, 1f), new Color(0f, 2.5f, 3.5f));
        amber = MakeMaterial("MazeGuide_Amber", new Color(1f, 0.72f, 0.18f), new Color(2.2f, 1.1f, 0.15f));
        dark = MakeMaterial("MazeGuide_DarkPlate", new Color(0.03f, 0.045f, 0.055f), new Color(0f, 0f, 0f));
    }

    Material MakeMaterial(string name, Color baseColor, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || !shader.isSupported)
            shader = Shader.Find("Standard");

        var material = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
        }
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.35f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.72f);
        return material;
    }

    Transform AddBox(string name, Vector3 localPosition, Vector3 localScale, Material material)
        => AddPrimitive(name, PrimitiveType.Cube, localPosition, localScale, material);

    void CreateChevron(string name, Vector3 localPosition)
    {
        var root = new GameObject(name).transform;
        root.SetParent(signRoot, false);
        root.localPosition = localPosition;
        Transform stem = AddBox($"{name}_Stem", new Vector3(-0.08f, 0f, 0.08f), new Vector3(0.33f, 0.08f, 0.055f), cyan);
        Transform top = AddBox($"{name}_TopHead", new Vector3(0.1f, 0.1f, 0.08f), new Vector3(0.11f, 0.34f, 0.055f), cyan);
        Transform bottom = AddBox($"{name}_BottomHead", new Vector3(0.1f, -0.1f, 0.08f), new Vector3(0.11f, 0.34f, 0.055f), cyan);
        stem.SetParent(root, false);
        top.SetParent(root, false);
        bottom.SetParent(root, false);
        top.localRotation = Quaternion.Euler(0f, 0f, 45f);
        bottom.localRotation = Quaternion.Euler(0f, 0f, -45f);
    }

    Transform AddPrimitive(string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(signRoot, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }
        return go.transform;
    }

}
