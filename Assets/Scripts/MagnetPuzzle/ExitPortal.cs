using UnityEngine;

/// <summary>
/// Placed on the disabled ExitZone trigger at the door opening.
/// GoalTrigger enables this object when the puzzle is solved.
/// When the player walks through, it starts the shared sci-fi return transition.
/// </summary>
public class ExitPortal : MonoBehaviour
{
    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;
        var visuals = FindFirstObjectByType<MagnetRoomVisuals>();
        if (visuals != null)
            visuals.HideCompletionOverlay();
        SubSceneReturnHandler.ReturnToMaze();
    }
}
