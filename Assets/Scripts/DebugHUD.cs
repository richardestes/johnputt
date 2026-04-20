using System.Collections.Generic;
using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private int maxLines = 8;
    [SerializeField] private int fontSize = 18;

    private static DebugHUD _instance;
    private readonly List<string> _lines = new();
    private GUIStyle _style;

    private void Awake()
    {
        _instance = this;
    }

    public static void Log(string message)
    {
        if (_instance == null) return;
        _instance._lines.Add(message);
        if (_instance._lines.Count > _instance.maxLines)
            _instance._lines.RemoveAt(0);
    }

    private void OnGUI()
    {
        _style ??= new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize,
            fontStyle = FontStyle.Bold,
        };

        float lineHeight = fontSize + 4;
        float padding    = 10f;

        for (int i = 0; i < _lines.Count; i++)
        {
            float y = padding + i * lineHeight;

            _style.normal.textColor = Color.black;
            GUI.Label(new Rect(padding + 1, y + 1, Screen.width, lineHeight), _lines[i], _style);

            _style.normal.textColor = Color.white;
            GUI.Label(new Rect(padding, y, Screen.width, lineHeight), _lines[i], _style);
        }
    }
}
