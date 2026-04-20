using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [SerializeField] private RewardDefinition[] rewardPool;
    [SerializeField] private int rewardChoices = 3;

    private RewardDefinition[] currentOffers;
    private bool showing = false;

    private GUIStyle _boxStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _labelStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ShowRewards()
    {
        currentOffers = PickOffers();
        showing = true;
    }

    private RewardDefinition[] PickOffers()
    {
        if (rewardPool == null || rewardPool.Length == 0) return new RewardDefinition[0];

        int count = Mathf.Min(rewardChoices, rewardPool.Length);
        var pool  = new System.Collections.Generic.List<RewardDefinition>(rewardPool);
        var picks = new RewardDefinition[count];

        for (int i = 0; i < count; i++)
        {
            int idx   = Random.Range(0, pool.Count);
            picks[i]  = pool[idx];
            pool.RemoveAt(idx);
        }
        return picks;
    }

    private void OnGUI()
    {
        if (!showing || currentOffers == null) return;

        InitStyles();

        float cardW  = 220f;
        float cardH  = 110f;
        float pad    = 20f;
        float totalW = currentOffers.Length * cardW + (currentOffers.Length - 1) * pad;
        float startX = (Screen.width  - totalW) / 2f;
        float startY = (Screen.height - cardH)  / 2f;

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _boxStyle);

        for (int i = 0; i < currentOffers.Length; i++)
        {
            float x      = startX + i * (cardW + pad);
            var   reward = currentOffers[i];

            GUI.Box(new Rect(x, startY, cardW, cardH), GUIContent.none);
            GUI.Label(new Rect(x + 10, startY + 10, cardW - 20, 30), reward.displayName, _labelStyle);
            GUI.Label(new Rect(x + 10, startY + 40, cardW - 20, 40), reward.description, GUI.skin.label);

            if (GUI.Button(new Rect(x + 10, startY + cardH - 35, cardW - 20, 28), "Choose", _buttonStyle))
                Select(reward);
        }
    }

    private void Select(RewardDefinition reward)
    {
        showing = false;
        Apply(reward);
        DebugHUD.Log($"Reward chosen: {reward.displayName}");
        GameManager.Instance.ReturnToMap();
    }

    private void Apply(RewardDefinition reward)
    {
        switch (reward.type)
        {
            case RewardType.DamageBonus:
                PlayerStats.Instance.AddDamageBonus(reward.value);
                break;
            case RewardType.ExtraStrokes:
                PlayerStats.Instance.AddNextHoleBonusStrokes(reward.value);
                break;
            case RewardType.BankShotBonus:
                PlayerStats.Instance.AddBankShotBonus(reward.value);
                break;
        }
    }

    private void InitStyles()
    {
        if (_labelStyle != null) return;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 16,
            fontStyle = FontStyle.Bold,
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
        };

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0, 0, 0, 0.6f)) }
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
