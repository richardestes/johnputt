using UnityEngine;

public class PlayerCombatSlot : MonoBehaviour
{
    [SerializeField] SpriteRenderer portrait;
    [SerializeField] HealthBar       healthBar;
    [SerializeField] HealthBar       manaBar;

    void OnEnable()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnHealthChanged += RefreshHealth;
            RefreshHealth();
        }

        if (EncounterManager.Instance != null)
        {
            EncounterManager.Instance.OnManaChanged += RefreshMana;
            RefreshMana();
        }
    }

    void OnDisable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHealthChanged -= RefreshHealth;

        if (EncounterManager.Instance != null)
            EncounterManager.Instance.OnManaChanged -= RefreshMana;
    }

    void RefreshHealth()
    {
        var ps = PlayerStats.Instance;
        if (ps == null || healthBar == null) return;
        healthBar.SetValue(ps.currentHealth, ps.maxHealth);
    }

    void RefreshMana()
    {
        var em = EncounterManager.Instance;
        if (em == null || manaBar == null) return;
        manaBar.SetValue(em.CurrentMana, em.ManaRequired);
    }
}
