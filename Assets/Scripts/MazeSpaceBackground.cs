using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Attach to any GameObject in the Maze scene.
/// At runtime, replaces the default skybox with a procedural Star Wars–style
/// space environment: deep-black sky, scattered stars, and colored nebula glow.
/// </summary>
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
    public bool enableFog = true;
    public Color fogColor = new Color(0.02f, 0.005f, 0.04f, 1f);
    public float fogDensity = 0.008f;

    // Runtime references
    GameObject domeRoot;
    Material domeMaterial;
    Material starMaterialWhite;
    Material starMaterialBlue;
    Material starMaterialOrange;

    void Start()
    {
        SetupCamera();
        SetupAmbientLighting();
        SetupFog();
        CreateStarDome();
        CreateStars();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.001f, 0.001f, 0.005f, 1f);
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, domeRadius * 2f);
    }

    void SetupAmbientLighting()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.06f, 1f);
        RenderSettings.reflectionIntensity = 0.1f;
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
        domeRoot.transform.SetParent(transform, false);

        // Create an inverted sphere (renders on the inside)
        var meshFilter = domeRoot.AddComponent<MeshFilter>();
        var meshRenderer = domeRoot.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateInvertedSphereMesh(domeRadius, 32, 32);

        // Very dark emissive material for the dome
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        domeMaterial = new Material(shader);
        domeMaterial.SetColor("_BaseColor", new Color(0.003f, 0.003f, 0.012f, 1f));
        domeMaterial.SetColor("_Color", new Color(0.003f, 0.003f, 0.012f, 1f));
        // Render behind everything
        domeMaterial.renderQueue = (int)RenderQueue.Background;

        meshRenderer.material = domeMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    void CreateStars()
    {
        // Prepare star materials with different colors
        starMaterialWhite = CreateStarMaterial(new Color(1f, 1f, 1f, 1f), 2f);
        starMaterialBlue = CreateStarMaterial(new Color(0.6f, 0.7f, 1f, 1f), 2.5f);
        starMaterialOrange = CreateStarMaterial(new Color(1f, 0.7f, 0.3f, 1f), 1.8f);

        var starsParent = new GameObject("Stars");
        starsParent.transform.SetParent(transform, false);

        // Shared quad mesh for all stars
        Mesh quadMesh = CreateQuadMesh();

        for (int i = 0; i < starCount; i++)
        {
            var starGO = new GameObject("Star");
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

            // Color distribution: 70% white, 15% blue, 15% orange
            float roll = Random.value;
            if (roll < 0.15f)
                mr.sharedMaterial = starMaterialBlue;
            else if (roll < 0.30f)
                mr.sharedMaterial = starMaterialOrange;
            else
                mr.sharedMaterial = starMaterialWhite;
        }
    }

    Material CreateStarMaterial(Color baseColor, float emissionIntensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        var mat = new Material(shader);
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
}
