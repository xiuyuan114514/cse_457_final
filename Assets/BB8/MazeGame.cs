using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeGame : MonoBehaviour
{
    [Header("Game Settings")]
    public int totalKeys = 3;
    public float fallThreshold = -5f;
    public float timeLimit = 180f; // 3 minutes

    int keysCollected = 0;
    bool gameWon = false;
    bool gameLost = false;
    string loseReason = "";
    string message = "";
    float messageTimer = 0f;
    float timeRemaining;

    // UI style
    GUIStyle scoreStyle;
    GUIStyle winStyle;
    GUIStyle loseStyle;
    GUIStyle messageStyle;
    GUIStyle buttonStyle;
    GUIStyle timerStyle;

    void Start()
    {
        timeRemaining = timeLimit;
    }

    void Update()
    {
        if (gameWon || gameLost) return;

        if (messageTimer > 0)
            messageTimer -= Time.deltaTime;

        // Timer countdown
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            gameLost = true;
            loseReason = "Time's up!";
            Time.timeScale = 0f;
            return;
        }

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

        // Key collection
        if (other.gameObject.name.StartsWith("Key"))
        {
            Destroy(other.gameObject);
            keysCollected++;
            message = "Key collected! (" + keysCollected + "/" + totalKeys + ")";
            messageTimer = 2f;
            return;
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

        // Score - top left
        GUI.Label(new Rect(20, 20, 300, 40),
            "Keys: " + keysCollected + " / " + totalKeys, scoreStyle);

        // Timer - top right
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        string timerText = string.Format("{0}:{1:00}", minutes, seconds);
        timerStyle.normal.textColor = timeRemaining <= 30f ? Color.red : Color.white;
        GUI.Label(new Rect(Screen.width - 160, 20, 140, 40), timerText, timerStyle);

        // Temporary message
        if (messageTimer > 0 && !gameWon && !gameLost)
        {
            float w = 400, h = 40;
            GUI.Label(new Rect(Screen.width / 2 - w / 2, 80, w, h), message, messageStyle);
        }

        // Win screen
        if (gameWon)
        {
            DrawEndScreen("You Win!", new Color(0.2f, 1f, 0.2f),
                "Time: " + string.Format("{0}:{1:00}",
                    Mathf.FloorToInt((timeLimit - timeRemaining) / 60f),
                    Mathf.FloorToInt((timeLimit - timeRemaining) % 60f)));
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
            RestartGame();
        }
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void InitStyles()
    {
        if (scoreStyle != null) return;

        scoreStyle = new GUIStyle(GUI.skin.label);
        scoreStyle.fontSize = 24;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.normal.textColor = Color.white;

        timerStyle = new GUIStyle(GUI.skin.label);
        timerStyle.fontSize = 28;
        timerStyle.fontStyle = FontStyle.Bold;
        timerStyle.alignment = TextAnchor.MiddleRight;
        timerStyle.normal.textColor = Color.white;

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
