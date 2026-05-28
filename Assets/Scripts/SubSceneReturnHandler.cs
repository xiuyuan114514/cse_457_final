using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to a GameObject in any sub-scene (LaserRoom, ConveyorRoom, MagnetPuzzle).
/// It auto-creates itself when the scene loads if coming from the maze.
/// Alternatively, add it to any scene manually.
///
/// For scenes that don't have an explicit "Return to Maze" button,
/// this provides a keyboard shortcut (Escape) to return, and can be
/// called from other scripts via SubSceneReturnHandler.ReturnToMaze().
/// </summary>
public class SubSceneReturnHandler : MonoBehaviour
{
    public static void ReturnToMaze()
    {
        var session = GameSessionData.GetOrCreate();
        if (session.CurrentKeyIndex >= 0)
            session.MarkKeyCollected(session.CurrentKeyIndex);
        session.ReturningFromSubScene = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Maze");
    }
}
