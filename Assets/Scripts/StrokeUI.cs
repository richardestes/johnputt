using UnityEngine;
using TMPro;

public class StrokeUI : MonoBehaviour
{
    public static StrokeUI Instance { get; private set; }

    private TextMeshProUGUI label;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        label    = GetComponent<TextMeshProUGUI>();
    }

    // Called by EncounterManager whenever strokes change.
    // Replaces the Update() poll — no per-frame string allocation.
    public void Refresh()
    {
        int remaining = EncounterManager.Instance.MaxStrokes - EncounterManager.Instance.StrokesUsed;
        label.text    = remaining.ToString();
    }
}