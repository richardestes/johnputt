using System.Collections.Generic;
using UnityEngine;

public class DamageNumbers : MonoBehaviour
{
    public static DamageNumbers Instance { get; private set; }

    private class Entry
    {
        public string  text;
        public Vector2 screenPos;
        public float   elapsed;
        public Color   color;
    }

    private readonly List<Entry> active = new List<Entry>();

    private const float Duration    = 1.2f;
    private const float FloatPixels = 60f;

    private GUIStyle _style;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Spawn(string text, Vector2 screenPos, Color color)
    {
        active.Add(new Entry { text = text, screenPos = screenPos, elapsed = 0f, color = color });
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            active[i].elapsed += Time.deltaTime;
            if (active[i].elapsed >= Duration)
                active.RemoveAt(i);
        }
    }

    private void OnGUI()
    {
        if (active.Count == 0) return;
        InitStyle();

        foreach (var e in active)
        {
            float t     = e.elapsed / Duration;
            float alpha = 1f - Mathf.Sqrt(t);
            float drift = FloatPixels * t;

            // Camera screen space is bottom-left origin; GUI is top-left — flip Y and apply upward drift
            float guiX = e.screenPos.x - 30f;
            float guiY = Screen.height - e.screenPos.y - drift;

            var col = e.color;
            col.a   = alpha;
            _style.normal.textColor = col;

            GUI.Label(new Rect(guiX, guiY, 60f, 30f), e.text, _style);
        }
    }

    private void InitStyle()
    {
        if (_style != null) return;
        _style = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
    }
}
