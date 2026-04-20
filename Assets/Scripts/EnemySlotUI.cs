using TMPro;
using UnityEngine;

public class EnemySlotUI : MonoBehaviour
{
    [SerializeField] SpriteRenderer portrait;
    [SerializeField] HealthBar       healthBar;
    [SerializeField] TMP_Text        nameLabel;

    private Enemy target;

    public void Bind(Enemy enemy)
    {
        if (target != null) target.OnHealthChanged -= Refresh;
        target = enemy;
        target.OnHealthChanged += Refresh;
        if (portrait  != null) portrait.sprite = enemy.Portrait;
        if (nameLabel != null) nameLabel.text  = enemy.name;
        Refresh();
    }

    void OnDestroy()
    {
        if (target != null) target.OnHealthChanged -= Refresh;
    }

    void Refresh()
    {
        if (healthBar == null) return;
        healthBar.SetValue(target.CurrentHealth, target.maxHealth);
        if (portrait != null) portrait.color = new Color(1, 1, 1, target.IsDead ? 0.35f : 1f);
    }
}
