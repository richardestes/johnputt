using UnityEngine;

[CreateAssetMenu(fileName = "HoleDefinition", menuName = "Golf Roguelite/Hole Definition")]
public class HoleDefinition : ScriptableObject
{
    public string holeName;
    public GameObject levelPrefab;
    public int baseMaxStrokes = 5;
}
