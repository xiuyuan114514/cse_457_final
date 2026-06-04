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
    static bool returnInProgress = false;

    public static void ReturnToMaze(float delayBeforeTransition = 0.35f)
    {
        if (returnInProgress) return;
        returnInProgress = true;

        var runner = new GameObject("SubSceneReturnDelay");
        DontDestroyOnLoad(runner);
        var handler = runner.AddComponent<SubSceneReturnHandler>();
        handler.StartCoroutine(handler.ReturnAfterDelay(Mathf.Max(0f, delayBeforeTransition)));
    }

    System.Collections.IEnumerator ReturnAfterDelay(float delayBeforeTransition)
    {
        var session = GameSessionData.GetOrCreate();
        bool hasMazeReturn = session.HasReturnPose && session.CurrentKeyIndex >= 0;
        if (hasMazeReturn)
            session.MarkKeyCollected(session.CurrentKeyIndex);
        session.ReturningFromSubScene = hasMazeReturn;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;

        if (delayBeforeTransition > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeTransition);

        // Play the shared sci-fi transition, then load Maze.
        SceneTransitionOverlay.Show(2.8f);
        returnInProgress = false;
        Destroy(gameObject);
    }
}
