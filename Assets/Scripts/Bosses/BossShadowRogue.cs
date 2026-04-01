using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 4 — The Shadow Rogue (spawns at Wave 55, Shadow Realm arena).
///
/// Phase 1: Stealth approach + burst attack
/// Phase 2: Clone duplicates (2 fake shadows)
/// Phase 3: Shadow daggers (5-way spread)
/// Phase 4: Darkness shroud (reduces player vision radius)
/// Phase 5: True form — all attacks combined, teleports every 2 s
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossShadowRogue : BossBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Shadow Rogue — Daggers")]
    [Tooltip("Prefab for the shadow dagger projectile.")]
    [SerializeField] private GameObject daggerPrefab;

    [Tooltip("Speed of each shadow dagger.")]
    [SerializeField] private float daggerSpeed = 9f;

    [Header("Shadow Rogue — Clones")]
    [Tooltip("Prefab for the decoy clone shadow.")]
    [SerializeField] private GameObject clonePrefab;

    [Tooltip("Radius from the boss at which clones appear.")]
    [SerializeField] private float cloneSpawnRadius = 3f;

    [Header("Shadow Rogue — Stealth")]
    [Tooltip("Duration in seconds of the stealth approach before the burst.")]
    [SerializeField] private float stealthDuration = 1.0f;

    [Tooltip("Burst damage dealt directly if player is in attack range during stealth exit.")]
    [SerializeField] private int stealthBurstDamage = 20;

    [Header("Shadow Rogue — Phase 5")]
    [Tooltip("Interval in seconds between automatic teleports in Phase 5.")]
    [SerializeField] private float teleportInterval = 2f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _phase5TeleportActive;

    // ─────────────────────────────────────────────────────────────────────────
    //  BossBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Dispatches the correct attack pattern for the current phase.</summary>
    /// <param name="phase">Current boss phase (1–5).</param>
    protected override void ExecutePhaseAttack(int phase)
    {
        switch (phase)
        {
            case 1: StartCoroutine(Phase1StealthBurst()); break;
            case 2: StartCoroutine(Phase2Clones()); break;
            case 3: StartCoroutine(Phase3ShadowDaggers()); break;
            case 4: StartCoroutine(Phase4DarknessShroud()); break;
            case 5: StartCoroutine(Phase5TrueForm()); break;
        }
    }

    /// <inheritdoc/>
    protected override void TransitionToPhase(int phase)
    {
        base.TransitionToPhase(phase);

        if (phase == 5 && !_phase5TeleportActive)
        {
            _phase5TeleportActive = true;
            StartCoroutine(Phase5PeriodicTeleport());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase Attacks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Phase 1 — Becomes invisible briefly then unleashes a burst attack.</summary>
    private IEnumerator Phase1StealthBurst()
    {
        Debug.Log("[BossShadowRogue] Phase 1: Stealth Approach!");

        // Fade sprite to simulate stealth
        if (_spriteRenderer != null)
            _spriteRenderer.color = new Color(1f, 1f, 1f, 0.2f);

        StopMovement();
        yield return new WaitForSeconds(stealthDuration);

        // Re-appear
        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.white;

        // Burst — deal damage if player is in range
        if (_player != null)
        {
            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= attackRange * 2f)
            {
                IDamageable playerDamageable = _player.GetComponent<IDamageable>();
                playerDamageable?.TakeDamage(stealthBurstDamage);
            }
        }

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayHitEffect(transform.position, Color.black);

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Phase 2 — Spawns 2 decoy clones to confuse the player.</summary>
    private IEnumerator Phase2Clones()
    {
        Debug.Log("[BossShadowRogue] Phase 2: Clone Duplicates!");

        if (clonePrefab != null)
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = (i * 180f) * Mathf.Deg2Rad;
                Vector2 spawnPos = (Vector2)transform.position +
                                   new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cloneSpawnRadius;
                Instantiate(clonePrefab, spawnPos, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Phase 3 — Fires a 5-way spread of shadow daggers toward the player.</summary>
    private IEnumerator Phase3ShadowDaggers()
    {
        Debug.Log("[BossShadowRogue] Phase 3: Shadow Daggers!");

        if (daggerPrefab != null && _player != null)
        {
            Vector2 dir = (_player.position - transform.position).normalized;
            float[] angles = { -40f, -20f, 0f, 20f, 40f };

            foreach (float angle in angles)
            {
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);
                Vector2 spreadDir = rot * dir;
                GameObject dagger = Instantiate(daggerPrefab, transform.position, Quaternion.identity);
                Rigidbody2D rb = dagger.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = spreadDir * daggerSpeed;
            }
        }

        yield return new WaitForSeconds(0.4f);
    }

    /// <summary>Phase 4 — Activates a darkness shroud, triggering the DarkenRoom arena effect.</summary>
    private IEnumerator Phase4DarknessShroud()
    {
        Debug.Log("[BossShadowRogue] Phase 4: Darkness Shroud!");

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("DarkenRoom");

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayHitEffect(transform.position, Color.black, 3f);

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Phase 5 — True form: combines all attacks in rapid succession.</summary>
    private IEnumerator Phase5TrueForm()
    {
        Debug.Log("[BossShadowRogue] Phase 5: True Form!");

        yield return StartCoroutine(Phase1StealthBurst());
        yield return StartCoroutine(Phase3ShadowDaggers());
        yield return StartCoroutine(Phase2Clones());
    }

    /// <summary>Phase 5 background loop — teleports behind the player every <see cref="teleportInterval"/> seconds.</summary>
    private IEnumerator Phase5PeriodicTeleport()
    {
        while (_phase5TeleportActive && gameObject != null)
        {
            yield return new WaitForSeconds(teleportInterval);

            if (_player == null) continue;

            // Teleport to a random offset around the player
            Vector2 offset = Random.insideUnitCircle.normalized * cloneSpawnRadius;
            transform.position = (Vector2)_player.position + offset;

            if (HitEffectManager.Instance != null)
                HitEffectManager.Instance.PlayHitEffect(transform.position, Color.magenta);
        }
    }
}
