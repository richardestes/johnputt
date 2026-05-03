using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    private bool showing = false;

    private GUIStyle _titleStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _overlayStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Show() => showing = true;

    private void OnGUI()
    {
        if (!showing) return;

        InitStyles();

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _overlayStyle);

        float panelW = 300f;
        float panelH = 160f;
        float panelX = (Screen.width  - panelW) / 2f;
        float panelY = (Screen.height - panelH) / 2f;

        GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none);

        GUI.Label(
            new Rect(panelX, panelY + 20f, panelW, 50f),
            "GAME OVER",
            _titleStyle
        );

        var ps = PlayerStats.Instance;
        if (ps != null)
        {
            string stats = $"{ps.currentHealth}/{ps.maxHealth} HP  •  {ps.currentBalls}/{ps.maxBalls} balls";
            GUI.Label(new Rect(panelX, panelY + 72f, panelW, 26f), stats, _subtitleStyle);
        }

        if (GUI.Button(new Rect(panelX + 60f, panelY + panelH - 48f, panelW - 120f, 34f), "Restart", _buttonStyle))
        {
            showing = false;
            GameManager.Instance.RestartRun();
        }
    }

    private GUIStyle _subtitleStyle;

    private void InitStyles()
    {
        if (_titleStyle != null) return;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.red },
        };

        _subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.8f, 0.8f, 0.8f) },
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
        };

        _overlayStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.75f)) }
        };
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, col);
        tex.Apply();
        return tex;
    }
}
