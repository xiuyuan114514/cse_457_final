using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generic "Return to Maze" UI button. Attach to any sub-scene.
/// Call Show() when the player completes the scene objective (e.g. via UnityEvent).
/// </summary>
public class ReturnToMazeUI : MonoBehaviour
{
    bool showButton = false;
    bool returning = false;
    GUIStyle buttonStyle;
    GUIStyle titleStyle;
    GUIStyle messageStyle;
    Texture2D panelTexture;
    Texture2D lineTexture;

    public void Show()
    {
        showButton = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (!showButton) return;

        InitStyles();

        float boxW = 420f, boxH = 210f;
        float x = Screen.width * 0.5f - boxW * 0.5f;
        float y = Screen.height * 0.5f - boxH * 0.5f;

        GUI.DrawTexture(new Rect(x, y, boxW, boxH), panelTexture);
        DrawBorder(new Rect(x, y, boxW, boxH), new Color(0.15f, 0.92f, 1f, 0.92f), 2f);

        titleStyle.normal.textColor = new Color(0.2f, 1f, 0.58f);
        GUI.Label(new Rect(x, y + 34f, boxW, 54f), "ROOM COMPLETE", titleStyle);

        messageStyle.normal.textColor = new Color(0.76f, 0.96f, 1f);
        GUI.Label(new Rect(x + 32f, y + 90f, boxW - 64f, 34f), "Objective achieved!", messageStyle);

        Rect btnRect = new Rect(x + 110f, y + 138f, 200f, 46f);
        if (!returning && GUI.Button(btnRect, "RETURN TO MAZE", buttonStyle))
        {
            ReturnToMaze();
        }
    }

    void ReturnToMaze()
    {
        returning = true;
        showButton = false;
        SubSceneReturnHandler.ReturnToMaze(0.35f);
    }

    void InitStyles()
    {
        if (titleStyle != null) return;

        panelTexture = MakeTexture(new Color(0.005f, 0.012f, 0.028f, 0.985f));
        lineTexture = MakeTexture(Color.white);

        titleStyle = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold
        };

        messageStyle = new GUIStyle(GUIStyle.none)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
    }

    void DrawBorder(Rect rect, Color color, float thickness)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), lineTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), lineTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), lineTexture);
        GUI.color = old;
    }

    Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
