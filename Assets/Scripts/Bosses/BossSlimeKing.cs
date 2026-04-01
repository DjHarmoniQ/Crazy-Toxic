using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 1 — The Slime King (spawns at Wave 10, Toxic Swamp arena).
///
/// Phase 1: Slow ground slam + slime trail
/// Phase 2: Summons 4 slime minions
/// Phase 3: Bouncing slime balls (3-way spread)
/// Phase 4: Rolls across the screen at high speed
/// Phase 5: Explodes into 8 slimes, becomes faster and smaller
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossSlimeKing : BossBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Slime King — Projectiles")]
    [Tooltip("Prefab for the bouncing slime ball projectile.")]
    [SerializeField] private GameObject slimeBallPrefab;

    [Tooltip("Speed of the slime ball projectile.")]
    [SerializeField] private float slimeBallSpeed = 6f;

    [Header("Slime King — Minions")]
    [Tooltip("Prefab for summoned slime minions.")]
    [SerializeField] private EnemyBase slimeMinionPrefab;

    [Tooltip("Radius around the boss at which minions spawn.")]
    [SerializeField] private float minionSpawnRadius = 2f;

    [Header("Slime King — Roll Attack")]
    [Tooltip("Speed multiplier applied during the Phase 4 roll.")]
    [SerializeField] private float rollSpeedMultiplier = 4f;

    [Tooltip("Duration of the Phase 4 roll across the screen.")]
    [SerializeField] private float rollDuration = 1.5f;

    [Header("Slime King — Phase 5")]
    [Tooltip("Number of slimes spawned during the Phase 5 explosion.")]
    [SerializeField] private int phase5SlimeCount = 8;

    [Tooltip("Speed multiplier applied in Phase 5.")]
    [SerializeField] private float phase5SpeedMultiplier = 1.5f;

    [Tooltip("Scale multiplier applied to the boss in Phase 5 (becomes smaller).")]
    [SerializeField] private float phase5ScaleMultiplier = 0.6f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _baseSpeed;
    private bool _phase5Applied;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        base.Start();
        _baseSpeed = moveSpeed;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BossBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Dispatches the correct attack pattern for the current phase.</summary>
    /// <param name="phase">Current boss phase (1–5).</param>
    protected override void ExecutePhaseAttack(int phase)
    {
        switch (phase)
        {
            case 1: StartCoroutine(Phase1GroundSlam()); break;
            case 2: StartCoroutine(Phase2SummonMinions()); break;
            case 3: StartCoroutine(Phase3SlimeBalls()); break;
            case 4: StartCoroutine(Phase4Roll()); break;
            case 5: StartCoroutine(Phase5Explosion()); break;
        }
    }

    /// <inheritdoc/>
    protected override void TransitionToPhase(int phase)
    {
        base.TransitionToPhase(phase);

        if (phase == 5 && !_phase5Applied)
        {
            _phase5Applied = true;
            moveSpeed *= phase5SpeedMultiplier;
            transform.localScale *= phase5ScaleMultiplier;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase Attacks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Phase 1 — Slow ground slam that leaves a slime trail.</summary>
    private IEnumerator Phase1GroundSlam()
    {
        StopMovement();
        Debug.Log("[BossSlimeKing] Phase 1: Ground Slam!");

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 1.5f);

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

        // Slime trail — spawn poison effects in a line
        if (HitEffectManager.Instance != null)
        {
            for (int i = -2; i <= 2; i++)
            {
                HitEffectManager.Instance.PlayPoisonEffect(
                    (Vector2)transform.position + Vector2.right * i);
            }
        }

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Phase 2 — Summons 4 slime minions around the boss.</summary>
    private IEnumerator Phase2SummonMinions()
    {
        Debug.Log("[BossSlimeKing] Phase 2: Summoning minions!");

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("SummonMinions");

        if (slimeMinionPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector2 spawnPos = (Vector2)transform.position +
                                   new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * minionSpawnRadius;
                Instantiate(slimeMinionPrefab, spawnPos, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Phase 3 — Fires a 3-way spread of bouncing slime balls toward the player.</summary>
    private IEnumerator Phase3SlimeBalls()
    {
        Debug.Log("[BossSlimeKing] Phase 3: Slime balls!");

        if (slimeBallPrefab != null && _player != null)
        {
            Vector2 dir = (_player.position - transform.position).normalized;
            float[] angles = { -20f, 0f, 20f };

            foreach (float angle in angles)
            {
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);
                Vector2 spreadDir = rot * dir;
                GameObject ball = Instantiate(slimeBallPrefab, transform.position, Quaternion.identity);
                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = spreadDir * slimeBallSpeed;
            }
        }

        yield return new WaitForSeconds(0.4f);
    }

    /// <summary>Phase 4 — Rolls across the screen at high speed.</summary>
    private IEnumerator Phase4Roll()
    {
        Debug.Log("[BossSlimeKing] Phase 4: Rolling!");

        if (_player == null) yield break;

        Vector2 rollDir = (_player.position - transform.position).normalized;
        float originalSpeed = moveSpeed;
        moveSpeed *= rollSpeedMultiplier;

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            if (_rb != null)
                _rb.linearVelocity = rollDir * moveSpeed;

            elapsed += Time.deltaTime;
            yield return null;
        }

        moveSpeed = originalSpeed;
        StopMovement();
    }

    /// <summary>Phase 5 — Explodes into 8 smaller slimes.</summary>
    private IEnumerator Phase5Explosion()
    {
        Debug.Log("[BossSlimeKing] Phase 5: Explosion!");

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 2f);

        if (slimeMinionPrefab != null)
        {
            for (int i = 0; i < phase5SlimeCount; i++)
            {
                float angle = i * (360f / phase5SlimeCount) * Mathf.Deg2Rad;
                Vector2 spawnPos = (Vector2)transform.position +
                                   new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * minionSpawnRadius;
                Instantiate(slimeMinionPrefab, spawnPos, Quaternion.identity);
            }
        }

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

        yield return new WaitForSeconds(0.5f);
    }
}
