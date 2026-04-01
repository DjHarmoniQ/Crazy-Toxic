using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 3 — The Golem Titan (spawns at Wave 35, Stone Fortress arena).
///
/// Phase 1: Ground pound (shockwave)
/// Phase 2: Rock throw (3 boulders)
/// Phase 3: Stomp + screen shake, spawns crack hazard zones
/// Phase 4: Loses an arm — arm becomes a separate enemy
/// Phase 5: Enrage — 2× speed, continuous ground slams
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossGolemTitan : BossBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Golem Titan — Rocks")]
    [Tooltip("Prefab for the boulder projectile.")]
    [SerializeField] private GameObject boulderPrefab;

    [Tooltip("Speed of each thrown boulder.")]
    [SerializeField] private float boulderSpeed = 5f;

    [Header("Golem Titan — Arm")]
    [Tooltip("Prefab spawned as a separate enemy when the Golem loses its arm in Phase 4.")]
    [SerializeField] private EnemyBase armEnemyPrefab;

    [Header("Golem Titan — Enrage (Phase 5)")]
    [Tooltip("Speed multiplier applied during Phase 5 enrage.")]
    [SerializeField] private float enrageSpeedMultiplier = 2f;

    [Tooltip("Interval in seconds between ground slams during Phase 5.")]
    [SerializeField] private float enrageSlamInterval = 1.2f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _armDetached;
    private bool _enrageActive;

    // ─────────────────────────────────────────────────────────────────────────
    //  BossBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Dispatches the correct attack pattern for the current phase.</summary>
    /// <param name="phase">Current boss phase (1–5).</param>
    protected override void ExecutePhaseAttack(int phase)
    {
        switch (phase)
        {
            case 1: StartCoroutine(Phase1GroundPound()); break;
            case 2: StartCoroutine(Phase2RockThrow()); break;
            case 3: StartCoroutine(Phase3Stomp()); break;
            case 4: StartCoroutine(Phase4LoseArm()); break;
            case 5: StartCoroutine(Phase5Enrage()); break;
        }
    }

    /// <inheritdoc/>
    protected override void TransitionToPhase(int phase)
    {
        base.TransitionToPhase(phase);

        if (phase == 5 && !_enrageActive)
        {
            _enrageActive = true;
            moveSpeed *= enrageSpeedMultiplier;
            StartCoroutine(ContinuousGroundSlams());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase Attacks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Phase 1 — Ground pound that sends a shockwave along the floor.</summary>
    private IEnumerator Phase1GroundPound()
    {
        Debug.Log("[BossGolemTitan] Phase 1: Ground Pound!");

        StopMovement();

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 2f);

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

        yield return new WaitForSeconds(0.6f);
    }

    /// <summary>Phase 2 — Throws 3 boulders in a spread toward the player.</summary>
    private IEnumerator Phase2RockThrow()
    {
        Debug.Log("[BossGolemTitan] Phase 2: Rock Throw!");

        if (boulderPrefab != null && _player != null)
        {
            Vector2 dir = (_player.position - transform.position).normalized;
            float[] angles = { -15f, 0f, 15f };

            foreach (float angle in angles)
            {
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);
                Vector2 spreadDir = rot * dir;
                GameObject boulder = Instantiate(boulderPrefab, transform.position, Quaternion.identity);
                Rigidbody2D rb = boulder.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = spreadDir * boulderSpeed;
            }
        }

        yield return new WaitForSeconds(0.4f);
    }

    /// <summary>Phase 3 — Stomp with screen shake that cracks the floor into hazard zones.</summary>
    private IEnumerator Phase3Stomp()
    {
        Debug.Log("[BossGolemTitan] Phase 3: Stomp!");

        StopMovement();

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 1.5f);

        if (BossArenaManager.Instance != null)
        {
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");
            BossArenaManager.Instance.TriggerArenaMechanic("CrackFloor");
        }

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Phase 4 — The Golem loses its arm, which detaches and becomes an enemy.</summary>
    private IEnumerator Phase4LoseArm()
    {
        Debug.Log("[BossGolemTitan] Phase 4: Losing Arm!");

        if (!_armDetached && armEnemyPrefab != null)
        {
            _armDetached = true;
            Vector2 armSpawnPos = (Vector2)transform.position + Vector2.right * 1.5f;
            Instantiate(armEnemyPrefab, armSpawnPos, Quaternion.identity);

            if (HitEffectManager.Instance != null)
                HitEffectManager.Instance.PlayBloodEffect(armSpawnPos, 20);
        }

        yield return new WaitForSeconds(0.4f);
    }

    /// <summary>Phase 5 — Enrage: performs a single powerful slam (continuous slams handled by coroutine).</summary>
    private IEnumerator Phase5Enrage()
    {
        Debug.Log("[BossGolemTitan] Phase 5: Enrage Slam!");

        StopMovement();

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 3f);

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Runs during Phase 5 enrage, repeating ground slams at <see cref="enrageSlamInterval"/>.</summary>
    private IEnumerator ContinuousGroundSlams()
    {
        while (_enrageActive && gameObject != null)
        {
            if (BossArenaManager.Instance != null)
                BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

            if (HitEffectManager.Instance != null)
                HitEffectManager.Instance.PlayExplosionEffect(transform.position, 1f);

            yield return new WaitForSeconds(enrageSlamInterval);
        }
    }
}
