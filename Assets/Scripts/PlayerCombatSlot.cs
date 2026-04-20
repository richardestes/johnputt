using UnityEngine;

public class PlayerCombatSlot : MonoBehaviour
{
    [SerializeField] SpriteRenderer portrait;
    [SerializeField] HealthBar       healthBar;

    void OnEnable()
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.OnHealthChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHealthChanged -= Refresh;
    }

    void Refresh()
    {
        var ps = PlayerStats.Instance;
        if (ps == null || healthBar == null) return;
        healthBar.SetValue(ps.currentHealth, ps.maxHealth);
    }
}
