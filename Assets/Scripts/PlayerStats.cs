using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Health")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Strokes")]
    [SerializeField] private int bonusMaxStrokes = 0;

    [Header("Power")]
    [SerializeField] private float powerMultiplier = 1f;

    [Header("Damage")]
    [SerializeField] private int damageBonus = 0;
    [SerializeField] private int bankShotDamageBonus = 0;

    public int  DamageBonus        => damageBonus;
    public int  BankShotDamageBonus => bankShotDamageBonus;

    // Per-encounter debuffs — cleared by OnEncounterStart()
    private int   strokeReductionDebuff = 0;
    private float powerDebuffMultiplier = 1f;

    public bool IsDead => currentHealth <= 0;

    public int BonusMaxStrokes =>
        Mathf.Max(0, bonusMaxStrokes - strokeReductionDebuff);

    public float EffectivePowerMultiplier =>
        powerMultiplier * powerDebuffMultiplier;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentHealth = maxHealth;
    }

    // ── Damage & Healing ───────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        DebugHUD.Log(currentHealth <= 0
            ? $"Player takes {amount} damage. You have been defeated!"
            : $"Player takes {amount} damage. ({currentHealth}/{maxHealth} HP remaining)");
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    // ── Debuffs ────────────────────────────────────────────────────

    public void ApplyStrokeReduction(int amount)
    {
        strokeReductionDebuff += amount;
    }

    public void ApplyPowerDebuff(int reductionPercent)
    {
        float factor = 1f - Mathf.Clamp01(reductionPercent / 100f);
        powerDebuffMultiplier = Mathf.Clamp(powerDebuffMultiplier * factor, 0.1f, 1f);
    }

    public void OnEncounterStart()
    {
        strokeReductionDebuff = 0;
        powerDebuffMultiplier = 1f;
    }

    // ── Permanent Upgrades ─────────────────────────────────────────

    public void AddMaxHealth(int amount)
    {
        maxHealth     += amount;
        currentHealth += amount;
    }

    public void AddPowerMultiplier(float amount)   => powerMultiplier      += amount;
    public void AddBonusMaxStrokes(int amount)     => bonusMaxStrokes      += amount;
    public void AddDamageBonus(int amount)         => damageBonus          += amount;
    public void AddBankShotBonus(int amount)       => bankShotDamageBonus  += amount;
}