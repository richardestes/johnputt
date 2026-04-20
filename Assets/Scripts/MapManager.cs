using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    private GUIStyle _titleStyle;
    private GUIStyle _nodeStyle;
    private GUIStyle _completedStyle;
    private GUIStyle _lockedStyle;

    private string[] _labels;
    private int      _cachedNodeIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnGUI()
    {
        InitStyles();

        ActDefinition act     = GameManager.Instance.CurrentAct;
        int           current = GameManager.Instance.CurrentNodeIndex;

        if (act == null) return;

        RebuildLabelsIfDirty(act, current);

        float cardW  = 260f;
        float cardH  = 60f;
        float pad    = 12f;
        float totalH = act.encounters.Length * (cardH + pad);
        float x      = (Screen.width  - cardW) / 2f;
        float y      = (Screen.height - totalH) / 2f;

        GUI.Label(new Rect(x, y - 50f, cardW, 40f), act.actName, _titleStyle);

        for (int i = 0; i < act.encounters.Length; i++)
        {
            float nodeY = y + i * (cardH + pad);
            bool  done  = i < current;
            bool  active = i == current;

            if (done)
                GUI.Box(new Rect(x, nodeY, cardW, cardH), _labels[i], _completedStyle);
            else if (active)
            {
                if (GUI.Button(new Rect(x, nodeY, cardW, cardH), _labels[i], _nodeStyle))
                    SelectNode(i);
            }
            else
                GUI.Box(new Rect(x, nodeY, cardW, cardH), _labels[i], _lockedStyle);
        }
    }

    private void RebuildLabelsIfDirty(ActDefinition act, int current)
    {
        if (_cachedNodeIndex == current && _labels != null) return;

        _labels = new string[act.encounters.Length];
        for (int i = 0; i < act.encounters.Length; i++)
        {
            string name = act.encounters[i].displayName;
            _labels[i] = i < current  ? $"✓  {name}"
                       : i == current ? $"▶  {name}"
                       :                $"🔒  {name}";
        }
        _cachedNodeIndex = current;
    }

    private void SelectNode(int index)
    {
        GameManager.Instance.LoadEncounterNode(index);
    }

    private void InitStyles()
    {
        if (_titleStyle != null) return;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };

        _nodeStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(16, 0, 0, 0)
        };

        _completedStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 16,
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(16, 0, 0, 0),
            normal    = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };

        _lockedStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 16,
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(16, 0, 0, 0),
            normal    = { textColor = new Color(0.4f, 0.4f, 0.4f) }
        };
    }
}
