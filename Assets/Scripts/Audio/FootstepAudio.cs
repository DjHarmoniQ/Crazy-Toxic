using UnityEngine;

/// <summary>
/// Plays footstep, dash, jump, and landing audio clips for the Player.
/// Footstep rate scales with the player's current movement speed so faster
/// movement produces faster footfall audio.
///
/// Attach to: The Player GameObject (requires a <see cref="PlayerController"/> on the same object).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class FootstepAudio : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Footstep Clips")]
    [Tooltip("Pool of footstep variants (4–6 recommended).  A random clip is chosen each step.")]
    [SerializeField] private AudioClip[] footstepClips;

    [Tooltip("Clip played when the player dashes.")]
    [SerializeField] private AudioClip dashClip;

    [Tooltip("Clip played when the player jumps.")]
    [SerializeField] private AudioClip jumpClip;

    [Tooltip("Clip played when the player lands after being airborne.")]
    [SerializeField] private AudioClip landClip;

    [Header("Footstep Settings")]
    [Tooltip("Footstep interval (seconds) at base move speed.  Faster movement reduces this proportionally.")]
    [SerializeField] private float baseStepInterval = 0.35f;

    [Tooltip("Reference move speed used to calibrate the step interval.")]
    [SerializeField] private float referenceSpeed = 8f;

    [Tooltip("Minimum allowed step interval regardless of speed.")]
    [SerializeField] private float minStepInterval = 0.15f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private PlayerController _playerController;
    private Rigidbody2D _rigidbody;

    private float _stepTimer;
    private bool _wasGrounded;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _rigidbody        = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        bool isGrounded = IsGrounded();

        // Landing detection
        if (isGrounded && !_wasGrounded)
            PlayClip(landClip);

        _wasGrounded = isGrounded;

        if (!isGrounded) return;

        float speedX = Mathf.Abs(_rigidbody != null ? _rigidbody.linearVelocity.x : 0f);
        if (speedX < 0.1f) return; // not moving horizontally

        // Scale interval by ratio of current speed to reference speed
        float interval = referenceSpeed > 0f
            ? Mathf.Max(minStepInterval, baseStepInterval * (referenceSpeed / speedX))
            : baseStepInterval;

        _stepTimer -= Time.deltaTime;
        if (_stepTimer <= 0f)
        {
            PlayRandomFootstep();
            _stepTimer = interval;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Plays the dash audio clip.  Call this from the dash initiation code in
    /// <see cref="PlayerController"/> or wherever the dash event is raised.
    /// </summary>
    public void OnDash()
    {
        PlayClip(dashClip);
    }

    /// <summary>
    /// Plays the jump audio clip.  Call this from the jump initiation code in
    /// <see cref="PlayerController"/> or wherever the jump event is raised.
    /// </summary>
    public void OnJump()
    {
        PlayClip(jumpClip);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the player is on the ground.
    /// Reads the private <c>_isGrounded</c> field via the public property exposed by
    /// <see cref="PlayerController"/>.  Falls back to a velocity check if unavailable.
    /// </summary>
    private bool IsGrounded()
    {
        // PlayerController exposes _isGrounded as a property via Physics2D.OverlapCircle;
        // we reuse that via reflection-free approach: check vertical velocity is ~0 and
        // rigidbody is resting, or rely on the fact that the controller sets the field.
        // The cleanest approach without modifying PlayerController is to query velocity.
        if (_rigidbody == null) return false;
        return Mathf.Abs(_rigidbody.linearVelocity.y) < 0.05f;
    }

    /// <summary>Picks a random clip from <see cref="footstepClips"/> and plays it via <see cref="SFXManager"/>.</summary>
    private void PlayRandomFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        PlayClip(clip);
    }

    /// <summary>Routes a clip through <see cref="SFXManager"/> if available, otherwise falls back to a local <see cref="AudioSource"/>.</summary>
    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFXWithVariation(clip, 0.95f, 1.05f);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
