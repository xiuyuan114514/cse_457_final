using UnityEngine;
using UnityEngine.SceneManagement;

public class LaserRoomGameManager : MonoBehaviour
{
    public RobotController robot;
    public Rigidbody robotBody;
    public Transform spawnPoint;
    public float robotHeight = 1.2f;
    public bool resetImmediatelyOnDeath = true;

    enum LaserRoomState
    {
        Playing,
        Dead,
        Won
    }

    LaserRoomState state = LaserRoomState.Playing;
    GUIStyle titleStyle;
    GUIStyle messageStyle;
    GUIStyle buttonStyle;
    Texture2D panelTexture;
    Texture2D buttonTexture;
    Texture2D buttonHoverTexture;
    Texture2D buttonActiveTexture;
    Texture2D lineTexture;

    public void RegisterRobot(RobotController newRobot, Rigidbody body, Transform spawn)
    {
        robot = newRobot;
        robotBody = body;
        spawnPoint = spawn;
        state = LaserRoomState.Playing;

        ResetRobotToSpawn();
        SetRobotControlEnabled(true);
    }

    public void KillRobot()
    {
        if (state != LaserRoomState.Playing)
            return;

        state = LaserRoomState.Dead;
        SetRobotControlEnabled(false);

        if (resetImmediatelyOnDeath)
            ResetRobotToSpawn();
        else
            StopRobotMotion();
    }

    public void WinGame()
    {
        if (state != LaserRoomState.Playing)
            return;

        state = LaserRoomState.Won;
        SetRobotControlEnabled(false);
        StopRobotMotion();
    }

    public void ResetAndRetry()
    {
        ResetRobotToSpawn();
        state = LaserRoomState.Playing;
        SetRobotControlEnabled(true);
    }

    void ResetRobotToSpawn()
    {
        if (spawnPoint == null || robot == null)
            return;

        robot.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        Transform bodyTransform = robot.Body != null
            ? robot.Body.transform
            : robot.transform.Find("Body");

        if (bodyTransform != null)
        {
            bodyTransform.localPosition = Vector3.zero;
            bodyTransform.localRotation = Quaternion.identity;
        }

        StopRobotMotion();

        if (robotBody != null)
        {
            robotBody.position = spawnPoint.position;
            robotBody.rotation = Quaternion.identity;
            robotBody.Sleep();
        }
    }

    void StopRobotMotion()
    {
        if (robotBody == null)
            return;

        robotBody.linearVelocity = Vector3.zero;
        robotBody.angularVelocity = Vector3.zero;
    }

    void SetRobotControlEnabled(bool enabled)
    {
        if (robot != null)
            robot.enabled = enabled;

        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

    void OnGUI()
    {
        if (state == LaserRoomState.Playing)
            return;

        InitStyles();

        const float boxWidth = 360f;
        const float boxHeight = 166f;
        float x = Screen.width * 0.5f - boxWidth * 0.5f;
        float y = Screen.height * 0.5f - boxHeight * 0.5f;
        Rect panelRect = new Rect(x, y, boxWidth, boxHeight);

        GUI.DrawTexture(panelRect, panelTexture);
        DrawBorder(panelRect, new Color(0.15f, 0.92f, 1f, 0.92f), 2f);
        DrawBorder(new Rect(x + 6f, y + 6f, boxWidth - 12f, boxHeight - 12f), new Color(0.15f, 0.92f, 1f, 0.32f), 1f);
        DrawCornerAccents(panelRect, new Color(0.15f, 0.92f, 1f, 0.95f), 26f, 3f);

        if (state == LaserRoomState.Dead)
        {
            DrawCrispLabel(new Rect(x, y + 20f, boxWidth, 42f), "DEAD", titleStyle, new Color(1f, 0.16f, 0.08f));
            DrawCrispLabel(new Rect(x + 28f, y + 66f, boxWidth - 56f, 28f), "ROBOT LOST - TRY AGAIN", messageStyle,
                new Color(0.76f, 0.96f, 1f));

            Rect retryRect = new Rect(x + 112f, y + 108f, 136f, 36f);
            DrawSciFiButton(retryRect, "RETRY", new Color(1f, 0.18f, 0.1f, 0.8f));
            if (WasClicked(retryRect))
                ResetAndRetry();

            return;
        }

        DrawCrispLabel(new Rect(x, y + 20f, boxWidth, 42f), "YOU WIN", titleStyle, new Color(0.2f, 1f, 0.58f));
        DrawCrispLabel(new Rect(x + 28f, y + 66f, boxWidth - 56f, 28f), "LASER ROOM COMPLETE", messageStyle,
            new Color(0.76f, 0.96f, 1f));

        Rect returnRect = new Rect(x + 86f, y + 108f, 188f, 36f);
        DrawSciFiButton(returnRect, "RETURN TO MAZE", new Color(0.2f, 1f, 0.58f, 0.8f));
        if (WasClicked(returnRect))
            ReturnToMaze();
    }

    void ReturnToMaze()
    {
        var session = GameSessionData.GetOrCreate();
        if (session.CurrentKeyIndex >= 0)
            session.MarkKeyCollected(session.CurrentKeyIndex);
        session.ReturningFromSubScene = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene("Maze");
    }

    void InitStyles()
    {
        if (titleStyle != null)
            return;

        panelTexture = MakeTexture(new Color(0.005f, 0.012f, 0.028f, 0.985f));
        buttonTexture = MakeTexture(new Color(0.008f, 0.08f, 0.12f, 1f));
        buttonHoverTexture = MakeTexture(new Color(0.018f, 0.2f, 0.28f, 1f));
        buttonActiveTexture = MakeTexture(new Color(0.07f, 0.5f, 0.58f, 1f));
        lineTexture = MakeTexture(Color.white);

        titleStyle = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip
        };

        messageStyle = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            normal = { textColor = new Color(0.76f, 0.96f, 1f) }
        };

