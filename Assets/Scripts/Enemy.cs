using UnityEngine;

public enum EnemyAttackType
{
    DealDamage,
    ReduceStrokes,
    ApplyDebuff
}

[System.Serializable]
public class EnemyAttackDefinition
{
    public EnemyAttackType type;
    [Tooltip("DealDamage: HP amount | ReduceStrokes: stroke count | ApplyDebuff: % power reduction (0-100)")]
    public int value = 5;
    public string description = "???";
}

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth;
    private int currentHealth;

    [Header("Attacks")]
    [SerializeField] private EnemyAttackDefinition[] possibleAttacks;

    [Header("Buff")]
    [Range(0f, 1f)]
    [SerializeField] private float buffChance = 0.3f;
    [SerializeField] private int buffDamageBonus = 5;

    private int pendingDamageBonus = 0;

    public bool IsDead        => currentHealth <= 0;
    public int  CurrentHealth => currentHealth;

    // ── Setup ──────────────────────────────────────────────────────

    public void Initialize()
    {
        currentHealth = maxHealth;
        DebugHUD.Log($"A wild enemy appears! ({currentHealth} HP)");
    }

    // ── Damage ─────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        DebugHUD.Log(currentHealth <= 0
            ? $"Enemy takes {amount} damage. Enemy is defeated!"
            : $"Enemy takes {amount} damage. ({currentHealth}/{maxHealth} HP remaining)");
    }

    // ── Buff ───────────────────────────────────────────────────────

    public bool TryBuff()
    {
        if (Random.value > buffChance) return false;
        pendingDamageBonus += buffDamageBonus;
        DebugHUD.Log($"Enemy is powering up! (+{buffDamageBonus} to next attack)");
        return true;
    }

    // ── Attack ─────────────────────────────────────────────────────

    public void ExecuteAttack(PlayerStats target)
    {
        if (possibleAttacks == null || possibleAttacks.Length == 0)
        {
            Debug.LogWarning($"[Enemy] {name} has no attacks configured!");
            return;
        }

        var attack       = possibleAttacks[Random.Range(0, possibleAttacks.Length)];
        int effectiveVal = attack.value + (attack.type == EnemyAttackType.DealDamage ? pendingDamageBonus : 0);
        pendingDamageBonus = 0;

        DebugHUD.Log($"Enemy uses {attack.description}!");

        switch (attack.type)
        {
            case EnemyAttackType.DealDamage:
                target.TakeDamage(effectiveVal);
                break;
            case EnemyAttackType.ReduceStrokes:
                target.ApplyStrokeReduction(effectiveVal);
                break;
            case EnemyAttackType.ApplyDebuff:
                target.ApplyPowerDebuff(effectiveVal);
                break;
        }
    }

    // Act() kept so nothing else in your codebase breaks if it's referenced elsewhere
    public void Act()
    {
        ExecuteAttack(PlayerStats.Instance);
    }
}