using UnityEngine;

[CreateAssetMenu(fileName = "ActDefinition", menuName = "Golf Roguelite/Act Definition")]
public class ActDefinition : ScriptableObject
{
    public string actName;
    public EncounterDefinition[] encounters;
}
