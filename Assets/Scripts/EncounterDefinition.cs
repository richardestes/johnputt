using UnityEngine;

[System.Serializable]
public class EnemyData
{
    public string name = "Enemy";
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "EncounterDefinition", menuName = "Golf Roguelite/Encounter Definition")]
public class EncounterDefinition : ScriptableObject
{
    [Header("Display")]
    public string displayName = "Encounter";

    [Header("Enemies")]
    [Tooltip("1–3 enemies fought in sequence.")]
    public EnemyData[] enemies = { new EnemyData() };

    [Header("Holes")]
    public HoleDefinition[] levels;
}
