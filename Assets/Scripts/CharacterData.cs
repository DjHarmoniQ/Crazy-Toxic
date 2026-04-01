using UnityEngine;

/// <summary>
/// ScriptableObject that stores all base stats and display info for a playable character.
/// Create a new asset via: Right-click → Create → Crazy-Toxic → CharacterData
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Crazy-Toxic/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown on the character select screen.")]
    public string characterName;

    [Tooltip("Portrait sprite shown on the character select screen.")]
    public Sprite portrait;

    [Header("Base Stats")]
    [Tooltip("Maximum hit points for this character.")]
    public float maxHealth;

    [Tooltip("Horizontal movement speed (units/second).")]
    public float moveSpeed;

    [Tooltip("Base damage dealt per bullet/hit.")]
    public float damage;

    [Tooltip("Damage reduction value (reduces incoming damage by this amount).")]
    public float armor;

    // Knight-specific buff (+25 % HP, +15 % damage) is applied at runtime by
    // CharacterStatApplier using a  characterName == "Knight"  check.
}
