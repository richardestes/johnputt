using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    private Texture2D _dotCompleted;
    private Texture2D _dotCurrent;
    private Texture2D _dotGlow;
    private Texture2D _dotLocked;
    private Texture2D _lineFilled;
    private Texture2D _lineDim;

    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _labelDimStyle;
    private GUIStyle _invisible;

    private bool _initialized;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnGUI()
    {
        Init();

        var act     = GameManager.Instance?.CurrentAct;
        int current = GameManager.Instance?.CurrentNodeIndex ?? 0;
        if (act == null) return;

        int   n       = act.encounters.Length;
        float spacing = 64f;
        float r       = 10f;
        float rGlow   = 20f;
        float lineW   = 4f;
        float cx      = Screen.width  * 0.5f;
        float startY  = (Screen.height - (n - 1) * spacing) * 0.5f;

        GUI.Label(new Rect(cx - 160f, startY - 60f, 320f, 44f), act.actName, _titleStyle);

        // Lines drawn first so dots sit on top
        for (int i = 0; i < n - 1; i++)
        {
            float y0 = startY + i       * spacing + r;
            float y1 = startY + (i + 1) * spacing - r;
            GUI.DrawTexture(new Rect(cx - lineW * 0.5f, y0, lineW, y1 - y0),
                            i < current ? _lineFilled : _lineDim);
        }

        // Dots
        for (int i = 0; i < n; i++)
        {
            bool  done   = i < current;
            bool  active = i == current;
            float cy     = startY + i * spacing;

            bool hovered = active && new Rect(cx - rGlow, cy - rGlow, rGlow * 2, rGlow * 2)
                                         .Contains(Event.current.mousePosition);

            if (hovered)
                GUI.DrawTexture(new Rect(cx - rGlow, cy - rGlow, rGlow * 2, rGlow * 2), _dotGlow);

            GUI.color = hovered ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - r, cy - r, r * 2, r * 2),
                            done ? _dotCompleted : (active ? _dotCurrent : _dotLocked));
            GUI.color = Color.white;

            GUI.Label(new Rect(cx + r + 12f,  cy - 12f, 220f, 24f),
                      act.encounters[i].displayName,
                      _labelDimStyle);

            if (active && GUI.Button(new Rect(cx - rGlow, cy - rGlow, rGlow * 2, rGlow * 2), "", _invisible))
                GameManager.Instance.LoadEncounterNode(i);
        }
    }

    void Init()
    {
        if (_initialized) return;
        _initialized = true;

        _dotCompleted = MakeCircle(64, new Color(0.55f, 0.55f, 0.60f));
        _dotCurrent   = MakeCircle(64, new Color(1.00f, 0.85f, 0.20f));
        _dotGlow      = MakeCircle(64, new Color(1.00f, 0.85f, 0.20f, 0.20f));
        _dotLocked    = MakeCircle(64, new Color(0.22f, 0.22f, 0.25f));
        _lineFilled   = MakeSolid(new Color(0.55f, 0.55f, 0.60f));
        _lineDim      = MakeSolid(new Color(0.22f, 0.22f, 0.25f));

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white },
            hover     = { textColor = Color.white }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal   = { textColor = Color.white }
        };

        _labelDimStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal   = { textColor = new Color(0.35f, 0.35f, 0.35f) },
            hover    = { textColor = new Color(0.35f, 0.35f, 0.35f) }
        };

        _invisible = new GUIStyle();
    }

    static Texture2D MakeCircle(int size, Color color)
    {
        var   tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        var   center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius
                    ? color : Color.clear);
        tex.Apply();
        return tex;
    }

    static Texture2D MakeSolid(Color color)
    {
        var tex = new Texture2D(2, 2);
        for (int i = 0; i < 4; i++) tex.SetPixel(i % 2, i / 2, color);
        tex.Apply();
        return tex;
    }
}
