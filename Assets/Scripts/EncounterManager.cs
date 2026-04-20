using System.Collections;
using UnityEngine;

// Defined outside the class so other scripts can reference it without qualification
public enum EncounterState
{
    PlayerTurn,
    BallRolling,
    PlayerAttack,
    EnemyTurn,
    CheckEnd,
    Reward,
    GameOver
}

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance { get; private set; }
    public static HoleDefinition     PendingHole     { get; set; }
    public static EncounterDefinition PendingEncounter { get; set; }
    public static Transform           PendingSpawnPoint { get; set; }

    [Header("References")]
    public Enemy enemy;

    [Header("Player Attack Scaling")]
    [Tooltip("Base damage dealt on a successful hole.")]
    [SerializeField] private int baseDamage = 10;
    [Tooltip("Damage multiplier added per stroke remaining. 0.5 → hole with 4 left = 3x damage.")]
    [SerializeField] private float multiplierPerRemainingStroke = 0.5f;

    [Header("Timing")]
    [SerializeField] private float enemyTurnDelay      = 0.8f;
    [SerializeField] private float betweenActionsDelay = 0.5f;

    [Header("Ball")]
    public GameObject ballPrefab;

    // ── State ──────────────────────────────────────────────────────

    public EncounterState State         { get; private set; }
    public int            MaxStrokes    { get; private set; }
    public int            StrokesUsed   { get; private set; }
    public int            StrokesRemaining => MaxStrokes - StrokesUsed;

    // ── Private ────────────────────────────────────────────────────

    private GameObject      currentBall;
    private GolfBallShooter currentShooter;
    private Transform       ballSpawnPoint;

    // ── Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (PendingHole == null || PendingEncounter == null)
        {
            Debug.LogWarning("[EncounterManager] No PendingHole or PendingEncounter set.");
            return;
        }
        MaxStrokes = PendingHole.baseMaxStrokes + PlayerStats.Instance.BonusMaxStrokes;
        enemy.Initialize(PendingEncounter.enemyMaxHealth, PendingEncounter.enemyDamage);
        RefreshSpawnPoint();
        SpawnBall();
        TransitionTo(EncounterState.PlayerTurn);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (currentBall != null) Destroy(currentBall);
    }

    // ── Level Helpers ──────────────────────────────────────────────

    // Finds the BallSpawnPoint that GameManager already placed in the scene
    private void RefreshSpawnPoint()
    {
        if (PendingSpawnPoint != null)
            ballSpawnPoint = PendingSpawnPoint;
        else
            Debug.LogWarning("[EncounterManager] PendingSpawnPoint not set.");
    }

    private void SpawnBall()
    {
        if (currentBall != null) Destroy(currentBall);

        Vector3 pos = ballSpawnPoint != null ? ballSpawnPoint.position : Vector3.zero;
        currentBall    = Instantiate(ballPrefab, pos, Quaternion.identity);
        currentShooter = currentBall.GetComponent<GolfBallShooter>();
        SetShooterEnabled(false);
    }

    private void SetShooterEnabled(bool value)
    {
        if (currentShooter != null) currentShooter.enabled = value;
    }

    // ══════════════════════════════════════════════════════════════
    //  STATE MACHINE
    // ══════════════════════════════════════════════════════════════

    private void TransitionTo(EncounterState next)
    {
        if (next != State)
        {
            switch (next)
            {
                case EncounterState.PlayerTurn:   DebugHUD.Log("Your turn.");                  break;
                case EncounterState.EnemyTurn:    DebugHUD.Log("The enemy stirs...");          break;
                case EncounterState.PlayerAttack: DebugHUD.Log("Ball holed!");                 break;
                case EncounterState.Reward:       DebugHUD.Log("Victory! Choose your reward."); break;
            }
        }

        State = next;
        switch (next)
        {
            case EncounterState.PlayerTurn:   EnterPlayerTurn();                         break;
            case EncounterState.BallRolling:  /* ballInMotion flag blocks input */        break;
            case EncounterState.PlayerAttack: StartCoroutine(PlayerAttackRoutine());     break;
            case EncounterState.EnemyTurn:    SetShooterEnabled(false);
                                              StartCoroutine(EnemyTurnRoutine());        break;
            case EncounterState.CheckEnd:     EnterCheckEnd();                           break;
            case EncounterState.Reward:       EnterReward();                             break;
            case EncounterState.GameOver:     EnterGameOver();                           break;
        }
    }

    // ── PlayerTurn ─────────────────────────────────────────────────

    private void EnterPlayerTurn()
    {
        SetShooterEnabled(true);
        StrokeUI.Instance?.Refresh();
    }

    // Called by GolfBallShooter on fire
    public void RegisterStroke()
    {
        if (State != EncounterState.PlayerTurn) return;

        StrokesUsed++;
        StrokeUI.Instance?.Refresh();
        TransitionTo(EncounterState.BallRolling);
    }

    // ── BallRolling callbacks ──────────────────────────────────────

    // Called by GolfHole when ball fully enters
    public void OnBallHoled()
    {
        if (State != EncounterState.BallRolling) return;
        TransitionTo(EncounterState.PlayerAttack);
    }

    // Called by GolfBallShooter when velocity drops below stop threshold
    public void OnBallStopped()
    {
        if (State != EncounterState.BallRolling) return;

        if (StrokesRemaining > 0)
            TransitionTo(EncounterState.PlayerTurn);
        else
            TransitionTo(EncounterState.EnemyTurn);
    }

    // Called by GolfBallShooter when ball leaves the camera viewport
    public void OnBallOutOfBounds()
    {
        if (State != EncounterState.BallRolling) return;

        const int penalty = 2;
        StrokesUsed = Mathf.Min(StrokesUsed + penalty, MaxStrokes);
        DebugHUD.Log($"Out of bounds! -{penalty} strokes. ({StrokesRemaining} remaining)");

        if (StrokesRemaining > 0)
        {
            SpawnBall();
            TransitionTo(EncounterState.PlayerTurn);
        }
        else
            TransitionTo(EncounterState.EnemyTurn);
    }

    // ── PlayerAttack ───────────────────────────────────────────────

    private IEnumerator PlayerAttackRoutine()
    {
        float multiplier = 1f + StrokesRemaining * multiplierPerRemainingStroke;
        int   damage     = Mathf.RoundToInt((baseDamage + PlayerStats.Instance.DamageBonus) * multiplier);

        bool isBankShot = currentShooter != null && currentShooter.HitObstacle;
        if (isBankShot)
        {
            int bonus = PlayerStats.Instance.BankShotDamageBonus;
            damage += bonus;
            DebugHUD.Log($"Bank shot! +{bonus} bonus damage.");
        }

        DebugHUD.Log($"Player sinks the putt! Attacks for {damage} damage. ({StrokesRemaining} strokes remaining)");
        enemy.TakeDamage(damage);

        // TODO: damage popup / hit flash

        yield return new WaitForSeconds(betweenActionsDelay);
        TransitionTo(EncounterState.EnemyTurn);
    }

    // ── EnemyTurn ──────────────────────────────────────────────────

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(enemyTurnDelay);

        if (!enemy.IsDead)
        {
            if (enemy.TryBuff())
            {
                // TODO: buff VFX
                yield return new WaitForSeconds(betweenActionsDelay);
            }

            enemy.ExecuteAttack(PlayerStats.Instance);
            // TODO: camera shake / health bar flash

            yield return new WaitForSeconds(betweenActionsDelay);
        }

        TransitionTo(EncounterState.CheckEnd);
    }

    // ── CheckEnd ───────────────────────────────────────────────────

    private void EnterCheckEnd()
    {
        if (enemy.IsDead)
        {
            TransitionTo(EncounterState.Reward);
            return;
        }

        if (PlayerStats.Instance.IsDead)
        {
            TransitionTo(EncounterState.GameOver);
            return;
        }

        // Both alive — load next hole and go again
        StrokesUsed = 0;
        GameManager.Instance.LoadNextLevel();
        RefreshSpawnPoint();
        MaxStrokes = PendingHole.baseMaxStrokes + PlayerStats.Instance.BonusMaxStrokes;
        Debug.Log($"[EncounterManager] New hole — MaxStrokes={MaxStrokes}, StrokeUI={(StrokeUI.Instance == null ? "NULL" : "ok")}");
        SpawnBall();
        TransitionTo(EncounterState.PlayerTurn);
    }

    // ── Reward ─────────────────────────────────────────────────────

    private void EnterReward()
    {
        RewardManager.Instance?.ShowRewards();
    }

    // ── GameOver ───────────────────────────────────────────────────

    private void EnterGameOver()
    {
        DebugHUD.Log("Game Over. Your round has come to an end.");
        // TODO: game-over screen
    }
}