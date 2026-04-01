using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 2 — The Skeleton Lord (spawns at Wave 20, Bone Crypt arena).
///
/// Phase 1: Bone throw (single projectile)
/// Phase 2: Bone wall (blocks half screen — spawns hazard objects)
/// Phase 3: Summons 3 skeleton archers
/// Phase 4: Teleports + backstab dash
/// Phase 5: Bone tornado (screen-filling rotating projectiles)
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BossSkeletonLord : BossBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Skeleton Lord — Projectiles")]
    [Tooltip("Prefab for the bone throw projectile.")]
    [SerializeField] private GameObject bonePrefab;

    [Tooltip("Speed of the bone throw projectile.")]
    [SerializeField] private float boneThrowSpeed = 8f;

    [Header("Skeleton Lord — Minions")]
    [Tooltip("Prefab for summoned skeleton archer minions.")]
    [SerializeField] private EnemyBase skeletonArcherPrefab;

    [Tooltip("Radius around the boss at which archers spawn.")]
    [SerializeField] private float archerSpawnRadius = 3f;

    [Header("Skeleton Lord — Phase 4 Teleport")]
    [Tooltip("Distance behind the player to teleport to for the backstab dash.")]
    [SerializeField] private float teleportBackstabOffset = 1.5f;

    [Tooltip("Speed of the backstab dash.")]
    [SerializeField] private float backstabDashSpeed = 15f;

    [Tooltip("Duration of the backstab dash.")]
    [SerializeField] private float backstabDashDuration = 0.2f;

    [Header("Skeleton Lord — Phase 5 Tornado")]
    [Tooltip("Number of bones in the Phase 5 tornado volley.")]
    [SerializeField] private int tornadoBoneCount = 12;

    [Tooltip("Speed of tornado bones.")]
    [SerializeField] private float tornadoBoneSpeed = 5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  BossBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Dispatches the correct attack pattern for the current phase.</summary>
    /// <param name="phase">Current boss phase (1–5).</param>
    protected override void ExecutePhaseAttack(int phase)
    {
        switch (phase)
        {
            case 1: StartCoroutine(Phase1BoneThrow()); break;
            case 2: StartCoroutine(Phase2BoneWall()); break;
            case 3: StartCoroutine(Phase3SummonArchers()); break;
            case 4: StartCoroutine(Phase4TeleportBackstab()); break;
            case 5: StartCoroutine(Phase5BoneTornado()); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase Attacks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Phase 1 — Throws a single bone projectile toward the player.</summary>
    private IEnumerator Phase1BoneThrow()
    {
        Debug.Log("[BossSkeletonLord] Phase 1: Bone Throw!");

        if (bonePrefab != null && _player != null)
        {
            Vector2 dir = (_player.position - transform.position).normalized;
            GameObject bone = Instantiate(bonePrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = bone.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = dir * boneThrowSpeed;
        }

        yield return new WaitForSeconds(0.4f);
    }

    /// <summary>Phase 2 — Spawns a bone-wall hazard blocking half the screen.</summary>
    private IEnumerator Phase2BoneWall()
    {
        Debug.Log("[BossSkeletonLord] Phase 2: Bone Wall!");

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("CrackFloor");

        // Spawn bone projectiles in a vertical wall pattern
        if (bonePrefab != null)
        {
            for (int i = -3; i <= 3; i++)
            {
                Vector3 wallPos = transform.position + Vector3.right * 2f + Vector3.up * i;
                Instantiate(bonePrefab, wallPos, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Phase 3 — Summons 3 skeleton archer minions.</summary>
    private IEnumerator Phase3SummonArchers()
    {
        Debug.Log("[BossSkeletonLord] Phase 3: Summoning Archers!");

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("SummonMinions");

        if (skeletonArcherPrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = (i * 120f - 60f) * Mathf.Deg2Rad;
                Vector2 spawnPos = (Vector2)transform.position +
                                   new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * archerSpawnRadius;
                Instantiate(skeletonArcherPrefab, spawnPos, Quaternion.identity);
            }
        }

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Phase 4 — Teleports behind the player and performs a backstab dash.</summary>
    private IEnumerator Phase4TeleportBackstab()
    {
        Debug.Log("[BossSkeletonLord] Phase 4: Teleport Backstab!");

        if (_player == null) yield break;

        // Teleport behind the player
        Vector2 behindPlayer = (Vector2)_player.position -
                                (Vector2)(_player.position - transform.position).normalized * teleportBackstabOffset;
        transform.position = behindPlayer;

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayHitEffect(transform.position, Color.magenta);

        yield return new WaitForSeconds(0.1f);

        // Dash toward the player's position
        Vector2 dashDir = (_player.position - transform.position).normalized;
        float elapsed = 0f;
        while (elapsed < backstabDashDuration)
        {
            if (_rb != null)
                _rb.linearVelocity = dashDir * backstabDashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMovement();
    }

    /// <summary>Phase 5 — Fires a screen-filling rotating bone tornado.</summary>
    private IEnumerator Phase5BoneTornado()
    {
        Debug.Log("[BossSkeletonLord] Phase 5: Bone Tornado!");

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.TriggerArenaMechanic("ScreenShake");

        if (bonePrefab != null)
        {
            for (int i = 0; i < tornadoBoneCount; i++)
            {
                float angle = i * (360f / tornadoBoneCount) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                GameObject bone = Instantiate(bonePrefab, transform.position, Quaternion.identity);
                Rigidbody2D rb = bone.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = dir * tornadoBoneSpeed;
            }
        }

        yield return new WaitForSeconds(0.5f);
    }
}
