using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  Enums — defined here so all Card scripts share a single source of truth.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Five rarity tiers used for all ability cards.</summary>
public enum CardRarity
{
    /// <summary>50 % drop weight. Grey colour.</summary>
    Common,
    /// <summary>25 % drop weight. Green colour.</summary>
    Uncommon,
    /// <summary>15 % drop weight. Blue colour.</summary>
    Rare,
    /// <summary>8 % drop weight. Purple colour.</summary>
    Epic,
    /// <summary>2 % drop weight. Orange / gold colour.</summary>
    Legendary
}

/// <summary>Gameplay category that groups related card effects.</summary>
public enum CardCategory
{
    /// <summary>Cards that increase offensive output.</summary>
    Offense,
    /// <summary>Cards that improve survivability.</summary>
    Defense,
    /// <summary>Cards that enhance movement and dashes.</summary>
    Mobility,
    /// <summary>Cards that modify mana behaviour.</summary>
    Mana,
    /// <summary>Meta / quality-of-life cards.</summary>
    Utility
}

/// <summary>
/// Every distinct effect an ability card can apply.
/// 50+ values across Offense, Defense, Mobility, Mana and Utility categories.
/// </summary>
public enum CardEffect
{
    // ── Offense (15) ────────────────────────────────────────────────────────
    /// <summary>Flat or % increase to bullet damage.</summary>
    IncreaseDamage,
    /// <summary>Shots fired on a critical hit deal double damage.</summary>
    DoubleShotOnCrit,
    /// <summary>Bullets apply a damage-over-time poison stack.</summary>
    PoisonBullets,
    /// <summary>Bullets detonate on impact, dealing area damage.</summary>
    ExplosiveBullets,
    /// <summary>Portion of bullet damage is returned as HP.</summary>
    LifeSteal,
    /// <summary>Bullets pass through the first N enemies.</summary>
    BulletPierce,
    /// <summary>Hits chain lightning to nearby enemies.</summary>
    ChainLightning,
    /// <summary>Bullets bounce to an additional target on hit.</summary>
    BounceShot,
    /// <summary>Increases critical hit chance.</summary>
    CritChanceUp,
    /// <summary>Increases critical hit damage multiplier.</summary>
    CritDamageUp,
    /// <summary>Fires a short burst instead of a single shot.</summary>
    BurstFire,
    /// <summary>Hits apply a movement-slowing debuff.</summary>
    SlowOnHit,
    /// <summary>Hits apply a burn damage-over-time effect.</summary>
    BurnOnHit,
    /// <summary>Hits briefly freeze the target.</summary>
    FreezeOnHit,
    /// <summary>Hits apply a curse that amplifies all damage taken.</summary>
    CurseOnHit,

    // ── Defense (10) ────────────────────────────────────────────────────────
    /// <summary>Increases maximum HP.</summary>
    MaxHealthUp,
    /// <summary>Passively regenerates HP over time.</summary>
    HealthRegenPassive,
    /// <summary>Reduces all incoming damage by a flat %.</summary>
    DamageReduction,
    /// <summary>Grants a temporary damage-absorbing shield on kill.</summary>
    ShieldOnKill,
    /// <summary>Returns a portion of received damage to the attacker.</summary>
    ThornsReflect,
    /// <summary>Grants brief invincibility when HP drops below 20 %.</summary>
    InvincibilityOnLowHP,
    /// <summary>Increases flat armour / physical damage reduction.</summary>
    ArmorUp,
    /// <summary>Chance to fully block an incoming hit.</summary>
    BlockChance,
    /// <summary>Revives the player once per run at 50 % HP.</summary>
    ReviveOnce,
    /// <summary>Grants immunity to curse-type status effects.</summary>
    CurseImmunity,

    // ── Mobility (8) ────────────────────────────────────────────────────────
    /// <summary>Increases dash travel distance and speed.</summary>
    DashSpeedUp,
    /// <summary>Grants one additional dash charge.</summary>
    ExtraDash,
    /// <summary>Deals damage to enemies passed through during a dash.</summary>
    DashDamage,
    /// <summary>Grants an extra mid-air jump.</summary>
    AirJumpExtra,
    /// <summary>Increases walk / run speed.</summary>
    MovementSpeedUp,
    /// <summary>Teleports the player a short distance when hit.</summary>
    TeleportOnDamage,
    /// <summary>Player is intangible (no collision) during a dash.</summary>
    PhaseOnDash,
    /// <summary>Grants a short speed burst after finishing a dash.</summary>
    SlipstreamAfterDash,

    // ── Mana (9) ────────────────────────────────────────────────────────────
    /// <summary>Increases maximum mana capacity.</summary>
    MaxManaUp,
    /// <summary>Increases passive mana regeneration rate.</summary>
    ManaRegenUp,
    /// <summary>Reduces the mana cost of all abilities.</summary>
    ManaCostDown,
    /// <summary>Restores mana on each kill.</summary>
    ManaOnKill,
    /// <summary>Restores mana on each critical hit.</summary>
    ManaOnCrit,
    /// <summary>Restores mana each time the player dashes.</summary>
    ManaOnDash,
    /// <summary>Absorbs damage with mana before HP is reduced.</summary>
    ManaShield,
    /// <summary>Excess mana above max is stored and converted to damage.</summary>
    ManaOverflow,
    /// <summary>Releases an AoE explosion when mana is fully emptied.</summary>
    ManaExplosion,

