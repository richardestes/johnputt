using UnityEngine;

[CreateAssetMenu(fileName = "EncounterDefinition", menuName = "Golf Roguelite/Encounter Definition")]
public class EncounterDefinition : ScriptableObject
{
    [Header("Enemy")]
    public string enemyName = "Enemy";
    public int enemyMaxHealth = 20;
    public int enemyDamage = 5;

    [Header("Holes")]
    public HoleDefinition[] levels;
}
