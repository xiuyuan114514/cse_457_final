using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    public LaserRoomGameManager gameManager;
    public Material laserMaterial;
    public float beamRadius = 0.11f;

    BoxCollider triggerCollider;
    LineRenderer lineRenderer;

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

        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(0f, 0f, -length * 0.5f));
        lineRenderer.SetPosition(1, new Vector3(0f, 0f, length * 0.5f));
        lineRenderer.widthMultiplier = beamRadius * 2f;
        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        if (laserMaterial != null)
            lineRenderer.material = laserMaterial;

        LaserHazard hazard = GetComponent<LaserHazard>();
        if (hazard == null)
            hazard = gameObject.AddComponent<LaserHazard>();

        hazard.gameManager = gameManager;
    }

    void Awake()
    {
        EnsureComponents();
    }

    void EnsureComponents()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }
}
