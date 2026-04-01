using UnityEngine;

/// <summary>
/// PlayerController handles all player movement, jumping, and dash mechanics.
/// Attach this script to your Player GameObject along with a Rigidbody2D and Collider2D.
///
/// Controls:
///   A / D or Arrow Keys  → Move left / right
///   Space                → Jump (press again in the air for Double Jump)
///   Left Shift           → Dash in the current movement direction
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Maximum horizontal run speed (units/second).")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("How quickly the player reaches full speed (higher = snappier).")]
    [SerializeField] private float acceleration = 12f;

    [Tooltip("How quickly the player decelerates when no input is pressed.")]
    [SerializeField] private float deceleration = 16f;

    [Header("Jumping")]
    [Tooltip("Initial upward force applied when the player jumps.")]
    [SerializeField] private float jumpForce = 14f;

    [Tooltip("Extra downward gravity multiplier while falling (makes jumps feel snappier).")]
    [SerializeField] private float fallGravityMultiplier = 2.5f;

    [Tooltip("Gravity multiplier when the jump button is released early (short hops).")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Double Jump")]
    [Tooltip("Allow the player to jump a second time while airborne.")]
    [SerializeField] private bool enableDoubleJump = true;

    [Header("Dash")]
    [Tooltip("Enable the dash ability (Left Shift).")]
    [SerializeField] private bool enableDash = true;

    [Tooltip("Speed of the dash burst.")]
    [SerializeField] private float dashSpeed = 20f;

    [Tooltip("Duration of the dash in seconds.")]
    [SerializeField] private float dashDuration = 0.15f;

    [Tooltip("Cooldown before the player can dash again.")]
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Ground Detection")]
    [Tooltip("Transform placed at the player's feet used as the raycast origin.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Radius of the overlap circle used to detect the ground.")]
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Tooltip("Layer(s) that count as ground.")]
    [SerializeField] private LayerMask groundLayer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties (Phase 1 additions)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Child Transform named "GunAttachPoint" found at startup.
    /// <see cref="GunController"/> uses this to position itself relative to the player.
    /// Assign a child GameObject called "GunAttachPoint" in the scene hierarchy.
    /// </summary>
    public Transform GunAttachPoint { get; private set; }

    // Movement
    private float _horizontalInput;
    private float _targetVelocityX;

    // Jumping
    private bool _isGrounded;
    private bool _hasDoubleJump;
    private bool _jumpRequested;

    // Dash
    private bool _isDashing;
    private bool _dashRequested;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDirection;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Prevent the player from rotating due to physics collisions
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Cache the gun attach point child Transform (Phase 1 addition)
        GunAttachPoint = transform.Find("GunAttachPoint");
        if (GunAttachPoint == null)
            Debug.LogWarning("[PlayerController] No child named 'GunAttachPoint' found. " +
                             "Create one in the hierarchy so GunController can position itself correctly.");
    }

    private void Update()
    {
        // Read input every frame (inputs must be polled in Update, not FixedUpdate)
        GatherInput();

        // Update ground-detection state
        CheckGrounded();

        // Flip sprite to face movement direction
        UpdateSpriteFlip();

        // Count down the dash cooldown
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        // All physics modifications happen here
        if (_isDashing)
        {
            ProcessDash();
        }
        else
        {
            ProcessHorizontalMovement();
            ProcessJump();
            ApplyCustomGravity();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Input
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads raw input values and sets request flags.</summary>
    private void GatherInput()
    {
        // Horizontal: returns -1 (left), 0 (none), or 1 (right)
        _horizontalInput = Input.GetAxisRaw("Horizontal");

        // Jump: set a flag so FixedUpdate can consume it
        if (Input.GetButtonDown("Jump"))
            _jumpRequested = true;

        // Dash: Left Shift
        if (enableDash && Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer <= 0f && !_isDashing)
            _dashRequested = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Ground Detection
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Casts a small circle at the player's feet to determine if they are grounded.
    /// Uses the groundCheck child transform as the center point.
    /// </summary>
    private void CheckGrounded()
    {
        bool wasGrounded = _isGrounded;

        // Use an overlap circle for reliable ground detection
        _isGrounded = Physics2D.OverlapCircle(
            groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.5f,
            groundCheckRadius,
            groundLayer
        );

        // Restore the double jump when landing
        if (_isGrounded && !wasGrounded)
            _hasDoubleJump = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Horizontal Movement
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Smoothly accelerates the player towards the desired horizontal speed,
    /// and decelerates when no input is held.
    /// </summary>
    private void ProcessHorizontalMovement()
    {
        _targetVelocityX = _horizontalInput * moveSpeed;

        // Choose acceleration or deceleration based on whether input is pressed
        float smoothing = Mathf.Approximately(_horizontalInput, 0f) ? deceleration : acceleration;

        float newVelocityX = Mathf.MoveTowards(
            _rb.linearVelocity.x,
            _targetVelocityX,
            smoothing * Time.fixedDeltaTime
        );

        _rb.linearVelocity = new Vector2(newVelocityX, _rb.linearVelocity.y);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Jumping
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies jump force on the first jump (ground) and optionally on a second
    /// jump (double jump) while the player is airborne.
    /// </summary>
    private void ProcessJump()
    {
        if (!_jumpRequested) return;
        _jumpRequested = false;

        if (_isGrounded)
        {
            // Normal jump from the ground
            PerformJump();
            _hasDoubleJump = enableDoubleJump; // Grant double jump after leaving ground
        }
        else if (enableDoubleJump && _hasDoubleJump)
        {
            // Double jump – reset vertical velocity first for consistent height
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            PerformJump();
            _hasDoubleJump = false; // Consume the double jump
        }
    }

    /// <summary>Applies the jump force to the Rigidbody.</summary>
    private void PerformJump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Custom Gravity
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies extra downward gravity when the player is falling or has released
    /// the jump button early, resulting in snappier, game-feel-friendly jumps.
    /// </summary>
    private void ApplyCustomGravity()
    {
        if (_rb.linearVelocity.y < 0f)
        {
            // Falling – apply fall gravity multiplier
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (_rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            // Rising but jump button released – apply low-jump multiplier for short hops
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dash
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates or continues the dash motion. While dashing, normal gravity and
    /// jump physics are disabled so the player moves in a straight horizontal line.
    /// </summary>
    private void ProcessDash()
    {
        if (_dashRequested)
        {
            // Start the dash
            _isDashing = true;
            _dashRequested = false;
            _dashTimer = dashDuration;
            _dashCooldownTimer = dashCooldown;

            // Dash in the direction the player is facing; default to right if idle
            _dashDirection = _horizontalInput != 0f ? Mathf.Sign(_horizontalInput) : (transform.localScale.x >= 0f ? 1f : -1f);

            // Disable gravity during dash so the trajectory stays flat
            _rb.gravityScale = 0f;
        }

        // Move horizontally at dash speed while the timer is active
        if (_dashTimer > 0f)
        {
            _rb.linearVelocity = new Vector2(_dashDirection * dashSpeed, 0f);
            _dashTimer -= Time.fixedDeltaTime;
        }
        else
        {
            // Dash finished – restore gravity and clear the flag
            _isDashing = false;
            _rb.gravityScale = 1f;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sprite / Visual Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Flips the SpriteRenderer horizontally so the character always faces
    /// the direction of movement.
    /// </summary>
    private void UpdateSpriteFlip()
    {
        if (_spriteRenderer == null) return;

        if (_horizontalInput > 0f)
            _spriteRenderer.flipX = false; // Facing right
        else if (_horizontalInput < 0f)
            _spriteRenderer.flipX = true;  // Facing left
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Gizmos (Editor Visualization)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws the ground-check radius in the Scene view for easy debugging.
    /// Shown in green when grounded, red when airborne.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase 1 — Stat Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Overrides the movement speed at runtime.
    /// Called by <see cref="CharacterStatApplier"/> after reading <see cref="CharacterData"/>.
    /// </summary>
    /// <param name="speed">New movement speed in units per second.</param>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
}
