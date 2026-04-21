using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Health")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Strokes")]
    [SerializeField] private int bonusMaxStrokes = 0;
    private int nextHoleBonusStrokes = 0;

    [Header("Power")]
    [SerializeField] private float powerMultiplier = 1f;

    [Header("Damage")]
    [SerializeField] private int minDamage           = 3;
    [SerializeField] private int damageBonus         = 0;
    [SerializeField] private int bankShotDamageBonus = 0;

    public int  MinDamage           => minDamage;
    public int  DamageBonus         => damageBonus;
    public int  BankShotDamageBonus => bankShotDamageBonus;

    // Per-encounter debuffs — cleared by OnEncounterStart()
    private int   strokeReductionDebuff = 0;
    private float powerDebuffMultiplier = 1f;

    public bool IsDead => currentHealth <= 0;

    public event System.Action OnHealthChanged;

    public int BonusMaxStrokes =>
        Mathf.Max(0, bonusMaxStrokes + nextHoleBonusStrokes - strokeReductionDebuff);

    public int DebugPermanentBonus => bonusMaxStrokes;
    public int DebugNextHoleBonus  => nextHoleBonusStrokes;

    public float EffectivePowerMultiplier =>
        powerMultiplier * powerDebuffMultiplier;

    // Cached inspector values — restored on reset
    private int   _baseMaxHealth;
    private int   _baseBonusMaxStrokes;
    private float _basePowerMultiplier;
    private int   _baseMinDamage;
    private int   _baseDamageBonus;
    private int   _baseBankShotDamageBonus;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _baseMaxHealth           = maxHealth;
        _baseBonusMaxStrokes     = bonusMaxStrokes;
        _basePowerMultiplier     = powerMultiplier;
        _baseMinDamage           = minDamage;
        _baseDamageBonus         = damageBonus;
        _baseBankShotDamageBonus = bankShotDamageBonus;

        currentHealth = maxHealth;
    }

    // ── Reset ──────────────────────────────────────────────────────

    public void ResetForNewRun()
    {
        maxHealth            = _baseMaxHealth;
        bonusMaxStrokes      = _baseBonusMaxStrokes;
        nextHoleBonusStrokes = 0;
        powerMultiplier      = _basePowerMultiplier;
        minDamage            = _baseMinDamage;
        damageBonus          = _baseDamageBonus;
        bankShotDamageBonus  = _baseBankShotDamageBonus;
        strokeReductionDebuff = 0;
        powerDebuffMultiplier = 1f;
        currentHealth        = maxHealth;
        OnHealthChanged?.Invoke();
    }

    // ── Damage & Healing ───────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke();
        DebugHUD.Log(currentHealth <= 0
            ? $"Player takes {amount} damage. You have been defeated!"
            : $"Player takes {amount} damage. ({currentHealth}/{maxHealth} HP remaining)");
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke();
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

    public void AddMaxHealth(int amount)         { maxHealth += amount; currentHealth += amount; }
    public void AddPowerMultiplier(float amount) => powerMultiplier      += amount;
    public void AddBonusMaxStrokes(int amount)   => bonusMaxStrokes      += amount;
    public void AddNextHoleBonusStrokes(int amount)  => nextHoleBonusStrokes += amount;
    public void ConsumeNextHoleBonusStrokes()        => nextHoleBonusStrokes  = 0;
    public void AddDamageBonus(int amount)       => damageBonus          += amount;
    public void AddBankShotBonus(int amount)     => bankShotDamageBonus  += amount;
}
