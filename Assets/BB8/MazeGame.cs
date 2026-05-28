using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeGame : MonoBehaviour
{
    [Header("Game Settings")]
    public int totalKeys = 3;
    public float fallThreshold = -5f;

    [Header("Key → Scene mapping (order matches Key1/Key2/Key3)")]
    public string[] keyScenes = new string[]
    {
        "MagnetPuzzleRoom",
        "LaserRoom",
        "ConveyorChallengeRoom"
    };

    int keysCollected = 0;
    bool gameWon = false;
    bool gameLost = false;
    string loseReason = "";
    string message = "";
    float messageTimer = 0f;

    // Key names in the scene (Key1, Key2, Key3)
    string[] keyNames = new string[] { "Key1", "Key2", "Key3" };

    // References to key GameObjects so we can hide collected ones
    GameObject[] keyObjects = new GameObject[3];

    // True while waiting for delayed teleport — suppresses fall detection
    bool teleportPending = false;

    // UI styles
    GUIStyle scoreStyle;
    GUIStyle winStyle;
    GUIStyle loseStyle;
    GUIStyle messageStyle;
    GUIStyle buttonStyle;

    void Start()
    {
        // Find all keys by name Key1, Key2, Key3
        for (int i = 0; i < 3; i++)
        {
            var key = GameObject.Find(keyNames[i]);
            if (key != null)
                keyObjects[i] = key;
        }

        var session = GameSessionData.GetOrCreate();

        // Restore state from session
        keysCollected = session.KeysCollected;

        // Hide already-collected keys
        for (int i = 0; i < 3; i++)
        {
            if (session.KeyCollected[i] && keyObjects[i] != null)
                keyObjects[i].SetActive(false);
        }

        // If returning from a sub-scene, teleport player back to saved position
        if (session.ReturningFromSubScene)
        {
            session.ReturningFromSubScene = false;
            teleportPending = true;
            StartCoroutine(DelayedTeleport(session.ReturnPosition, session.ReturnRotation));
        }
    }

    void Update()
    {
        if (gameWon || gameLost || teleportPending) return;

        if (messageTimer > 0)
            messageTimer -= Time.deltaTime;

        // Fall detection
        var body = GetComponentInChildren<Rigidbody>();
        if (body != null && body.transform.position.y < fallThreshold)
        {
            gameLost = true;
            loseReason = "You fell off the maze!";
            Time.timeScale = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameWon || gameLost) return;

        // Key touch → save position and teleport to sub-scene
        for (int i = 0; i < 3; i++)
        {
            if (other.gameObject.name == keyNames[i])
            {
                if (GameSessionData.Instance != null && GameSessionData.Instance.KeyCollected[i])
                    return; // already collected

                // Save the key's position + Y offset so robot spawns above the maze
                var session = GameSessionData.GetOrCreate();
                Vector3 keyWorldPos = other.transform.position;
                session.ReturnPosition = keyWorldPos + Vector3.up * 2f;
                session.ReturnRotation = Quaternion.identity;
                session.CurrentKeyIndex = i;
                Debug.Log($"[MazeGame] Key {keyNames[i]} touched at worldPos={keyWorldPos}, saving returnPos={session.ReturnPosition}");

                // Load the sub-scene
                if (i < keyScenes.Length && !string.IsNullOrEmpty(keyScenes[i]))
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(keyScenes[i]);
                }
                return;
            }
        }

        // Exit detection
        if (other.gameObject.name == "Exit")
        {
            if (keysCollected >= totalKeys)
            {
                gameWon = true;
                Time.timeScale = 0f;
            }
            else
            {
                message = "Need all " + totalKeys + " keys! (" + keysCollected + "/" + totalKeys + ")";
                messageTimer = 2f;
            }
        }
    }

    void OnGUI()
    {
        InitStyles();

        // Key count - top left
        GUI.Label(new Rect(20, 20, 300, 40),
            "Keys: " + keysCollected + " / " + totalKeys, scoreStyle);

        // Temporary message
        if (messageTimer > 0 && !gameWon && !gameLost)
        {
            float w = 400, h = 40;
            GUI.Label(new Rect(Screen.width / 2 - w / 2, 80, w, h), message, messageStyle);
        }

        // Win screen
        if (gameWon)
        {
            DrawEndScreen("You Win!", new Color(0.2f, 1f, 0.2f), "All keys collected!");
        }

        // Lose screen
        if (gameLost)
        {
            DrawEndScreen("You Lose!", Color.red, loseReason);
        }
    }

    void DrawEndScreen(string title, Color titleColor, string subtitle)
    {
        float boxW = 400, boxH = 200;
        float boxX = Screen.width / 2 - boxW / 2;
        float boxY = Screen.height / 2 - boxH / 2;

        GUI.Box(new Rect(boxX, boxY, boxW, boxH), "");

        var titleStyle = gameWon ? winStyle : loseStyle;
        titleStyle.normal.textColor = titleColor;
        GUI.Label(new Rect(boxX, boxY + 30, boxW, 60), title, titleStyle);
        GUI.Label(new Rect(boxX, boxY + 80, boxW, 30), subtitle, messageStyle);

        if (GUI.Button(new Rect(boxX + boxW / 2 - 75, boxY + 130, 150, 40),
            "Play Again", buttonStyle))
        {
            Time.timeScale = 1f;
            // Reset session data on restart
            if (GameSessionData.Instance != null)
                Destroy(GameSessionData.Instance.gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    IEnumerator DelayedTeleport(Vector3 targetPos, Quaternion targetRot)
    {
        // Make all rigidbodies kinematic immediately to freeze physics
        var allRbs = GetComponentsInChildren<Rigidbody>();
        bool[] wasKinematic = new bool[allRbs.Length];
        for (int i = 0; i < allRbs.Length; i++)
        {
            wasKinematic[i] = allRbs[i].isKinematic;
            allRbs[i].isKinematic = true;
        }

        // Wait one frame for physics to settle
        yield return new WaitForFixedUpdate();

        // Find the Body (first rigidbody child) and compute offset
        var bodyRb = allRbs.Length > 0 ? allRbs[0] : null;
        if (bodyRb != null)
        {
            Vector3 offset = targetPos - bodyRb.position;
            Debug.Log($"[MazeGame] Teleport: target={targetPos}, bodyBefore={bodyRb.position}, offset={offset}");

            // Only move the parent transform — children move with it automatically
            transform.position += offset;

            // Sync each rigidbody's physics position to match its new transform position
            foreach (var rb in allRbs)
            {
                rb.position = rb.transform.position;
            }

            Debug.Log($"[MazeGame] Teleport done: bodyAfter={bodyRb.position}, bodyTransform={bodyRb.transform.position}");
        }

        // Restore kinematic state and zero velocities
        for (int i = 0; i < allRbs.Length; i++)
        {
            allRbs[i].isKinematic = wasKinematic[i];
            allRbs[i].linearVelocity = Vector3.zero;
            allRbs[i].angularVelocity = Vector3.zero;
        }

        teleportPending = false;
    }

    void InitStyles()
    {
        if (scoreStyle != null) return;

        scoreStyle = new GUIStyle(GUI.skin.label);
        scoreStyle.fontSize = 24;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.normal.textColor = Color.white;

        winStyle = new GUIStyle(GUI.skin.label);
        winStyle.fontSize = 48;
        winStyle.fontStyle = FontStyle.Bold;
        winStyle.alignment = TextAnchor.MiddleCenter;
        winStyle.normal.textColor = new Color(0.2f, 1f, 0.2f);

        loseStyle = new GUIStyle(GUI.skin.label);
        loseStyle.fontSize = 48;
        loseStyle.fontStyle = FontStyle.Bold;
        loseStyle.alignment = TextAnchor.MiddleCenter;
        loseStyle.normal.textColor = Color.red;

        messageStyle = new GUIStyle(GUI.skin.label);
        messageStyle.fontSize = 20;
        messageStyle.alignment = TextAnchor.MiddleCenter;
        messageStyle.normal.textColor = Color.yellow;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;
    }
}
