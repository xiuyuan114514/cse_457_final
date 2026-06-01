using UnityEngine;

/// <summary>
/// Persists maze game state across scene loads.
/// Singleton that survives scene transitions via DontDestroyOnLoad.
/// </summary>
public class GameSessionData : MonoBehaviour
{
    public static GameSessionData Instance { get; private set; }

    // Player position/rotation when they left the maze
    public Vector3 ReturnPosition;
    public Quaternion ReturnRotation;

    // Which key index (0,1,2) was just touched — used to know which scene to load
    public int CurrentKeyIndex = -1;

    // Track which keys have been collected (completed their scene)
    public bool[] KeyCollected = new bool[3];

    // Total keys collected so far
    public int KeysCollected = 0;

    // Whether we are returning from a sub-scene
    public bool ReturningFromSubScene = false;

    // True only after the maze saved a real return pose before loading a challenge room.
    public bool HasReturnPose = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void MarkKeyCollected(int keyIndex)
    {
        if (keyIndex >= 0 && keyIndex < KeyCollected.Length && !KeyCollected[keyIndex])
        {
            KeyCollected[keyIndex] = true;
            KeysCollected++;
        }
    }

    public static GameSessionData GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameSessionData");
        return go.AddComponent<GameSessionData>();
    }
}
