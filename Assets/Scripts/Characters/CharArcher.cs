using UnityEngine;

/// <summary>
/// Archer — Ranger class.
/// <para><b>Passive — Eagle Eye:</b> +15 % critical-hit chance when the target is beyond <see cref="_longRangeThreshold"/> units.</para>
/// <para><b>Ultimate — Arrow Rain:</b> Fires 20 arrows in a spread arc.</para>
/// </summary>
public class CharArcher : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Eagle Eye")]
    [Tooltip("Distance in units beyond which Eagle Eye grants bonus crit chance.")]
    [SerializeField] private float _longRangeThreshold = 10f;

    [Tooltip("Bonus critical-hit chance (0–1) granted at long range.")]
    [SerializeField] private float _critBonus = 0.15f;

    [Header("Ultimate — Arrow Rain")]
    [Tooltip("Number of arrows fired in the ultimate spread.")]
    [SerializeField] private int _arrowCount = 20;

    [Tooltip("Total spread arc in degrees for Arrow Rain.")]
    [SerializeField] private float _spreadAngle = 90f;

    [Tooltip("Prefab for each arrow projectile. Must have BulletProjectile component.")]
    [SerializeField] private GameObject _arrowPrefab;

    [Tooltip("Mana required to activate Arrow Rain.")]
    [SerializeField] private float _ultimateCost = 50f;

    [Tooltip("Cooldown in seconds after using Arrow Rain.")]
    [SerializeField] private float _ultimateCooldown = 18f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties (passive state exposed for GunController/combat)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when the nearest enemy is beyond the long-range threshold,
    /// granting the Eagle Eye crit bonus this frame.
    /// </summary>
    public bool EagleEyeActive { get; private set; }

    /// <summary>
    /// Current crit-chance bonus from Eagle Eye (0 when inactive, <see cref="_critBonus"/> when active).
    /// </summary>
    public float ActiveCritBonus => EagleEyeActive ? _critBonus : 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Eagle Eye passive: checks the distance to the nearest tagged enemy each frame.
    /// Sets <see cref="EagleEyeActive"/> for the combat system to read.
    /// </summary>
    public override void ApplyPassive()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float nearest = float.MaxValue;
        foreach (GameObject e in enemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < nearest) nearest = dist;
        }
        EagleEyeActive = nearest > _longRangeThreshold;
    }

    /// <summary>
    /// Arrow Rain: fires <see cref="_arrowCount"/> arrows spread across a
    /// <see cref="_spreadAngle"/>-degree arc in front of the player.
    /// </summary>
    public override void ActivateUltimate()
    {
        if (_arrowPrefab == null)
        {
            Debug.LogWarning("[CharArcher] Arrow prefab not assigned — Arrow Rain skipped.");
            return;
        }

        float halfSpread = _spreadAngle / 2f;
        float step = _arrowCount > 1 ? _spreadAngle / (_arrowCount - 1) : 0f;

        for (int i = 0; i < _arrowCount; i++)
        {
            float angle = -halfSpread + step * i;
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * transform.right;
            Instantiate(_arrowPrefab, transform.position, Quaternion.LookRotation(Vector3.forward, dir));
        }

        Debug.Log("[CharArcher] Arrow Rain fired.");
    }
}
