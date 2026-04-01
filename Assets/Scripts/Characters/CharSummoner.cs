using UnityEngine;

/// <summary>
/// Summoner — Summoner class.
/// <para><b>Passive — Familiar:</b> A small pet deals 5 DPS to the nearest enemy.</para>
/// <para><b>Ultimate — Summon Army:</b> Spawns 5 golem allies that fight for 15 seconds.</para>
/// </summary>
public class CharSummoner : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Familiar")]
    [Tooltip("Damage per second the Familiar deals to the nearest enemy.")]
    [SerializeField] private float _familiarDPS = 5f;

    [Tooltip("Maximum range within which the Familiar targets an enemy.")]
    [SerializeField] private float _familiarRange = 8f;

    [Header("Ultimate — Summon Army")]
    [Tooltip("Number of golems summoned by Summon Army.")]
    [SerializeField] private int _golemCount = 5;

    [Tooltip("Duration in seconds each golem lasts before despawning.")]
    [SerializeField] private float _golemDuration = 15f;

    [Tooltip("Golem prefab to instantiate (must have EnemyBase or a Golem script).")]
    [SerializeField] private GameObject _golemPrefab;

    [Tooltip("Spawn radius around the player for the golem circle.")]
    [SerializeField] private float _spawnRadius = 3f;

    [Tooltip("Mana required to activate Summon Army.")]
    [SerializeField] private float _ultimateCost = 70f;

    [Tooltip("Cooldown in seconds after using Summon Army.")]
    [SerializeField] private float _ultimateCooldown = 25f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _familiarDamageAccumulator;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Familiar passive: accumulates damage at <see cref="_familiarDPS"/> and
    /// applies it to the nearest enemy each second.
    /// </summary>
    public override void ApplyPassive()
    {
        _familiarDamageAccumulator += _familiarDPS * Time.deltaTime;

        if (_familiarDamageAccumulator >= 1f)
        {
            int damage = Mathf.FloorToInt(_familiarDamageAccumulator);
            _familiarDamageAccumulator -= damage;

            GameObject nearest = FindNearestEnemy();
            if (nearest != null)
            {
                IDamageable target = nearest.GetComponent<IDamageable>();
                target?.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// Summon Army: spawns <see cref="_golemCount"/> golems in a circle around the player.
    /// Each golem is destroyed after <see cref="_golemDuration"/> seconds.
    /// </summary>
    public override void ActivateUltimate()
    {
        if (_golemPrefab == null)
        {
            Debug.LogWarning("[CharSummoner] Golem prefab not assigned — Summon Army skipped.");
            return;
        }

        for (int i = 0; i < _golemCount; i++)
        {
            float angle = i * (360f / _golemCount) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _spawnRadius;
            GameObject golem = Instantiate(_golemPrefab, transform.position + offset, Quaternion.identity);
            Destroy(golem, _golemDuration);
        }

        Debug.Log($"[CharSummoner] Summoned {_golemCount} golems for {_golemDuration}s.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the nearest enemy GameObject within <see cref="_familiarRange"/>, or null.</summary>
    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float nearestDist = _familiarRange;

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
