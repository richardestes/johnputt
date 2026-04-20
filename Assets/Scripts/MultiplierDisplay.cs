using TMPro;
using UnityEngine;

public class MultiplierDisplay : MonoBehaviour
{
    public static MultiplierDisplay Instance { get; private set; }

    [SerializeField] float minFontSize = 18f;
    [SerializeField] float maxFontSize = 72f;

    private TextMeshProUGUI label;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        label    = GetComponent<TextMeshProUGUI>();
    }

    public void Refresh()
    {
        var em = EncounterManager.Instance;
        if (em == null || label == null) return;

        int strokes = em.StrokesRemaining;
        int max     = em.MaxStrokes;

        float t        = max > 1 ? Mathf.Clamp01((float)(strokes - 1) / (max - 1)) : 1f;
        label.fontSize = Mathf.Lerp(minFontSize, maxFontSize, t);
        label.text     = $"x{strokes}";
    }
}
