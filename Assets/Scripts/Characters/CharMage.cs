using UnityEngine;

/// <summary>
/// Mage — Mage class.
/// <para><b>Passive — Arcane Surge:</b> Spells cost 10 % less mana while the passive is active.</para>
/// <para><b>Ultimate — Arcane Nova:</b> 360° energy burst that damages all nearby enemies.</para>
/// </summary>
public class CharMage : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Arcane Surge")]
    [Tooltip("Fraction (0–1) by which all mana costs are reduced while Arcane Surge is active.")]
    [SerializeField] private float _manaDiscount = 0.10f;

    [Header("Ultimate — Arcane Nova")]
    [Tooltip("Radius in units of the 360° Arcane Nova burst.")]
    [SerializeField] private float _novaRadius = 8f;

    [Tooltip("Damage dealt to each enemy caught in the Nova.")]
    [SerializeField] private int _novaDamage = 80;

    [Tooltip("Mana required to activate Arcane Nova.")]
    [SerializeField] private float _ultimateCost = 60f;

    [Tooltip("Cooldown in seconds after using Arcane Nova.")]
    [SerializeField] private float _ultimateCooldown = 20f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Multiplicative mana-cost modifier. Multiply any mana cost by this value
    /// to apply the Arcane Surge discount (e.g. 0.9 = 10 % off).
    /// </summary>
    public float ManaCostMultiplier => 1f - _manaDiscount;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Arcane Surge passive: always active — the mana discount is read via
    /// <see cref="ManaCostMultiplier"/> rather than applied each frame.
    /// No per-frame work is needed here.
    /// </summary>
    public override void ApplyPassive()
    {
        // Discount is applied passively via ManaCostMultiplier property.
    }

    /// <summary>
    /// Arcane Nova: deals <see cref="_novaDamage"/> to every enemy within
    /// <see cref="_novaRadius"/> units in all directions.
    /// </summary>
    public override void ActivateUltimate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _novaRadius);
        int count = 0;
        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue;

            IDamageable target = col.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(_novaDamage);
                count++;
            }
        }
        Debug.Log($"[CharMage] Arcane Nova hit {count} targets.");
    }
}
