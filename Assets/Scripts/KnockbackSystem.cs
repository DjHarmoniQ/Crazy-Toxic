using System.Collections;
using UnityEngine;

/// <summary>
/// Utility component that applies a timed knockback impulse to any
/// <see cref="Rigidbody2D"/>. Works on both enemies and the player.
///
/// For static utility use call <see cref="ApplyKnockback"/> directly.
/// Attach this component to an enemy or player GameObject if you need
/// per-GameObject knockback immunity (e.g. bosses, shielded enemies).
/// </summary>
public class KnockbackSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Knockback Immunity")]
    [Tooltip(
        "When true this GameObject ignores all knockback calls " +
        "(use for bosses and shielded enemies).")]
    [SerializeField] private bool isKnockbackImmune = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Whether this object is currently immune to knockback.</summary>
    public bool IsKnockbackImmune
    {
        get => isKnockbackImmune;
        set => isKnockbackImmune = value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Static Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies a timed knockback impulse to <paramref name="rb"/>, pushing it
    /// away from <paramref name="sourcePosition"/>.
    ///
    /// If the target has a <see cref="KnockbackSystem"/> component with
    /// <c>isKnockbackImmune = true</c> the call is silently ignored.
    /// </summary>
    /// <param name="rb">The target's Rigidbody2D.</param>
    /// <param name="sourcePosition">World-space origin of the hit (e.g. bullet impact point).</param>
    /// <param name="force">Force magnitude applied to the target.</param>
    /// <param name="duration">Seconds the knockback velocity persists before physics resumes normally.</param>
    public static void ApplyKnockback(Rigidbody2D rb, Vector2 sourcePosition, float force, float duration)
    {
        if (rb == null) return;

        // Check immunity via the component on the same GameObject
        KnockbackSystem ks = rb.GetComponent<KnockbackSystem>();
        if (ks != null && ks.isKnockbackImmune) return;

        // Direction: away from the damage source
        Vector2 direction = ((Vector2)rb.transform.position - sourcePosition).normalized;
        if (direction == Vector2.zero) direction = Vector2.right; // fallback

        // Use the component as coroutine host if available, otherwise create a temporary one
        MonoBehaviour host = ks != null ? (MonoBehaviour)ks : rb.GetComponent<MonoBehaviour>();
        if (host == null) host = rb.gameObject.AddComponent<KnockbackSystem>();

        host.StartCoroutine(KnockbackCoroutine(rb, direction, force, duration));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Coroutine that overrides the body's velocity for <paramref name="duration"/>
    /// seconds to simulate knockback, then restores it to zero.
    /// </summary>
    private static IEnumerator KnockbackCoroutine(Rigidbody2D rb, Vector2 direction, float force, float duration)
    {
        if (rb == null) yield break;

        Vector2 knockbackVelocity = direction * force;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rb == null) yield break;
            rb.linearVelocity = knockbackVelocity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore to resting state — the character's own movement system will
        // take over again on the next frame.
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}
