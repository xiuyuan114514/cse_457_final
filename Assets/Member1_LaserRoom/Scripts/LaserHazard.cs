using UnityEngine;

public class LaserHazard : MonoBehaviour
{
    public LaserRoomGameManager gameManager;

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindAnyObjectByType<LaserRoomGameManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameManager == null || !IsRobotCollider(other))
            return;

        gameManager.KillRobot();
    }

    static bool IsRobotCollider(Collider other)
    {
        if (other.GetComponentInParent<RobotController>() != null)
            return true;

        Rigidbody rb = other.attachedRigidbody;
        return rb != null && rb.GetComponentInParent<RobotController>() != null;
    }
}
