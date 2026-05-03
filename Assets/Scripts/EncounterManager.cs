using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    [SerializeField] private float ballRefundPercent = 0.25f;

    [Header("Mana")]
    [SerializeField] private GameObject manaPrefab;
    [FormerlySerializedAs("manaPerSpecialAttack")] [SerializeField] private int        manaRequired = 5;
    [SerializeField] private int        manaSpecialDamage    = 20;
    [SerializeField] private int        manaSpawnMin         = 10;
    [SerializeField] private int        manaSpawnMax         = 20;

    // ── State ──────────────────────────────────────────────────────

    public EncounterState State          { get; private set; }
    public int            Par            { get; private set; }
    public int            StrokesThisHole { get; private set; }
    public int            ParMultiplier  => Mathf.Max(1, Par - StrokesThisHole);

    // ── Private ────────────────────────────────────────────────────

    private int             currentEnemyIndex;
    private int             parMultiplierAtShot;
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

    public GameObject CurrentBall => currentBall;

    private readonly List<GameObject> manaObjects = new List<GameObject>();
    private int                       currentMana;

    public int CurrentMana  => currentMana;
    public int ManaRequired => manaRequired;
    public event System.Action OnManaChanged;

    // ── Lifecycle ──────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance          = null;
        PendingEncounter  = null;
        PendingSpawnPoint = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (PendingEncounter == null)
        {
            Debug.LogWarning("[EncounterManager] No PendingEncounter set.");
            return;
        }
        var ps = PlayerStats.Instance;
        ps.OnEncounterStart();
        Par = GameManager.Instance.RolledPars[GameManager.Instance.CurrentNodeIndex];
        StrokesThisHole = 0;
        currentEnemyIndex = 0;
        currentMana = 0;
        SpawnEnemies();
        RefreshSpawnPoint();
        SpawnMana();
        SpawnBall();
        CombatViewController.Instance?.Initialize(spawnedEnemies);
        TransitionTo(EncounterState.PlayerTurn);
    }

    private void SpawnMana()
    {
        if (manaPrefab == null) return;

        Camera levelCam = null;
        foreach (var cam in Camera.allCameras)
        {
            if (cam.rect.y < 0.1f && cam.rect.height < 0.6f)
            {
                levelCam = cam;
                break;
            }
        }

        if (levelCam == null)
        {
            Debug.LogWarning("[EncounterManager] Could not find level camera for mana spawn.");
            return;
        }

        float   halfH  = levelCam.orthographicSize;
        float   halfW  = halfH * levelCam.aspect;
        Vector2 center = levelCam.transform.position;
        int     count  = Random.Range(manaSpawnMin, manaSpawnMax + 1);

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(center.x - halfW, center.x + halfW),
                Random.Range(center.y - halfH, center.y + halfH)
            );
            manaObjects.Add(Instantiate(manaPrefab, pos, Quaternion.identity));
        }
    }

    private void ClearManaPickups()
    {
        foreach (var obj in manaObjects)
            if (obj) Destroy(obj);
        manaObjects.Clear();
    }

    private void ClearMana()
    {
        ClearManaPickups();
        currentMana = 0;
        OnManaChanged?.Invoke();
    }

    public void CollectMana()
    {
        currentMana++;
        OnManaChanged?.Invoke();
        DebugHUD.Log($"Mana! ({currentMana}/{manaRequired})");
        if (currentMana >= manaRequired)
        {
            currentMana = 0;
            OnManaChanged?.Invoke();
            TriggerManaSpecial();
        }
    }

    private void TriggerManaSpecial()
    {
        DebugHUD.Log($"Mana burst! All enemies take {manaSpecialDamage} damage!");
        foreach (var enemy in spawnedEnemies)
            if (!enemy.IsDead) enemy.TakeDamage(manaSpecialDamage);

        if (System.Array.TrueForAll(spawnedEnemies, e => e.IsDead))
            TransitionTo(EncounterState.CheckEnd);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (currentBall != null) Destroy(currentBall);
        if (spawnedEnemies != null)
            foreach (var e in spawnedEnemies)
                if (e != null) Destroy(e.gameObject);
        ClearMana();
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
        if (PendingSpawnPoint)
            ballSpawnPoint = PendingSpawnPoint;
        else
            Debug.LogWarning("[EncounterManager] PendingSpawnPoint not set.");
    }

    private void SpawnBall()
    {
        if (currentBall) Destroy(currentBall);

        Vector3 pos = ballSpawnPoint != null ? ballSpawnPoint.position : Vector3.zero;
        currentBall    = Instantiate(ballPrefab, pos, Quaternion.identity);
        currentShooter = currentBall.GetComponent<GolfBallShooter>();
        SetShooterEnabled(false);
    }

    private void SetShooterEnabled(bool value)
    {
        if (currentShooter) currentShooter.enabled = value;
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

        parMultiplierAtShot = ParMultiplier;
        StrokesThisHole++;
        PlayerStats.Instance.SpendBall();
        TransitionTo(EncounterState.BallRolling);
    }

    // ── BallRolling callbacks ──────────────────────────────────────

    // Called by GolfHole when ball fully enters
    public void OnBallHoled()
    {
        if (State != EncounterState.BallRolling) return;

        int refund = Mathf.CeilToInt(StrokesThisHole * ballRefundPercent);
        PlayerStats.Instance.RestoreBalls(refund);

        TransitionTo(EncounterState.PlayerAttack);
    }

    // Called by GolfBallShooter when velocity drops below stop threshold
    public void OnBallStopped()
    {
        if (State != EncounterState.BallRolling) return;

        if (!PlayerStats.Instance.IsOutOfBalls)
            TransitionTo(EncounterState.PlayerTurn);
        else
            TransitionTo(EncounterState.GameOver);
    }

    // Called by GolfBallShooter when ball leaves the camera viewport
    public void OnBallOutOfBounds()
    {
        if (State != EncounterState.BallRolling) return;

        var ps = PlayerStats.Instance;
        const int penalty = 2;
        for (int i = 0; i < penalty; i++) ps.SpendBall();
        DebugHUD.Log($"Out of bounds! -{penalty} balls. ({ps.currentBalls}/{ps.maxBalls} remaining)");

        if (!ps.IsOutOfBalls)
        {
            SpawnBall();
            TransitionTo(EncounterState.PlayerTurn);
        }
        else
            TransitionTo(EncounterState.GameOver);
    }

    // ── PlayerAttack ───────────────────────────────────────────────

    private IEnumerator PlayerAttackRoutine()
    {
        var   ps         = PlayerStats.Instance;
        float shotPower  = currentShooter ? currentShooter.LastShotPower : 1f;
        int   rawBase    = Mathf.Max(ps.MinDamage, Mathf.RoundToInt(baseDamage * shotPower));
        int   bankBonus  = (currentShooter && currentShooter.HitObstacle) ? ps.BankShotDamageBonus : 0;
        int   multiplier = parMultiplierAtShot;
        int   scaled     = (rawBase + bankBonus) * multiplier;
        int   damage     = scaled + ps.DamageBonus;

        LastAttack = new AttackDetail
        {
            Base          = rawBase,
            DamageBonus   = ps.DamageBonus,
            Multiplier    = multiplier,
            BankShotBonus = bankBonus,
            Total         = damage
        };

        int expectedTotal = (rawBase + bankBonus) * multiplier + ps.DamageBonus;
        if (expectedTotal != damage)
            Debug.LogError($"[AttackDisplay] Total mismatch! ({rawBase} + {bankBonus}) x {multiplier} + {ps.DamageBonus} = {expectedTotal}, but Total = {damage}");

        if (bankBonus > 0) DebugHUD.Log($"Bank shot! +{bankBonus} bonus damage.");

        if (!CurrentEnemy.IsDead)
        {
            DebugHUD.Log($"Player deals {damage} damage.");
            CurrentEnemy.TakeDamage(damage);
            AttackDisplay.Instance?.Show(LastAttack);
        }

        yield return new WaitForSeconds(betweenActionsDelay);
        if (State == EncounterState.PlayerAttack)
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
        // Check if all enemies are dead
        // Loop through array of enemies and return true if every enemy.IsDead
        if (System.Array.TrueForAll(spawnedEnemies, e => e.IsDead))
        {
            TransitionTo(EncounterState.Reward);
            return;
        }

        // Check if player is dead
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
        StrokesThisHole = 0;
        ClearMana();
        GameManager.Instance.LoadNextLevel();
        RefreshSpawnPoint();
        Par = PendingEncounter.par;
        SpawnMana();
        SpawnBall();
        TransitionTo(EncounterState.PlayerTurn);
    }

    // ── Reward ─────────────────────────────────────────────────────

    private void EnterReward()
    {
        SetShooterEnabled(false);
        RewardManager.Instance?.ShowRewards();
    }

    // ── GameOver ───────────────────────────────────────────────────

    private void EnterGameOver()
    {
        SetShooterEnabled(false);
        GameOverScreen.Instance?.Show();
    }
}