        buttonStyle = new GUIStyle(GUIStyle.none)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            normal =
            {
                textColor = new Color(0.82f, 1f, 1f)
            },
            hover =
            {
                textColor = Color.white
            },
            active =
            {
                textColor = Color.white
            },
            focused =
            {
                textColor = Color.white
            }
        };
    }

    Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void DrawCrispLabel(Rect rect, string text, GUIStyle style, Color textColor)
    {
        Color oldColor = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
        style.normal.textColor = oldColor;
    }

    void DrawBorder(Rect rect, Color color, float thickness)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), lineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), lineTexture);
        GUI.color = oldColor;
    }

    void DrawSciFiButton(Rect rect, string text, Color accentColor)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        bool active = hover && Event.current.type == EventType.MouseDown;
        Texture2D background = active ? buttonActiveTexture : hover ? buttonHoverTexture : buttonTexture;

        GUI.DrawTexture(rect, background);
        DrawBorder(rect, accentColor, 1f);
        DrawBorder(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f),
            new Color(0.15f, 0.92f, 1f, hover ? 0.65f : 0.35f), 1f);
        DrawCornerAccents(rect, accentColor, 11f, 2f);

        Color oldColor = buttonStyle.normal.textColor;
        int oldFontSize = buttonStyle.fontSize;
        GUIContent content = new GUIContent(text);
        while (buttonStyle.CalcSize(content).x > rect.width - 18f && buttonStyle.fontSize > 11)
        {
            buttonStyle.fontSize--;
        }

        buttonStyle.normal.textColor = hover ? Color.white : new Color(0.82f, 1f, 1f);
        buttonStyle.hover.textColor = buttonStyle.normal.textColor;
        buttonStyle.active.textColor = buttonStyle.normal.textColor;
        buttonStyle.focused.textColor = buttonStyle.normal.textColor;
        Color shadowColor = buttonStyle.normal.textColor;
        buttonStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), content, buttonStyle);
        buttonStyle.normal.textColor = shadowColor;
        GUI.Label(rect, content, buttonStyle);
        buttonStyle.fontSize = oldFontSize;
        buttonStyle.normal.textColor = oldColor;
    }

    void DrawCornerAccents(Rect rect, Color color, float length, float thickness)
    {
        Color oldColor = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(new Rect(rect.x, rect.y, length, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, length), lineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - length, rect.y, length, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, length), lineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, length, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - length, thickness, length), lineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - length, rect.yMax - thickness, length, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMax - length, thickness, length), lineTexture);

        GUI.color = oldColor;
    }

    bool WasClicked(Rect rect)
    {
        Event current = Event.current;
        if (current.type != EventType.MouseUp || current.button != 0 || !rect.Contains(current.mousePosition))
            return false;

        current.Use();
        return true;
    }
}
