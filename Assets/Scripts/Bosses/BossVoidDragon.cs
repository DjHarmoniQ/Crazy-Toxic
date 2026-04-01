using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 5 — The Void Dragon (spawns at Wave 80, Void Rift arena).
///
/// Phase 1: Fireball volley (3 fireballs in sequence)
/// Phase 2: Wing gust (knockback blast)
/// Phase 3: Laser beam (telegraph, then beam)
/// Phase 4: Dive bomb (targets player's position)
/// Phase 5: Void rift portals (enemies spawn from portals mid-fight)
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossVoidDragon : BossBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Void Dragon — Fireballs")]
    [Tooltip("Prefab for the fireball projectile.")]
    [SerializeField] private GameObject fireballPrefab;

    [Tooltip("Speed of each fireball.")]
    [SerializeField] private float fireballSpeed = 7f;

    [Tooltip("Number of fireballs in the Phase 1 volley.")]
    [SerializeField] private int fireballCount = 3;

    [Tooltip("Delay in seconds between each fireball in the volley.")]
    [SerializeField] private float fireballDelay = 0.3f;

    [Header("Void Dragon — Wing Gust")]
    [Tooltip("Knockback force applied to the player by the wing gust.")]
    [SerializeField] private float gustKnockbackForce = 12f;

    [Tooltip("Knockback duration for the wing gust.")]
    [SerializeField] private float gustKnockbackDuration = 0.5f;

    [Header("Void Dragon — Laser")]
    [Tooltip("Duration of the laser telegraph (wind-up) before the beam fires.")]
    [SerializeField] private float laserTelegraphDuration = 1.2f;

    [Tooltip("Duration of the active laser beam.")]
    [SerializeField] private float laserBeamDuration = 1.5f;

    [Tooltip("Damage per second dealt while the laser hits the player.")]
    [SerializeField] private int laserDPS = 15;

    [Header("Void Dragon — Dive Bomb")]
    [Tooltip("Speed of the dive bomb toward the player.")]
    [SerializeField] private float diveBombSpeed = 20f;

    [Tooltip("Duration of the dive-bomb dash.")]
    [SerializeField] private float diveBombDuration = 0.25f;

    [Header("Void Dragon — Phase 5 Portals")]
    [Tooltip("Prefab for a void-rift portal that spawns enemies.")]
    [SerializeField] private GameObject voidPortalPrefab;

    [Tooltip("Enemy prefab spawned from each void portal.")]
    [SerializeField] private EnemyBase portalEnemyPrefab;

    [Tooltip("Number of portals opened during Phase 5.")]
    [SerializeField] private int portalCount = 3;

    // ─────────────────────────────────────────────────────────────────────────
    //  BossBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Dispatches the correct attack pattern for the current phase.</summary>
    /// <param name="phase">Current boss phase (1–5).</param>
    protected override void ExecutePhaseAttack(int phase)
    {
        switch (phase)
        {
            case 1: StartCoroutine(Phase1FireballVolley()); break;
            case 2: StartCoroutine(Phase2WingGust()); break;
            case 3: StartCoroutine(Phase3LaserBeam()); break;
            case 4: StartCoroutine(Phase4DiveBomb()); break;
            case 5: StartCoroutine(Phase5VoidRifts()); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase Attacks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Phase 1 — Fires a volley of fireballs toward the player.</summary>
    private IEnumerator Phase1FireballVolley()
    {
        Debug.Log("[BossVoidDragon] Phase 1: Fireball Volley!");

        for (int i = 0; i < fireballCount; i++)
        {
            if (fireballPrefab != null && _player != null)
            {
                Vector2 dir = (_player.position - transform.position).normalized;
                // Add slight spread per shot
                dir = Quaternion.Euler(0f, 0f, Random.Range(-10f, 10f)) * dir;
                GameObject fireball = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
                Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = dir * fireballSpeed;

                if (HitEffectManager.Instance != null)
                    HitEffectManager.Instance.PlayExplosionEffect(transform.position, 0.5f);
            }

            yield return new WaitForSeconds(fireballDelay);
        }
    }

    /// <summary>Phase 2 — Creates a powerful wing-gust that knocks back the player.</summary>
    private IEnumerator Phase2WingGust()
    {
        Debug.Log("[BossVoidDragon] Phase 2: Wing Gust!");

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 3f);

        // Knock the player back
        if (_player != null)
        {
            Rigidbody2D playerRb = _player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
                KnockbackSystem.ApplyKnockback(playerRb, transform.position,
                                               gustKnockbackForce, gustKnockbackDuration);
        }

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Phase 3 — Charges a telegraph, then fires a sustained laser beam.</summary>
    private IEnumerator Phase3LaserBeam()
    {
        Debug.Log("[BossVoidDragon] Phase 3: Laser Beam — telegraphing!");

        StopMovement();

        // Telegraph phase — flash warning
        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayHitEffect(transform.position, Color.cyan, 2f);

        yield return new WaitForSeconds(laserTelegraphDuration);

        Debug.Log("[BossVoidDragon] Phase 3: Laser BEAM!");

        // Beam active — deal damage each second while player is in front
        float elapsed = 0f;
        while (elapsed < laserBeamDuration)
        {
            if (_player != null)
            {
                float dist = Vector2.Distance(transform.position, _player.position);
                if (dist <= detectionRange)
                {
                    IDamageable playerDamageable = _player.GetComponent<IDamageable>();
                    playerDamageable?.TakeDamage(laserDPS);
                }
            }

            if (HitEffectManager.Instance != null)
                HitEffectManager.Instance.PlayHitEffect(transform.position, Color.cyan, 1f);

            elapsed += 1f;
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>Phase 4 — Rises up, then dive-bombs toward the player's position.</summary>
    private IEnumerator Phase4DiveBomb()
    {
        Debug.Log("[BossVoidDragon] Phase 4: Dive Bomb!");

        if (_player == null) yield break;

        // Record player position at the moment the dive starts
        Vector2 targetPos = _player.position;
        Vector2 diveBombDir = (targetPos - (Vector2)transform.position).normalized;

        float elapsed = 0f;
        while (elapsed < diveBombDuration)
        {
            if (_rb != null)
                _rb.linearVelocity = diveBombDir * diveBombSpeed;

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMovement();

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 2f);

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Phase 5 — Opens void-rift portals around the arena that continuously spawn enemies.</summary>
    private IEnumerator Phase5VoidRifts()
    {
        Debug.Log("[BossVoidDragon] Phase 5: Void Rift Portals!");

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("SummonMinions");

        for (int i = 0; i < portalCount; i++)
        {
            float angle = i * (360f / portalCount) * Mathf.Deg2Rad;
            Vector2 portalPos = (Vector2)transform.position +
                                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f;

            if (voidPortalPrefab != null)
                Instantiate(voidPortalPrefab, portalPos, Quaternion.identity);

            if (portalEnemyPrefab != null)
                Instantiate(portalEnemyPrefab, portalPos, Quaternion.identity);

            if (HitEffectManager.Instance != null)
                HitEffectManager.Instance.PlayHitEffect(portalPos, Color.magenta, 1.5f);
        }

        yield return new WaitForSeconds(0.5f);
    }
}
