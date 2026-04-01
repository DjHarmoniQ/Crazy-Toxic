using System.Collections;
using UnityEngine;

/// <summary>
/// CameraController provides a smooth side-scroller camera that follows the player.
///
/// Setup:
///   1. Attach this script to your Main Camera.
///   2. Assign the Player's Transform to the "Target" field in the Inspector.
///   3. Adjust Smooth Speed, Horizontal Offset, and Vertical Offset to taste.
///
/// The camera always maintains its Z position so it stays fixed on the side-view
/// plane—essential for a side-scroller.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the scene's CameraController.</summary>
    public static CameraController Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Follow Target")]
    [Tooltip("The Transform the camera should follow (drag the Player here).")]
    [SerializeField] private Transform target;

    [Header("Smoothing")]
    [Tooltip("How smoothly the camera follows the target. Lower = smoother, higher = snappier.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float smoothSpeed = 0.12f;

    [Header("Offset")]
    [Tooltip("Horizontal offset from the target's position (positive = look ahead to the right).")]
    [SerializeField] private float horizontalOffset = 2f;

    [Tooltip("Vertical offset from the target's position.")]
    [SerializeField] private float verticalOffset = 1f;

    [Header("Camera Bounds (Optional)")]
    [Tooltip("If true, the camera will be clamped within the min/max bounds.")]
    [SerializeField] private bool useBounds = false;

    [Tooltip("Minimum X position the camera can move to.")]
    [SerializeField] private float minX = -50f;

    [Tooltip("Maximum X position the camera can move to.")]
    [SerializeField] private float maxX = 50f;

    [Tooltip("Minimum Y position the camera can move to.")]
    [SerializeField] private float minY = -10f;

    [Tooltip("Maximum Y position the camera can move to.")]
    [SerializeField] private float maxY = 20f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    // Fixed Z position of the camera so it never moves toward/away from the scene
    private float _fixedZ;

    // Current shake offset applied on top of the follow position each frame
    private Vector3 _shakeOffset;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        // Cache the initial Z depth (typically -10 for 2D/side-scroller cameras)
        _fixedZ = transform.position.z;

        // If no target is assigned, try to find the player automatically
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("[CameraController] Target not assigned – found Player automatically.");
            }
            else
            {
                Debug.LogWarning("[CameraController] No target assigned and no GameObject tagged 'Player' found. " +
                                 "Please assign a Target in the Inspector.");
            }
        }
    }

    /// <summary>
    /// LateUpdate runs after all Update calls, which ensures the camera moves
    /// after the player has already moved this frame—preventing jitter.
    /// </summary>
    private void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Camera Follow Logic
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Smoothly moves the camera toward the target position each frame.
    /// Applies the configured offset and preserves the fixed Z depth.
    /// </summary>
    private void FollowTarget()
    {
        // Desired world position (target + offset)
        Vector3 desiredPosition = new Vector3(
            target.position.x + horizontalOffset,
            target.position.y + verticalOffset,
            _fixedZ
        );

        // Smooth interpolation – SmoothDamp gives a natural ease-in/ease-out feel
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Optional: clamp the camera within level bounds so it doesn't show
        // empty space outside the level geometry
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        // Always lock Z to the fixed depth
        smoothedPosition.z = _fixedZ;

        transform.position = smoothedPosition + _shakeOffset;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shakes the camera for <paramref name="duration"/> seconds with a random
    /// per-frame offset of up to <paramref name="intensity"/> units, then
    /// smoothly returns to the base follow position.
    /// </summary>
    /// <param name="intensity">Maximum shake displacement in world units.</param>
    /// <param name="duration">How long the shake lasts in seconds.</param>
    public void ShakeCamera(float intensity, float duration)
    {
        StartCoroutine(ShakeCameraCoroutine(intensity, duration));
    }

    /// <summary>
    /// Instantly snaps the camera to the target (useful after scene loads or
    /// respawn so the player doesn't see the camera pan in from far away).
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        transform.position = new Vector3(
            target.position.x + horizontalOffset,
            target.position.y + verticalOffset,
            _fixedZ
        );
    }

    /// <summary>
    /// Dynamically changes the follow target at runtime (e.g., when switching
    /// between characters or following a cinematic object).
    /// </summary>
    /// <param name="newTarget">The new Transform to follow.</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Coroutine that applies a random per-frame shake offset for <paramref name="duration"/>
    /// seconds, then smoothly damps the offset back to zero.
    /// </summary>
    private IEnumerator ShakeCameraCoroutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Random 2-D offset scaled by remaining intensity
            float progress = elapsed / duration;
            float currentIntensity = Mathf.Lerp(intensity, 0f, progress);
            Vector2 randomOffset = Random.insideUnitCircle * currentIntensity;
            _shakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Smoothly return to zero offset
        while (_shakeOffset.sqrMagnitude > 0.0001f)
        {
            _shakeOffset = Vector3.Lerp(_shakeOffset, Vector3.zero, Time.deltaTime * 10f);
            yield return null;
        }

        _shakeOffset = Vector3.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Gizmos (Editor Visualization)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Draws a line from the camera to its target in the Scene view.</summary>
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(
            new Vector3(target.position.x + horizontalOffset, target.position.y + verticalOffset, 0f),
            0.3f
        );
    }
}
