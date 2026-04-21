using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private int logMaxLines = 5;
    [SerializeField] private int statsFontSize = 16;
    [SerializeField] private int logFontSize   = 13;
    [SerializeField] private Key toggleKey     = Key.Tab;

    private static DebugHUD _instance;
    private readonly List<string> _log = new();
    private bool _visible = false;

    private GUIStyle _statsStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _logStyle;
    private GUIStyle _panelStyle;

    private void Awake() => _instance = this;

    private void Update()
    {
        if (Keyboard.current[toggleKey].wasPressedThisFrame)
            _visible = !_visible;
    }

    public static void Log(string message)
    {
        if (_instance == null) return;
        _instance._log.Add(message);
        if (_instance._log.Count > _instance.logMaxLines)
            _instance._log.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (!_visible) return;
        InitStyles();

        float x       = 10f;
        float y       = 10f;
        float panelW  = 260f;
        float lineH   = statsFontSize + 6f;
        float pad     = 8f;

        var em = EncounterManager.Instance;
        var ps = PlayerStats.Instance;

        if (em != null && ps != null)
        {
            var lines = BuildStatLines(em, ps);
            float panelH = pad * 2 + lines.Count * lineH;

            GUI.Box(new Rect(x, y, panelW, panelH), GUIContent.none, _panelStyle);

            float ly = y + pad;
            foreach (var line in lines)
            {
                DrawLabel(line.header, line.value, x + pad, ly, panelW - pad * 2, lineH);
                ly += lineH;
            }

            y += panelH + 6f;
        }

        // Event log
        if (_log.Count > 0)
        {
            float logLineH = logFontSize + 5f;
            float logH     = pad * 2 + _log.Count * logLineH;

            GUI.Box(new Rect(x, y, panelW, logH), GUIContent.none, _panelStyle);

            float ly = y + pad;
            foreach (var line in _log)
            {
                DrawShadowLabel(line, x + pad, ly, panelW - pad * 2, logLineH, _logStyle);
                ly += logLineH;
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────

    private struct StatLine { public string header; public string value; }

    private List<StatLine> BuildStatLines(EncounterManager em, PlayerStats ps)
    {
        var list = new List<StatLine>();

        list.Add(new StatLine { header = "State",      value = em.State.ToString() });
        list.Add(new StatLine { header = "───────",    value = "" });
        list.Add(new StatLine { header = "Player",     value = $"{ps.currentHealth} / {ps.maxHealth} HP" });
        list.Add(new StatLine { header = "Strokes",    value = $"{em.StrokesRemaining} / {em.MaxStrokes}" });
        list.Add(new StatLine { header = "Multiplier", value = $"{Mathf.Max(1, em.StrokesRemaining) * 100}%" });

        var atk = em.LastAttack;
        if (atk.Total > 0)
        {
            list.Add(new StatLine { header = "───────", value = "" });
            list.Add(new StatLine { header = "Base dmg",   value = atk.Base.ToString() });
            if (atk.DamageBonus > 0)
                list.Add(new StatLine { header = "Flat bonus", value = $"+{atk.DamageBonus}" });
            if (atk.BankShotBonus > 0)
                list.Add(new StatLine { header = "Bank shot",  value = $"+{atk.BankShotBonus}" });
            list.Add(new StatLine { header = "Total",      value = $"{atk.Total} dmg" });
        }

        var enemies = em.SpawnedEnemies;
        if (enemies != null && enemies.Length > 0)
        {
            list.Add(new StatLine { header = "───────", value = "" });
            foreach (var e in enemies)
            {
                if (e == null) continue;
                string hp = e.IsDead ? "DEAD" : $"{e.CurrentHealth} / {e.maxHealth} HP";
                list.Add(new StatLine { header = e.name, value = hp });
            }
        }

        return list;
    }

    private void DrawLabel(string header, string value, float x, float y, float w, float h)
    {
        DrawShadowLabel(header, x, y, w * 0.55f, h, _headerStyle);
        if (!string.IsNullOrEmpty(value))
            DrawShadowLabel(value, x + w * 0.55f, y, w * 0.45f, h, _statsStyle);
    }

    private void DrawShadowLabel(string text, float x, float y, float w, float h, GUIStyle style)
    {
        var shadow = new Rect(x + 1, y + 1, w, h);
        var normal = new Rect(x,     y,     w, h);

        Color prev = style.normal.textColor;
        style.normal.textColor = Color.black;
        GUI.Label(shadow, text, style);
        style.normal.textColor = prev;
        GUI.Label(normal, text, style);
    }

    private void InitStyles()
    {
        if (_statsStyle != null) return;

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.55f)) }
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = statsFontSize,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.75f, 0.75f, 0.75f) }
        };

        _statsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = statsFontSize,
            fontStyle = FontStyle.Normal,
            normal    = { textColor = Color.white }
        };

        _logStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = logFontSize,
            fontStyle = FontStyle.Normal,
            normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
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
