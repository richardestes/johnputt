using System.Collections;
using UnityEngine;

public class GolfHole : MonoBehaviour
{
    public float detectionTolerance = 0.1f;
    [SerializeField] float sinkDuration = 0.35f;

    private CircleCollider2D holeCollider;
    private float holeRadius;
    private bool  isComplete = false;

    void Awake()
    {
        holeCollider = GetComponent<CircleCollider2D>();
        holeRadius   = holeCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isComplete || !other.CompareTag("Ball")) return;

        var ballCollider = other.GetComponent<CircleCollider2D>();
        if (ballCollider == null) return;

        float ballRadius = ballCollider.radius * Mathf.Max(other.transform.localScale.x, other.transform.localScale.y);
        float distance   = Vector2.Distance(transform.position, other.transform.position);

        if (distance + ballRadius <= holeRadius + detectionTolerance)
        {
            isComplete = true;
            StartCoroutine(SinkBall(other.gameObject));
        }
    }

    IEnumerator SinkBall(GameObject ball)
    {
        // Freeze physics
        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.isKinematic = true; }

        // Prevent shooter from firing OnBallStopped during animation
        var shooter = ball.GetComponent<GolfBallShooter>();
        if (shooter != null) shooter.enabled = false;

        Vector3 startPos   = ball.transform.position;
        Vector3 targetPos  = new Vector3(transform.position.x, transform.position.y, ball.transform.position.z);
        Vector3 startScale = ball.transform.localScale;
        float   elapsed    = 0f;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sinkDuration);
            float e = t * t; // ease-in — accelerates into the hole
            ball.transform.position   = Vector3.Lerp(startPos,   targetPos,  e);
            ball.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, e);
            yield return null;
        }

        ball.transform.localScale = Vector3.zero;
        EncounterManager.Instance.OnBallHoled();
    }
}
