using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerStats PlayerStats { get; private set; }

    [Header("Scene Names")]
    public string encounterSceneName = "Encounter";
    public string mapSceneName       = "Map";

    // ── Act Progress ───────────────────────────────────────────────

    public ActDefinition CurrentAct       { get; private set; }
    public int           CurrentNodeIndex { get; private set; }
    
    // ── Seeding ────────────────────────────────────────────────────
    
    public int   Seed       { get; private set; }
    public int[] RolledPars { get; private set; }

    // ── Private ────────────────────────────────────────────────────

    private EncounterDefinition currentEncounter;
    private GameObject          currentLevelInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PlayerStats = GetComponent<PlayerStats>();
    }

    // ── Game Flow ──────────────────────────────────────────────────

    public void StartGame(ActDefinition act)
    {
        CurrentAct       = act;
        CurrentNodeIndex = 0;
        CreateSeed();
        RollPars();
        LoadMap();
    }

    public void CreateSeed()
    {
        Seed = System.Environment.TickCount;
        Random.InitState(Seed);
        DebugHUD.Log("Seed: " + Seed);
    }

    private void RollPars()
    {
        RolledPars = new int[CurrentAct.encounters.Length];
        for (int i = 0; i < CurrentAct.encounters.Length; i++)
        {
            var enc = CurrentAct.encounters[i];
            RolledPars[i] = Random.Range(enc.parMin, enc.parMax + 1);
        }
    }

    public void RestartRun()
    {
        PlayerStats.Instance.ResetForNewRun();
        UnloadEncounter();
        CurrentNodeIndex  = 0;
        currentLevelIndex = -1;
        currentEncounter  = null;
        CreateSeed();
        RollPars();
        var mapScene = SceneManager.GetSceneByName(mapSceneName);
        if (mapScene.isLoaded) SceneManager.UnloadSceneAsync(mapSceneName);
        LoadMap();
    }

    public void ReturnToMap()
    {
        UnloadEncounter();
        CurrentNodeIndex++;

        if (CurrentAct == null || CurrentNodeIndex >= CurrentAct.encounters.Length)
        {
            DebugHUD.Log("Act complete!");
            // TODO: act complete screen
            return;
        }

        LoadMap();
    }

    // ── Map Scene ──────────────────────────────────────────────────

    private void LoadMap()
    {
        SceneManager.LoadSceneAsync(mapSceneName, LoadSceneMode.Additive);
    }

    private void UnloadMap()
    {
        SceneManager.UnloadSceneAsync(mapSceneName);
    }

    // ── Encounter Scene ────────────────────────────────────────────

    public void LoadEncounterNode(int index)
    {
        UnloadMap();
        currentEncounter  = CurrentAct.encounters[index];
        currentLevelIndex = -1;
        SpawnLevel(PickNextLevel());
        SceneManager.LoadSceneAsync(encounterSceneName, LoadSceneMode.Additive);
    }

    public void UnloadEncounter()
    {
        if (currentLevelInstance)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
        SceneManager.UnloadSceneAsync(encounterSceneName);
    }

    // ── Level Loading ──────────────────────────────────────────────

    public void LoadNextLevel()
    {
        if (CurrentAct == null || CurrentAct.holePool == null || CurrentAct.holePool.Length == 0)
        {
            Debug.LogWarning("[GameManager] No holes in act hole pool.");
            return;
        }
        SpawnLevel(PickNextLevel());
    }

    private int currentLevelIndex = -1;

    private GameObject PickNextLevel()
    {
        var pool = CurrentAct.holePool;
        if (pool.Length == 1) return pool[0];

        int next;
        do { next = Random.Range(0, pool.Length); }
        while (next == currentLevelIndex);

        currentLevelIndex = next;
        return pool[next];
    }

    private void SpawnLevel(GameObject prefab)
    {
        if (currentLevelInstance)
            Destroy(currentLevelInstance);

        currentLevelInstance               = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        var levelCam = currentLevelInstance.GetComponentInChildren<Camera>();
        if (levelCam)
            levelCam.rect = new Rect(0, 0, 1, 0.5f);
        EncounterManager.PendingEncounter  = currentEncounter;
        EncounterManager.PendingSpawnPoint = FindSpawnPoint(currentLevelInstance);
    }

    private static Transform FindSpawnPoint(GameObject root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
            if (t.CompareTag("BallSpawnPoint")) return t;
        Debug.LogWarning("[GameManager] BallSpawnPoint tag not found in level prefab.");
        return null;
    }
}
