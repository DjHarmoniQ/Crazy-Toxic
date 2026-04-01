using UnityEngine;

/// <summary>
/// Elementalist — Mage class.
/// <para><b>Passive — Attunement:</b> Every 3rd hit applies a random elemental status (fire, ice, or lightning).</para>
/// <para><b>Ultimate — Elemental Catastrophe:</b> Triggers a simultaneous fire + ice + lightning combo on all nearby enemies.</para>
/// </summary>
public class CharElementalist : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Attunement")]
    [Tooltip("How many hits are required to trigger the random element application.")]
    [SerializeField] private int _attunementHitInterval = 3;

    [Header("Ultimate — Elemental Catastrophe")]
    [Tooltip("Radius in units of the Elemental Catastrophe combo.")]
    [SerializeField] private float _catastropheRadius = 10f;

    [Tooltip("Fire damage component of Elemental Catastrophe.")]
    [SerializeField] private int _fireDamage = 50;

    [Tooltip("Ice damage component of Elemental Catastrophe.")]
    [SerializeField] private int _iceDamage = 50;

    [Tooltip("Lightning damage component of Elemental Catastrophe.")]
    [SerializeField] private int _lightningDamage = 50;

    [Tooltip("Mana required to activate Elemental Catastrophe.")]
    [SerializeField] private float _ultimateCost = 75f;

    [Tooltip("Cooldown in seconds after using Elemental Catastrophe.")]
    [SerializeField] private float _ultimateCooldown = 28f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Elemental Status Enum
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The three elemental types the Attunement passive can apply.</summary>
    public enum Element { Fire, Ice, Lightning }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private int _hitCounter;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Number of hits since the last Attunement proc.</summary>
    public int HitCounter => _hitCounter;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Attunement passive: no per-frame work needed — hit counting is driven
    /// externally via <see cref="RegisterHit"/>.
    /// </summary>
    public override void ApplyPassive()
    {
        // Hit tracking is event-driven via RegisterHit().
    }

    /// <summary>
    /// Elemental Catastrophe: applies fire + ice + lightning damage to all enemies
    /// within <see cref="_catastropheRadius"/>.
    /// </summary>
    public override void ActivateUltimate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _catastropheRadius);
        int totalDamage = _fireDamage + _iceDamage + _lightningDamage;
        int count = 0;

        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue;

            IDamageable target = col.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(totalDamage);
                count++;
            }
        }
        Debug.Log($"[CharElementalist] Elemental Catastrophe hit {count} enemies for {totalDamage} damage.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the combat system each time the Elementalist lands a hit.
    /// Every <see cref="_attunementHitInterval"/>th call applies a random element to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The enemy that was hit.</param>
    public void RegisterHit(GameObject target)
    {
        _hitCounter++;
        if (_hitCounter >= _attunementHitInterval)
        {
            _hitCounter = 0;
            ApplyRandomElement(target);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Randomly picks one of the three elements and applies an additional damage
    /// tick or status effect to <paramref name="target"/>.
    /// </summary>
    private void ApplyRandomElement(GameObject target)
    {
        Element element = (Element)Random.Range(0, 3);
        IDamageable damageable = target != null ? target.GetComponent<IDamageable>() : null;

        switch (element)
        {
            case Element.Fire:
                damageable?.TakeDamage(_fireDamage / 2);
                Debug.Log($"[CharElementalist] Attunement: Fire on {target?.name}");
                break;
            case Element.Ice:
                EnemyBase enemy = target != null ? target.GetComponent<EnemyBase>() : null;
                enemy?.Stun(1.5f);
                Debug.Log($"[CharElementalist] Attunement: Ice (slow) on {target?.name}");
                break;
            case Element.Lightning:
                damageable?.TakeDamage(_lightningDamage / 2);
                Debug.Log($"[CharElementalist] Attunement: Lightning on {target?.name}");
                break;
        }
    }
}
