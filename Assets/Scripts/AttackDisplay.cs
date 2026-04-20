using System.Collections;
using TMPro;
using UnityEngine;

public class AttackDisplay : MonoBehaviour
{
    public static AttackDisplay Instance { get; private set; }

    [SerializeField] TMP_Text formulaLabel;
    [SerializeField] TMP_Text totalLabel;
    [SerializeField] float    countDuration = 0.6f;
    [SerializeField] float    holdDuration  = 1.2f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Show(EncounterManager.AttackDetail attack)
    {
        StopAllCoroutines();
        formulaLabel.text = $"{attack.Base}  x  {(int)attack.Multiplier}";
        totalLabel.text   = "0";
        gameObject.SetActive(true);
        StartCoroutine(CountUp(attack.Total));
    }

    IEnumerator CountUp(int target)
    {
        float elapsed = 0f;
        while (elapsed < countDuration)
        {
            elapsed        += Time.deltaTime;
            float t         = Mathf.Clamp01(elapsed / countDuration);
            totalLabel.text = Mathf.RoundToInt(Mathf.Lerp(0, target, t)).ToString();
            yield return null;
        }
        totalLabel.text = target.ToString();
        yield return new WaitForSeconds(holdDuration);
        gameObject.SetActive(false);
    }
}
