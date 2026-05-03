using TMPro;
using UnityEngine;

public class BallDisplay : MonoBehaviour
{
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowColor    = new Color(1f, 0.25f, 0.25f);
    [SerializeField] [Range(0f, 1f)] private float lowThreshold = 0.25f;

    private TextMeshProUGUI label;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        ps.OnBallsChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnBallsChanged -= Refresh;
    }

    void Refresh()
    {
        var ps = PlayerStats.Instance;
        if (ps == null || label == null) return;

        label.text  = $"{ps.currentBalls} / {ps.maxBalls}";
        float ratio = ps.maxBalls > 0 ? (float)ps.currentBalls / ps.maxBalls : 0f;
        label.color = ratio <= lowThreshold ? lowColor : normalColor;
    }
}
