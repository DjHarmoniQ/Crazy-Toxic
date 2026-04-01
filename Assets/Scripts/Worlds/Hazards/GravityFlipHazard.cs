using System.Collections;
using UnityEngine;

/// <summary>
/// Gravity Flip hazard — World 5: Void Rift (waves 80+).
/// Randomly reverses <see cref="Physics2D.gravity"/> for <see cref="_flipDuration"/> seconds,
/// then restores it.  Plays an optional warning sound 1 second before each flip.
/// </summary>
public class GravityFlipHazard : EnvironmentalHazardBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Gravity Flip")]
    [Tooltip("How long (seconds) gravity stays flipped. Default: 3 s as per spec.")]
    [SerializeField] private float _flipDuration = 3f;

    [Tooltip("AudioSource used to play the warning and flip sounds.")]
    [SerializeField] private AudioSource _audioSource;

    [Tooltip("Warning sound played 1 second before gravity flips.")]
    [SerializeField] private AudioClip _warningSound;

    [Tooltip("Sound played the instant gravity flips.")]
    [SerializeField] private AudioClip _flipSound;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly Vector2 NormalGravity  = new Vector2(0f, -9.81f);
    private static readonly Vector2 FlippedGravity = new Vector2(0f,  9.81f);
    private bool _isFlipped;
    private Coroutine _flipCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    protected override void OnDisable()
    {
        base.OnDisable();
        RestoreGravity();
    }

    private void OnApplicationQuit()
    {
        RestoreGravity();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Hazard Activation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void ActivateHazard()
    {
        if (_flipCoroutine != null)
            StopCoroutine(_flipCoroutine);
        _flipCoroutine = StartCoroutine(FlipSequence());
    }

    /// <inheritdoc/>
    protected override void DeactivateHazard()
    {
        // Gravity is restored at the end of FlipSequence; also restore here as a safety net
        RestoreGravity();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private – Flip Sequence
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FlipSequence()
    {
        // 1. Play warning 1 second before the flip
        PlaySound(_warningSound);
        Debug.Log("[GravityFlipHazard] ⚠ Gravity flip incoming in 1 s!");
        yield return new WaitForSeconds(1f);

        // 2. Flip gravity
        Physics2D.gravity = FlippedGravity;
        _isFlipped = true;
        PlaySound(_flipSound);
        Debug.Log("[GravityFlipHazard] 🔄 Gravity flipped!");

        // 3. Maintain for flipDuration
        yield return new WaitForSeconds(_flipDuration);

        // 4. Restore
        RestoreGravity();
        Debug.Log("[GravityFlipHazard] ✅ Gravity restored.");
    }

    private void RestoreGravity()
    {
        if (_isFlipped)
        {
            Physics2D.gravity = NormalGravity;
            _isFlipped = false;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
            _audioSource.PlayOneShot(clip);
    }
}
