using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerStats PlayerStats { get; private set; }
    public DebugHUD    DebugHUD    { get; private set; }

    [Header("Scene Names")]
    public string encounterSceneName = "Encounter";
    public string mapSceneName       = "Map";

    // ── Act Progress ───────────────────────────────────────────────

    public ActDefinition CurrentAct       { get; private set; }
    public int           CurrentNodeIndex { get; private set; }

    // ── Private ────────────────────────────────────────────────────

    private EncounterDefinition currentEncounter;
    private GameObject          currentLevelInstance;
    private HoleDefinition      currentLevel;

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
        LoadMap();
    }

    public void RestartRun()
    {
        PlayerStats.Instance.ResetForNewRun();
        UnloadEncounter();
        CurrentNodeIndex  = 0;
        currentLevelIndex = -1;
        currentEncounter  = null;

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
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
        SceneManager.UnloadSceneAsync(encounterSceneName);
    }

    // ── Level Loading ──────────────────────────────────────────────

    public void LoadNextLevel()
    {
        if (currentEncounter == null || currentEncounter.levels == null || currentEncounter.levels.Length == 0)
        {
            Debug.LogWarning("[GameManager] No levels in current encounter.");
            return;
        }
        SpawnLevel(PickNextLevel());
    }

    private int currentLevelIndex = -1;

    private HoleDefinition PickNextLevel()
    {
        var levels = currentEncounter.levels;
        if (levels.Length == 1) return levels[0];

        int next;
        do { next = Random.Range(0, levels.Length); }
        while (next == currentLevelIndex);

        currentLevelIndex = next;
        return levels[next];
    }

    private void SpawnLevel(HoleDefinition level)
    {
        if (currentLevelInstance != null)
            Destroy(currentLevelInstance);

        currentLevelInstance               = Instantiate(level.levelPrefab, Vector3.zero, Quaternion.identity);
        currentLevel                       = level;
        var levelCam = currentLevelInstance.GetComponentInChildren<Camera>();
        if (levelCam != null)
            levelCam.rect = new Rect(0, 0, 1, 0.5f);
        EncounterManager.PendingHole       = level;
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
