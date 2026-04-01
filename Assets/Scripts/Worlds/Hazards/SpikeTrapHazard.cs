using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spike Trap hazard — World 2: Bone Crypt (waves 20-34).
/// Periodically animates spikes up from the floor.  Any <see cref="IDamageable"/>
/// that touches the spikes' trigger collider while they are extended takes
/// <see cref="EnvironmentalHazardBase._damage"/> instant damage (default 30).
/// Each entity is damaged only once per activation cycle (tracked via a hit-set).
///
/// Assign a <c>Spike</c> child GameObject whose localPosition Y will be lerped
/// between <see cref="_retractedYOffset"/> and <see cref="_extendedYOffset"/>.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpikeTrapHazard : EnvironmentalHazardBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Spike Trap")]
    [Tooltip("Child transform representing the visible spike mesh/sprite.")]
    [SerializeField] private Transform _spikeVisual;

    [Tooltip("Local Y position when spikes are fully retracted (hidden below floor).")]
    [SerializeField] private float _retractedYOffset = -1f;

    [Tooltip("Local Y position when spikes are fully extended (deal damage).")]
    [SerializeField] private float _extendedYOffset = 0f;

    [Tooltip("Speed at which spikes extend or retract (units per second).")]
    [SerializeField] private float _animateSpeed = 5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _isExtended;
    private Collider2D _damageCollider;

    /// <summary>Entities already hit during the current activation cycle — prevents repeat damage.</summary>
    private readonly HashSet<IDamageable> _hitThisCycle = new HashSet<IDamageable>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _damageCollider = GetComponent<Collider2D>();
        _damageCollider.isTrigger = true;
        _damageCollider.enabled = false;

        // Start fully retracted
        if (_spikeVisual != null)
        {
            Vector3 pos = _spikeVisual.localPosition;
            pos.y = _retractedYOffset;
            _spikeVisual.localPosition = pos;
        }
    }

    private void Update()
    {
        if (_spikeVisual == null) return;

        float targetY = _isExtended ? _extendedYOffset : _retractedYOffset;
        Vector3 pos = _spikeVisual.localPosition;
        pos.y = Mathf.MoveTowards(pos.y, targetY, _animateSpeed * Time.deltaTime);
        _spikeVisual.localPosition = pos;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Hazard Activation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void ActivateHazard()
    {
        _isExtended = true;
        _hitThisCycle.Clear();
        _damageCollider.enabled = true;
        Debug.Log("[SpikeTrapHazard] Spikes extended.");
    }

    /// <inheritdoc/>
    protected override void DeactivateHazard()
    {
        _isExtended = false;
        _damageCollider.enabled = false;
        _hitThisCycle.Clear();
        Debug.Log("[SpikeTrapHazard] Spikes retracted.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Trigger Detection
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isExtended) return;
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null && _hitThisCycle.Add(target))
        {
            target.TakeDamage(_damage);
            Debug.Log($"[SpikeTrapHazard] Dealt {_damage} damage to {other.gameObject.name}.");
        }
    }
}
