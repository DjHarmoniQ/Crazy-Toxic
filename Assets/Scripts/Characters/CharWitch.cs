using UnityEngine;

/// <summary>
/// Witch — Mage class.
/// <para><b>Passive — Hex:</b> Enemies hit by the Witch deal 10 % less damage.</para>
/// <para><b>Ultimate — Curse Storm:</b> Applies a hex to every enemy currently visible on screen.</para>
/// </summary>
public class CharWitch : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Hex")]
    [Tooltip("Damage reduction fraction (0–1) applied to hexed enemies.")]
    [SerializeField] private float _hexDamageReduction = 0.10f;

    [Tooltip("Duration in seconds that a Hex persists on an enemy.")]
    [SerializeField] private float _hexDuration = 5f;

    [Header("Ultimate — Curse Storm")]
    [Tooltip("Range within which Curse Storm hexes enemies (should cover full screen).")]
    [SerializeField] private float _curseStormRadius = 30f;

    [Tooltip("Mana required to activate Curse Storm.")]
    [SerializeField] private float _ultimateCost = 55f;

    [Tooltip("Cooldown in seconds after using Curse Storm.")]
    [SerializeField] private float _ultimateCooldown = 22f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Damage-reduction fraction applied to each hexed enemy.</summary>
    public float HexDamageReduction => _hexDamageReduction;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Hex passive: no per-frame work; the hex is applied via the HexEnemy helper
    /// when a bullet/hit connects.
    /// </summary>
    public override void ApplyPassive()
    {
        // Hex is applied on hit — no per-frame tick needed.
    }

    /// <summary>
    /// Curse Storm ultimate: applies a Hex component to every enemy within
    /// <see cref="_curseStormRadius"/> (intended to cover the full screen).
    /// </summary>
    public override void ActivateUltimate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _curseStormRadius);
        int count = 0;
        foreach (Collider col in hits)
        {
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                HexEnemy(enemy.gameObject);
                count++;
            }
        }
        Debug.Log($"[CharWitch] Curse Storm hexed {count} enemies.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attaches (or refreshes) a <see cref="EnemyHex"/> component on <paramref name="enemyGO"/>
    /// so it deals reduced damage for <see cref="_hexDuration"/> seconds.
    /// </summary>
    /// <param name="enemyGO">The enemy GameObject to hex.</param>
    public void HexEnemy(GameObject enemyGO)
    {
        EnemyHex hex = enemyGO.GetComponent<EnemyHex>();
        if (hex == null)
            hex = enemyGO.AddComponent<EnemyHex>();

        hex.Apply(_hexDamageReduction, _hexDuration);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  EnemyHex — lightweight component attached to hexed enemies at runtime
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Temporary component added to an enemy when hexed by the Witch.
/// Reduces outgoing damage by <see cref="DamageReduction"/> for
/// <see cref="RemainingDuration"/> seconds.
/// </summary>
public class EnemyHex : MonoBehaviour
{
    /// <summary>Fraction by which this enemy's outgoing damage is reduced.</summary>
    public float DamageReduction { get; private set; }

    /// <summary>Remaining time in seconds before the hex expires.</summary>
    public float RemainingDuration { get; private set; }

    /// <summary>Applies (or refreshes) the hex with the given parameters.</summary>
    /// <param name="reduction">Damage reduction fraction (0–1).</param>
    /// <param name="duration">Duration in seconds.</param>
    public void Apply(float reduction, float duration)
    {
        DamageReduction = reduction;
        RemainingDuration = duration;
    }

    private void Update()
    {
        RemainingDuration -= Time.deltaTime;
        if (RemainingDuration <= 0f)
            Destroy(this);
    }
}
