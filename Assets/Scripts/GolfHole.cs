using UnityEngine;

public class GolfHole : MonoBehaviour
{
    public float detectionTolerance = 0.1f;

    private CircleCollider2D holeCollider;
    private float holeRadius;
    private bool isComplete = false;

    void Awake()
    {
        holeCollider = GetComponent<CircleCollider2D>();
        holeRadius   = holeCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isComplete || !other.CompareTag("Ball")) return;

        CircleCollider2D ballCollider = other.GetComponent<CircleCollider2D>();
        if (ballCollider == null) return;

        float ballRadius = ballCollider.radius * Mathf.Max(other.transform.localScale.x, other.transform.localScale.y);
        float distance   = Vector2.Distance(transform.position, other.transform.position);

        if (distance + ballRadius <= holeRadius + detectionTolerance)
        {
            isComplete = true;
            EncounterManager.Instance.OnBallHoled(); // was RegisterScore()
        }
    }
}