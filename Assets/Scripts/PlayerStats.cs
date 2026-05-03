using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Health")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Balls")]
    public int maxBalls = 20;
    public int currentBalls;

    [Header("Power")]
    [SerializeField] private float powerMultiplier = 1f;

    [Header("Damage")]
    [SerializeField] private int minDamage           = 3;
    [SerializeField] private int damageBonus         = 0;
    [SerializeField] private int bankShotDamageBonus = 0;

    public int  MinDamage           => minDamage;
    public int  DamageBonus         => damageBonus;
    public int  BankShotDamageBonus => bankShotDamageBonus;

    [Header("Mana Magnet")]
    [SerializeField] private float magnetRadius = 0f;
    public float MagnetRadius => magnetRadius;

    // Per-encounter debuffs — cleared by OnEncounterStart()
    private float powerDebuffMultiplier = 1f;

    public bool IsDead       => currentHealth <= 0;
    public bool IsOutOfBalls => currentBalls <= 0;

    public event System.Action OnHealthChanged;
    public event System.Action OnBallsChanged;

    public float EffectivePowerMultiplier =>
        powerMultiplier * powerDebuffMultiplier;

    // Cached inspector values — restored on reset
    private int   _baseMaxHealth;
    private int   _baseMaxBalls;
    private float _basePowerMultiplier;
    private int   _baseMinDamage;
    private int   _baseDamageBonus;
    private int   _baseBankShotDamageBonus;
    private float _baseMagnetRadius;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _baseMaxHealth           = maxHealth;
        _baseMaxBalls            = maxBalls;
        _basePowerMultiplier     = powerMultiplier;
        _baseMinDamage           = minDamage;
        _baseDamageBonus         = damageBonus;
        _baseBankShotDamageBonus = bankShotDamageBonus;
        _baseMagnetRadius        = magnetRadius;

        currentHealth = maxHealth;
        currentBalls  = maxBalls;
    }

    // ── Reset ──────────────────────────────────────────────────────

    public void ResetForNewRun()
    {
        maxHealth           = _baseMaxHealth;
        maxBalls            = _baseMaxBalls;
        powerMultiplier     = _basePowerMultiplier;
        minDamage           = _baseMinDamage;
        damageBonus         = _baseDamageBonus;
        bankShotDamageBonus = _baseBankShotDamageBonus;
        magnetRadius        = _baseMagnetRadius;
        powerDebuffMultiplier = 1f;
        currentHealth       = maxHealth;
        currentBalls        = maxBalls;
        OnHealthChanged?.Invoke();
        OnBallsChanged?.Invoke();
    }

    // ── Damage & Healing ───────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke();

        var screenPos = CombatViewController.Instance?.GetPlayerScreenPos() ?? Vector2.zero;
        screenPos.x += 50; // offset 
        DamageNumbers.Instance?.Spawn($"-{amount}", screenPos, Color.red);

        DebugHUD.Log(currentHealth <= 0
            ? $"Player takes {amount} damage. You have been defeated!"
            : $"Player takes {amount} damage. ({currentHealth}/{maxHealth} HP remaining)");
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke();
    }

    // ── Ball Bucket ────────────────────────────────────────────────

    public void SpendBall()
    {
        currentBalls = Mathf.Max(0, currentBalls - 1);
        OnBallsChanged?.Invoke();
    }

    public void RestoreBalls(int amount)
    {
        currentBalls = Mathf.Min(maxBalls, currentBalls + amount);
        OnBallsChanged?.Invoke();
        if (amount > 0) DebugHUD.Log($"Recovered {amount} ball(s). ({currentBalls}/{maxBalls} remaining)");
    }

    public void AddMaxBalls(int amount)
    {
        maxBalls     += amount;
        currentBalls  = Mathf.Min(maxBalls, currentBalls + amount);
        OnBallsChanged?.Invoke();
    }

    // ── Debuffs ────────────────────────────────────────────────────

    public void ApplyPowerDebuff(int reductionPercent)
    {
        float factor = 1f - Mathf.Clamp01(reductionPercent / 100f);
        powerDebuffMultiplier = Mathf.Clamp(powerDebuffMultiplier * factor, 0.1f, 1f);
    }

    public void OnEncounterStart()
    {
        powerDebuffMultiplier = 1f;
    }

    // ── Permanent Upgrades ─────────────────────────────────────────

    public void AddMaxHealth(int amount)         { maxHealth += amount; currentHealth += amount; }
    public void AddPowerMultiplier(float amount) => powerMultiplier  += amount;
    public void AddDamageBonus(int amount)       => damageBonus      += amount;
    public void AddBankShotBonus(int amount)     => bankShotDamageBonus += amount;
    public void AddMagnetRadius(float amount)    => magnetRadius        += amount;
}
