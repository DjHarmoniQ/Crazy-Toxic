using UnityEngine;

/// <summary>
/// Knight — Warrior class.
/// <para><b>Passive — Block:</b> 20 % chance to negate incoming damage entirely.</para>
/// <para><b>Ultimate — Shield Bash:</b> Stuns all nearby enemies for 2 seconds.</para>
/// </summary>
public class CharKnight : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Block")]
    [Tooltip("Probability (0–1) that any incoming hit is completely negated.")]
    [SerializeField] private float _blockChance = 0.20f;

    [Header("Ultimate — Shield Bash")]
    [Tooltip("Radius around the player in which enemies are stunned.")]
    [SerializeField] private float _bashRadius = 5f;

    [Tooltip("Duration in seconds that Shield Bash stuns enemies.")]
    [SerializeField] private float _stunDuration = 2f;

    [Tooltip("Mana required to activate Shield Bash.")]
    [SerializeField] private float _ultimateCost = 40f;

    [Tooltip("Cooldown in seconds after using Shield Bash.")]
    [SerializeField] private float _ultimateCooldown = 20f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Block State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Set to <c>true</c> for the current frame when a block proc is active.</summary>
    private bool _isBlocking;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Block passive: each frame there is a <see cref="_blockChance"/> chance
    /// the Knight will negate the next hit. The <see cref="Health"/> component
    /// should query <see cref="IsBlockingThisFrame"/> before applying damage.
    /// </summary>
    public override void ApplyPassive()
    {
        // Roll a new block chance each frame; Health checks this flag
        _isBlocking = Random.value < _blockChance;
    }

    /// <summary>
    /// Shield Bash: damages and stuns all enemies within <see cref="_bashRadius"/>.
    /// </summary>
    public override void ActivateUltimate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _bashRadius);
        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue;

            // Attempt to apply stun via a common interface or component
            EnemyBase enemy = col.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.Stun(_stunDuration);
                Debug.Log($"[CharKnight] Shield Bash stunned {col.gameObject.name} for {_stunDuration}s.");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if the Block passive proc'd this frame.
    /// Call from the damage-receive path before subtracting HP.
    /// </summary>
    public bool IsBlockingThisFrame => _isBlocking;
}
