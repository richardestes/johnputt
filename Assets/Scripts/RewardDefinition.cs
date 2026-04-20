using UnityEngine;

public enum RewardType
{
    DamageBonus,
    ExtraStrokes,
    BankShotBonus
}

[CreateAssetMenu(fileName = "RewardDefinition", menuName = "Golf Roguelite/Reward Definition")]
public class RewardDefinition : ScriptableObject
{
    public string displayName;
    public string description;
    public RewardType type;
    public int value;
}
