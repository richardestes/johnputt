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
    public Transform enemySpawnPoint;

    [Header("Player Attack Scaling")]
    [Tooltip("Max damage at full wind-back. Scales down linearly to PlayerStats.MinDamage at minimum drag.")]
    [SerializeField] private int baseDamage = 10;

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

    private int             currentEnemyIndex;
    private int             strokesAtShot;
    private Enemy[]         spawnedEnemies;
    public  Enemy[]         SpawnedEnemies => spawnedEnemies;

    public struct AttackDetail
    {
        public int   Base;
        public int   DamageBonus;
        public float Multiplier;
        public int   BankShotBonus;
        public int   Total;
    }
    public AttackDetail LastAttack { get; private set; }
    private Enemy           CurrentEnemy   => spawnedEnemies[currentEnemyIndex];

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
        var ps = PlayerStats.Instance;
        ps.OnEncounterStart();
        Debug.Log($"[EncounterManager] Start — base={PendingHole.baseMaxStrokes} bonusMax={ps.BonusMaxStrokes} (permanent={ps.DebugPermanentBonus} nextHole={ps.DebugNextHoleBonus})");
        MaxStrokes = PendingHole.baseMaxStrokes + ps.BonusMaxStrokes;
        ps.ConsumeNextHoleBonusStrokes();
        currentEnemyIndex = 0;
        SpawnEnemies();
        RefreshSpawnPoint();
        SpawnBall();
        CombatViewController.Instance?.Initialize(spawnedEnemies);
        TransitionTo(EncounterState.PlayerTurn);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (currentBall != null) Destroy(currentBall);
        if (spawnedEnemies != null)
            foreach (var e in spawnedEnemies)
                if (e != null) Destroy(e.gameObject);
    }

    // ── Enemy Helpers ──────────────────────────────────────────────

    private void SpawnEnemies()
    {
        var defs = PendingEncounter.enemies;
        spawnedEnemies = new Enemy[defs.Length];
        Vector3 spawnPos = enemySpawnPoint != null ? enemySpawnPoint.position : Vector3.zero;

        for (int i = 0; i < defs.Length; i++)
        {
            var e = Instantiate(defs[i].prefab, spawnPos, Quaternion.identity).GetComponent<Enemy>();
            e.Initialize();
            spawnedEnemies[i] = e;
        }
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
        MultiplierDisplay.Instance?.Refresh();
    }

    // Called by GolfBallShooter on fire
    public void RegisterStroke()
    {
        if (State != EncounterState.PlayerTurn) return;

        strokesAtShot = StrokesRemaining;
        StrokesUsed++;
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
        var   ps         = PlayerStats.Instance;
        float shotPower  = currentShooter != null ? currentShooter.LastShotPower : 1f;
        int   rawBase    = Mathf.Max(ps.MinDamage, Mathf.RoundToInt(baseDamage * shotPower));
        int   multiplier = Mathf.Max(1, strokesAtShot);
        int   scaled     = rawBase * multiplier;
        int   bankBonus  = (currentShooter != null && currentShooter.HitObstacle) ? ps.BankShotDamageBonus : 0;
        int   damage     = scaled + ps.DamageBonus + bankBonus;

        LastAttack = new AttackDetail
        {
            Base          = rawBase,
            DamageBonus   = ps.DamageBonus,
            Multiplier    = multiplier,
            BankShotBonus = bankBonus,
            Total         = damage
        };

        if (bankBonus > 0) DebugHUD.Log($"Bank shot! +{bankBonus} bonus damage.");
        DebugHUD.Log($"Player deals {damage} damage.");
        CurrentEnemy.TakeDamage(damage);
        AttackDisplay.Instance?.Show(LastAttack);

        yield return new WaitForSeconds(betweenActionsDelay);
        TransitionTo(EncounterState.EnemyTurn);
    }

    // ── EnemyTurn ──────────────────────────────────────────────────

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(enemyTurnDelay);

        foreach (var enemy in spawnedEnemies)
        {
            if (enemy.IsDead) continue;

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
        if (System.Array.TrueForAll(spawnedEnemies, e => e.IsDead))
        {
            TransitionTo(EncounterState.Reward);
            return;
        }

        if (PlayerStats.Instance.IsDead)
        {
            TransitionTo(EncounterState.GameOver);
            return;
        }

        // Retarget if current target is dead
        if (CurrentEnemy.IsDead)
        {
            for (int i = 0; i < spawnedEnemies.Length; i++)
            {
                if (!spawnedEnemies[i].IsDead) { currentEnemyIndex = i; break; }
            }
        }

        // Advance to next hole
        StrokesUsed = 0;
        GameManager.Instance.LoadNextLevel();
        RefreshSpawnPoint();
        MaxStrokes = PendingHole.baseMaxStrokes + PlayerStats.Instance.BonusMaxStrokes;
        PlayerStats.Instance.ConsumeNextHoleBonusStrokes();
        Debug.Log($"[EncounterManager] New hole — MaxStrokes={MaxStrokes}");
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