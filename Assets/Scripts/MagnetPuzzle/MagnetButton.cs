using UnityEngine;

/// <summary>
/// Pressure-plate magnet button. Activates when the player stands on it —
/// no key press required. Uses Physics.OverlapBox so the detection volume
/// is independent of the plate's visual scale.
/// </summary>
public class MagnetButton : MonoBehaviour
{
    [Header("Connections")]
    public MagneticCoreController magneticCore;
    public Transform magnetTarget;

    [Header("Plate Feel")]
    [Tooltip("How far the plate sinks when pressed (world units)")]
    public float pressDepth = 0.04f;
    public float pressSpeed = 10f;

    [Header("Detection")]
    [Tooltip("Half-width of the detection box around the plate")]
    public float detectionRadius = 0.85f;
    [Tooltip("Total height of the detection column above the plate")]
    public float detectionHeight = 2.0f;

    bool playerOnPlate;
    Vector3 restPosition;
    Renderer plateRenderer;
    Material plateMat;

    static readonly Color ColorIdle   = new Color(0.08f, 0.12f, 0.28f);
    static readonly Color ColorActive = new Color(0f,    0.7f,  0.25f);
    static readonly Color EmitIdle    = new Color(0f,    0.15f, 0.8f);
    static readonly Color EmitActive  = new Color(0f,    2.0f,  0.7f);

    void Start()
    {
        restPosition  = transform.position;
        plateRenderer = GetComponent<Renderer>();
        if (plateRenderer != null)
            plateMat = plateRenderer.material; // per-instance so colour doesn't bleed
    }

    void Update()
    {
        bool nowOn = IsPlayerOverPlate();

        if (nowOn && !playerOnPlate)
            Activate();

        playerOnPlate = nowOn;

        // Animate depression
        float targetY = playerOnPlate ? restPosition.y - pressDepth : restPosition.y;
        Vector3 pos   = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, targetY, pressSpeed * Time.deltaTime);
        transform.position = pos;

        // Colour feedback
        if (plateMat != null)
        {
            plateMat.SetColor("_BaseColor",     playerOnPlate ? ColorActive : ColorIdle);
            plateMat.SetColor("_EmissionColor", playerOnPlate ? EmitActive  : EmitIdle);
        }
    }

    /// <summary>
    /// Casts a tall box above the plate to detect the player standing on it.
    /// Box is in world space so it's unaffected by the plate's own scale.
    /// </summary>
    bool IsPlayerOverPlate()
    {
        Vector3 center = restPosition + Vector3.up * (detectionHeight * 0.5f + 0.05f);
        Vector3 half   = new Vector3(detectionRadius, detectionHeight * 0.5f, detectionRadius);
        Collider[] hits = Physics.OverlapBox(center, half, Quaternion.identity);
        foreach (Collider col in hits)
            if (col.CompareTag("Player")) return true;
        return false;
    }

    void Activate()
    {
        if (magneticCore != null && magnetTarget != null)
        {
            magneticCore.SetMagnetTarget(magnetTarget);
            Debug.Log($"[MagnetButton] Plate activated: {gameObject.name}");
        }
    }
}
