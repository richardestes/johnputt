using UnityEngine;

public class CombatViewController : MonoBehaviour
{
    public static CombatViewController Instance { get; private set; }

    [SerializeField] Camera            combatCamera;
    [SerializeField] PlayerCombatSlot  playerSlotPrefab;
    [SerializeField] EnemySlotUI       enemySlotPrefab;

    [Header("Viewport Positions (0–1 within combat camera view)")]
    [SerializeField] Vector2 playerViewport = new Vector2(0.2f, 0.5f);
    [SerializeField] Vector2 enemyViewport  = new Vector2(0.7f, 0.5f);
    [SerializeField] float   enemySpacingViewport = 0.15f;

    private PlayerCombatSlot  playerSlot;
    private EnemySlotUI[]     enemySlots;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(Enemy[] enemies)
    {
        if (combatCamera == null)
        {
            Debug.LogError("[CombatViewController] combatCamera not assigned.", this);
            return;
        }

        if (playerSlot != null) Destroy(playerSlot.gameObject);
        if (enemySlots != null)
            foreach (var s in enemySlots) if (s != null) Destroy(s.gameObject);

        playerSlot = Instantiate(playerSlotPrefab, ViewportToWorld(playerViewport), Quaternion.identity);
        SetLayerRecursive(playerSlot.gameObject, LayerMask.NameToLayer("Combat"));

        int   count      = enemies.Length;
        float totalWidth = (count - 1) * enemySpacingViewport;
        enemySlots       = new EnemySlotUI[count];

        for (int i = 0; i < count; i++)
        {
            float x    = enemyViewport.x + i * enemySpacingViewport - totalWidth * 0.5f;
            var   slot = Instantiate(enemySlotPrefab, ViewportToWorld(new Vector2(x, enemyViewport.y)), Quaternion.identity);
            slot.Bind(enemies[i]);
            SetLayerRecursive(slot.gameObject, LayerMask.NameToLayer("Combat"));
            enemySlots[i] = slot;
        }
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    Vector3 ViewportToWorld(Vector2 viewport)
    {
        float depth = Mathf.Abs(combatCamera.transform.position.z);
        var   pos   = combatCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
        pos.z = 0f;
        return pos;
    }
}
