using System.Collections;
using TMPro;
using UnityEngine;

public class EnemySlotUI : MonoBehaviour
{
    [SerializeField] SpriteRenderer portrait;
    [SerializeField] HealthBar       healthBar;
    [SerializeField] TMP_Text        nameLabel;
    [SerializeField] float           fadeDuration = 1f;

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

        if (target.IsDead)
        {
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (nameLabel != null) nameLabel.gameObject.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeOut()
    {
        if (portrait == null) yield break;

        float elapsed    = 0f;
        Color startColor = portrait.color;

        while (elapsed < fadeDuration)
        {
            elapsed        += Time.deltaTime;
            float alpha     = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            portrait.color  = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
