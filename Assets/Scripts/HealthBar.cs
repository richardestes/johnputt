using TMPro;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] RectTransform fillRect;
    [SerializeField] TMP_Text      label;

    public void Init(RectTransform fill, TMP_Text labelText = null)
    {
        fillRect = fill;
        label    = labelText;
    }

    public void SetValue(int current, int max)
    {
        if (fillRect == null) { Debug.LogWarning("[HealthBar] FillRect not set.", this); return; }
        float t = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        fillRect.anchorMax = new Vector2(t, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        if (label != null) label.text = $"{current} / {max}";
    }
}
