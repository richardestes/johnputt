using UnityEngine;
using UnityEngine.InputSystem;

public class GolfBallShooter : MonoBehaviour
{
    [Header("Shot Settings")]
    public float maxDragDistance = 3f;
    public float shotPowerMultiplier = 8f;

    [Header("References")]
    public AimLineRenderer aimLine;

    private Rigidbody2D rb;
    private Camera mainCam;

    private bool isDragging = false;
    private Vector2 dragStartWorld;
    private bool ballInMotion = false;

    public bool HitObstacle { get; private set; }

    private InputAction clickAction;
    private Mouse mouse;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        mouse = Mouse.current;

        clickAction = new InputAction("Click", binding: "<Mouse>/leftButton");
        clickAction.performed += OnMouseDown;
        clickAction.canceled += OnMouseUp;
    }

    void OnEnable()  => clickAction.Enable();
    void OnDisable() => clickAction.Disable();

    void OnDestroy()
    {
        clickAction.performed -= OnMouseDown;
        clickAction.canceled  -= OnMouseUp;
        clickAction.Dispose();
    }

    void OnMouseDown(InputAction.CallbackContext ctx)
    {
        if (ballInMotion) return;
        isDragging = true;
        dragStartWorld = mainCam.ScreenToWorldPoint(mouse.position.ReadValue());
    }

    void OnMouseUp(InputAction.CallbackContext ctx)
    {
        if (!isDragging) return;

        Vector2 currentWorld  = mainCam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 rawDrag       = dragStartWorld - currentWorld;
        Vector2 clampedDrag   = Vector2.ClampMagnitude(rawDrag, maxDragDistance);

        Shoot(clampedDrag);
        aimLine.Hide();
        isDragging = false;
    }

    void FixedUpdate()
    {
        if (!ballInMotion) return;

        if (IsOutOfBounds())
        {
            rb.linearVelocity = Vector2.zero;
            ballInMotion = false;
            EncounterManager.Instance.OnBallOutOfBounds();
            return;
        }

        if (rb.linearVelocity.magnitude < 0.3f)
        {
            rb.linearVelocity = Vector2.zero;
            ballInMotion = false;
            EncounterManager.Instance.OnBallStopped();
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (ballInMotion && col.gameObject.CompareTag("Obstacle"))
            HitObstacle = true;
    }

    private bool IsOutOfBounds()
    {
        Vector3 vp = mainCam.WorldToViewportPoint(transform.position);
        return vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
    }

    void Update()
    {
        if (ballInMotion) return;

        if (isDragging)
        {
            Vector2 currentWorld = mainCam.ScreenToWorldPoint(mouse.position.ReadValue());
            Vector2 rawDrag      = dragStartWorld - currentWorld;
            Vector2 clampedDrag  = Vector2.ClampMagnitude(rawDrag, maxDragDistance);
            aimLine.Show(transform.position, clampedDrag, maxDragDistance);
        }
    }

    void Shoot(Vector2 dragVector)
    {
        if (dragVector.magnitude < 0.1f) return;

        float t         = dragVector.magnitude / maxDragDistance;
        float power     = Mathf.Sqrt(t) * PlayerStats.Instance.EffectivePowerMultiplier;
        HitObstacle  = false;
        rb.AddForce(-dragVector * power * shotPowerMultiplier, ForceMode2D.Impulse);
        ballInMotion = true;

        EncounterManager.Instance.RegisterStroke(); // was RegisterStroke, kept as-is
    }
}