    // ── Utility (10) ────────────────────────────────────────────────────────
    /// <summary>Increases the radius in which loot is automatically attracted.</summary>
    LootRadiusUp,
    /// <summary>Multiplies all gold earned.</summary>
    GoldMultiplier,
    /// <summary>Multiplies all XP earned.</summary>
    ExpMultiplier,
    /// <summary>Reduces all ability and dash cooldowns.</summary>
    CooldownReduction,
    /// <summary>Draws an extra card after each kill (once per wave).</summary>
    CardDrawOnKill,
    /// <summary>Provides a free reroll at the start of each wave card offer.</summary>
    RerollOnWaveStart,
    /// <summary>Copies a random card already in the deck.</summary>
    DuplicateCard,
    /// <summary>Extends the combo validity window.</summary>
    ComboWindowUp,
    /// <summary>Allows the player to skip a wave without losing rewards.</summary>
    WaveSkip,
    /// <summary>Reduces boss HP by a flat % at the start of boss encounters.</summary>
    BossWeakener
}

// ─────────────────────────────────────────────────────────────────────────────
//  AbilityCard ScriptableObject
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Data container for a single ability card.
/// Create instances via <c>Assets ▶ Create ▶ Crazy-Toxic ▶ AbilityCard</c>.
/// </summary>
[CreateAssetMenu(menuName = "Crazy-Toxic/AbilityCard", fileName = "NewAbilityCard")]
public class AbilityCard : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Identity")]
    [Tooltip("Display name shown in the card picker UI.")]
    public string cardName = "Unnamed Card";

    [Tooltip("Short flavour/description text shown below the card name.")]
    [TextArea(2, 4)]
    public string description = "";

    [Tooltip("Card illustration sprite shown in the card slot.")]
    public Sprite artwork;

    // ─────────────────────────────────────────────────────────────────────────
    //  Classification
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Classification")]
    [Tooltip("Rarity tier that governs drop weight and border colour.")]
    public CardRarity rarity = CardRarity.Common;

    [Tooltip("Gameplay category this card belongs to.")]
    public CardCategory category = CardCategory.Utility;

    [Tooltip("The mechanical effect this card applies.")]
    public CardEffect effect = CardEffect.IncreaseDamage;

    // ─────────────────────────────────────────────────────────────────────────
    //  Values
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Values")]
    [Tooltip("Primary modifier value (e.g. +20 damage, +0.15 crit chance).")]
    public float effectValue = 0f;

    [Tooltip("Secondary modifier value for cards that adjust two stats simultaneously.")]
    public float effectValue2 = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Behaviour
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Behaviour")]
    [Tooltip("True = the effect is always-on (passive). " +
             "False = the card augments or replaces an active ability.")]
    public bool isPassive = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  Visuals
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Visuals")]
    [Tooltip("Border and accent colour driven by rarity. " +
             "Leave at default to use the automatic rarity colour.")]
    public Color rarityColor = Color.grey;

    // ─────────────────────────────────────────────────────────────────────────
    //  Rarity Weights
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the normalised drop-weight for the given <paramref name="rarity"/>.
    /// Weights: Common 50 %, Uncommon 25 %, Rare 15 %, Epic 8 %, Legendary 2 %.
    /// </summary>
    /// <param name="rarity">The rarity tier to query.</param>
    /// <returns>A value between 0 and 1 representing relative drop probability.</returns>
    public static float GetRarityWeight(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common    => 0.50f,
            CardRarity.Uncommon  => 0.25f,
            CardRarity.Rare      => 0.15f,
            CardRarity.Epic      => 0.08f,
            CardRarity.Legendary => 0.02f,
            _                    => 0f
        };
    }

    /// <summary>
    /// Returns the canonical rarity colour for the given <paramref name="rarity"/>.
    /// Common=grey, Uncommon=green, Rare=blue, Epic=purple, Legendary=orange.
    /// </summary>
    /// <param name="rarity">The rarity tier to query.</param>
    /// <returns>The associated <see cref="Color"/>.</returns>
    public static Color GetRarityColor(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common    => new Color(0.65f, 0.65f, 0.65f),
            CardRarity.Uncommon  => new Color(0.10f, 0.75f, 0.20f),
            CardRarity.Rare      => new Color(0.10f, 0.45f, 0.95f),
            CardRarity.Epic      => new Color(0.65f, 0.10f, 0.90f),
            CardRarity.Legendary => new Color(1.00f, 0.60f, 0.00f),
            _                    => Color.white
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ScriptableObject Reset (editor helper)
    // ─────────────────────────────────────────────────────────────────────────

    private void Reset()
    {
        rarityColor = GetRarityColor(rarity);
    }

    private void OnValidate()
    {
        // Keep the serialised colour in sync with the rarity when edited in the Inspector.
        rarityColor = GetRarityColor(rarity);
    }
}
