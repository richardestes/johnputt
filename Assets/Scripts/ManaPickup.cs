using UnityEngine;

public class ManaPickup : MonoBehaviour
{
    [SerializeField] private float attractSpeed    = 8f;
    [SerializeField] private float collectSpeed    = 18f;
    [SerializeField] private float collectDistance = 0.15f;

    private Transform ballTransform;
    private bool      isCollecting;

    private void Update()
    {
        if (ballTransform == null)
        {
            var ballObj = EncounterManager.Instance?.CurrentBall;
            if (ballObj == null) return;
            ballTransform = ballObj.transform;
        }

        if (isCollecting)
        {
            if (ballTransform == null) { Destroy(gameObject); return; }

            transform.position = Vector2.MoveTowards(
                transform.position, ballTransform.position, collectSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, ballTransform.position) <= collectDistance)
            {
                EncounterManager.Instance?.CollectMana();
                Destroy(gameObject);
            }
            return;
        }

        var ps = PlayerStats.Instance;
        if (ps == null || ps.MagnetRadius <= 0f) return;

        if (Vector2.Distance(transform.position, ballTransform.position) <= ps.MagnetRadius)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, ballTransform.position, attractSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Ball") || isCollecting) return;
        isCollecting = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}
