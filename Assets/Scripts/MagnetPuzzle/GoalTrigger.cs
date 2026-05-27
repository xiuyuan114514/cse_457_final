using System.Collections;
using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public GameObject exitDoor;
    public float openHeight = 3f; // kept for inspector compatibility, not used in portal mode

    [Tooltip("Trigger zone at the door opening — disabled until puzzle is solved")]
    public GameObject exitZone;

    /// <summary>Read-only: true once the core has been docked.</summary>
    public bool IsComplete { get; private set; }

    Light portalLight;

    void OnTriggerEnter(Collider other)
    {
        if (IsComplete) return;

        if (other.gameObject.name == "MagneticCore")
        {
            IsComplete = true;
            Debug.Log("Magnet puzzle complete — portal activated!");
            StartCoroutine(ActivatePortal());

            if (exitZone != null)
                exitZone.SetActive(true);
        }
    }

    IEnumerator ActivatePortal()
    {
        if (exitDoor == null) yield break;

        // Disable physical collider so the player can walk through the doorway
        foreach (var col in exitDoor.GetComponents<Collider>())
            col.enabled = false;

        // --- Portal material ---
        var rend = exitDoor.GetComponent<Renderer>();
        if (rend != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.05f, 0.65f, 1f, 1f));
            mat.SetColor("_EmissionColor", new Color(0f, 2.5f, 5f)); // HDR cyan
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
            rend.material = mat;
        }

        // --- Portal point light (appears gradually) ---
        var lightGO = new GameObject("PortalLight");
        lightGO.transform.position = exitDoor.transform.position;
        portalLight       = lightGO.AddComponent<Light>();
        portalLight.type  = LightType.Point;
        portalLight.color = new Color(0f, 0.75f, 1f);
        portalLight.range = 7f;
        portalLight.shadows = LightShadows.None;

        // Ramp the light up over 0.6 s
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            portalLight.intensity = Mathf.Lerp(0f, 8f, t / 0.6f);
            yield return null;
        }

        // Then pulse forever
        while (portalLight != null)
        {
            portalLight.intensity =
                Mathf.Lerp(5f, 10f, (Mathf.Sin(Time.time * 2.8f) + 1f) * 0.5f);
            yield return null;
        }
    }
}
