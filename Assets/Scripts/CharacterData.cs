using UnityEngine;

/// <summary>
/// Defines which broad role a character fills, used for display and future
/// class-specific mechanics.
/// </summary>
public enum CharacterClass
{
    /// <summary>Melee front-liner.</summary>
    Warrior,
    /// <summary>Ranged damage dealer.</summary>
    Ranger,
    /// <summary>Spell-caster.</summary>
    Mage,
    /// <summary>Stealth and burst damage.</summary>
    Rogue,
    /// <summary>Healer or buffer.</summary>
    Support,
    /// <summary>High-HP defensive fighter.</summary>
    Tank,
    /// <summary>Summons allies.</summary>
    Summoner
}

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

    [Header("Class")]
    [Tooltip("Broad role this character fills (Warrior, Ranger, Mage, etc.).")]
    public CharacterClass characterClass;

    [Header("Base Stats")]
    [Tooltip("Maximum hit points for this character.")]
    public float maxHealth;

    [Tooltip("Horizontal movement speed (units/second).")]
    public float moveSpeed;

    [Tooltip("Base damage dealt per bullet/hit.")]
    public float damage;

    [Tooltip("Damage reduction value (reduces incoming damage by this amount).")]
    public float armor;

    [Header("Passive Ability")]
    [Tooltip("Short display name for this character's passive ability.")]
    public string passiveName;

    [Tooltip("One-sentence description of what the passive does.")]
    public string passiveDescription;

    [Header("Ultimate Ability")]
    [Tooltip("Short display name for this character's ultimate ability.")]
    public string ultimateName;

    [Tooltip("One-sentence description of what the ultimate does.")]
    public string ultimateDescription;

    [Tooltip("Mana cost to activate the ultimate ability.")]
    public float ultimateCost;

    [Tooltip("Seconds the player must wait after using the ultimate before using it again.")]
    public float ultimateCooldown;

    // Knight-specific buff (+25 % HP, +15 % damage) is applied at runtime by
    // CharacterStatApplier using a  characterName == "Knight"  check.
}
