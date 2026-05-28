using UnityEngine;

public class DangerZone : MonoBehaviour
{
    public MagneticCoreController magneticCore;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MagneticCore"))
        {
            Debug.Log("Core touched danger zone. Resetting core.");

            if (magneticCore != null)
            {
                magneticCore.ResetCore();
            }
        }
    }
}