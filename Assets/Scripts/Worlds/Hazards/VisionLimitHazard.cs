using UnityEngine;

/// <summary>
/// Vision Limit hazard — World 4: Shadow Realm (waves 55-79).
/// Reduces the camera's visible area to a small radius around the player by
/// scaling a circular sprite-mask (or a dark overlay with a transparent centre)
/// that is attached to the player.
///
/// Setup option A (recommended — Sprite Mask):
///   1. Create a child GameObject on the Player named "VisionMask".
///   2. Add a <see cref="SpriteMask"/> component; assign a soft-edged radial sprite.
///   3. Set all world sprites' <c>Mask Interaction</c> to <em>Visible Inside Mask</em>
///      in their SpriteRenderer.
///   4. Drag the SpriteMask transform into <see cref="_maskTransform"/>.
///
/// Setup option B (dark-overlay):
///   Assign a full-screen Canvas Image with a radial cutout as <see cref="_overlayObject"/>.
///   The hazard simply enables/disables it.
/// </summary>
public class VisionLimitHazard : EnvironmentalHazardBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Vision Limit")]
    [Tooltip("Transform of the sprite mask (or dark overlay) used to restrict visibility. " +
             "Its local scale is set to (radius × 2) when active.")]
    [SerializeField] private Transform _maskTransform;

    [Tooltip("Radius (world units) of the visible area around the player when active. " +
             "Default: 5 units as per spec.")]
    [SerializeField] private float _visionRadius = 5f;

    [Tooltip("Optional dark-overlay GameObject that is simply enabled when the hazard is active " +
             "and disabled when inactive (use if a sprite-mask approach is not suitable).")]
    [SerializeField] private GameObject _overlayObject;

    [Tooltip("Transform to follow while active (typically the player). " +
             "If null, the mask stays at its original world position.")]
    [SerializeField] private Transform _followTarget;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Keep the mask centred on the player while active
        if (_maskTransform != null && _followTarget != null && _maskTransform.gameObject.activeSelf)
            _maskTransform.position = _followTarget.position;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Hazard Activation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void ActivateHazard()
    {
        if (_maskTransform != null)
        {
            float diameter = _visionRadius * 2f;
            _maskTransform.localScale = new Vector3(diameter, diameter, 1f);
            _maskTransform.gameObject.SetActive(true);
        }

        if (_overlayObject != null)
            _overlayObject.SetActive(true);

        Debug.Log($"[VisionLimitHazard] Vision restricted to radius {_visionRadius} units.");
    }

    /// <inheritdoc/>
    protected override void DeactivateHazard()
    {
        if (_maskTransform != null)
            _maskTransform.gameObject.SetActive(false);

        if (_overlayObject != null)
            _overlayObject.SetActive(false);

        Debug.Log("[VisionLimitHazard] Full vision restored.");
    }
}
