using UnityEngine;

/// <summary>
/// VoidWalker — Rogue class.
/// <para><b>Passive — Phase:</b> 5 % chance to dodge all hits.</para>
/// <para><b>Ultimate — Void Shift:</b> Teleports behind the nearest enemy;
/// instantly kills it if it is below 25 % HP.</para>
/// </summary>
public class CharVoidWalker : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Phase")]
    [Tooltip("Probability (0–1) that any incoming hit is completely dodged.")]
    [SerializeField] private float _dodgeChance = 0.05f;

    [Header("Ultimate — Void Shift")]
    [Tooltip("HP threshold (0–1) below which the teleport results in an instant kill.")]
    [SerializeField] private float _instantKillThreshold = 0.25f;

    [Tooltip("Maximum range in units to search for a target to teleport behind.")]
    [SerializeField] private float _teleportRange = 20f;

    [Tooltip("How far behind the target the VoidWalker teleports (units).")]
    [SerializeField] private float _behindOffset = 1.5f;

    [Tooltip("Mana required to activate Void Shift.")]
    [SerializeField] private float _ultimateCost = 50f;

    [Tooltip("Cooldown in seconds after using Void Shift.")]
    [SerializeField] private float _ultimateCooldown = 16f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _phasedThisFrame;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when the Phase passive proc'd this frame, negating the incoming hit.
    /// Query from the damage-receive path before subtracting HP.
    /// </summary>
    public bool PhasedThisFrame => _phasedThisFrame;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Phase passive: rolls a dodge chance each frame.
    /// </summary>
    public override void ApplyPassive()
    {
        _phasedThisFrame = Random.value < _dodgeChance;
    }

    /// <summary>
    /// Void Shift: teleports behind the nearest enemy within <see cref="_teleportRange"/>.
    /// If the enemy's HP is below <see cref="_instantKillThreshold"/>, it is instantly killed.
    /// </summary>
    public override void ActivateUltimate()
    {
        GameObject nearest = FindNearestEnemy();
        if (nearest == null)
        {
            Debug.Log("[CharVoidWalker] No enemy in range for Void Shift.");
            return;
        }

        // Teleport behind target
        Vector3 behindPos = nearest.transform.position -
                            nearest.transform.right * _behindOffset;
        transform.position = behindPos;
        Debug.Log($"[CharVoidWalker] Void Shift — teleported behind {nearest.name}.");

        // Instant kill check
        Health enemyHealth = nearest.GetComponent<Health>();
        if (enemyHealth != null)
        {
            float maxHp = enemyHealth.MaxHealth > 0f ? enemyHealth.MaxHealth : 1f;
            bool belowThreshold = enemyHealth.CurrentHealth / maxHp < _instantKillThreshold;

            if (belowThreshold)
            {
                // Instant kill: deal massive damage
                IDamageable target = nearest.GetComponent<IDamageable>();
                target?.TakeDamage(99999);
                Debug.Log($"[CharVoidWalker] Void Shift instant kill on {nearest.name}!");
            }
            else
            {
                // Normal backstab hit
                IDamageable target = nearest.GetComponent<IDamageable>();
                target?.TakeDamage(50);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the nearest enemy within <see cref="_teleportRange"/> or null.</summary>
    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float nearestDist = _teleportRange;

        foreach (GameObject e in enemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = e;
            }
        }
        return nearest;
    }
}
