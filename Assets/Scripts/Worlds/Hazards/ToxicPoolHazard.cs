using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toxic Pool hazard — World 1: Toxic Swamp (waves 1-19).
/// A trigger collider deals poison damage-over-time (<see cref="_dotDamagePerSecond"/> DPS)
/// to any <see cref="IDamageable"/> that stands inside the pool while it is active.
/// Multiple entities can be in the pool simultaneously; each receives its own DoT coroutine.
/// The pool is visualised as a green semi-transparent area; assign a green sprite or
/// use the placeholder colour applied at runtime.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ToxicPoolHazard : EnvironmentalHazardBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Toxic Pool")]
    [Tooltip("Damage per second dealt to any IDamageable standing in the pool. " +
             "Default: 5 DPS (as per spec).")]
    [SerializeField] private float _dotDamagePerSecond = 5f;

    [Tooltip("Colour applied to the pool's SpriteRenderer when active.")]
    [SerializeField] private Color _activeColor = new Color(0.2f, 0.8f, 0.1f, 0.55f);

    [Tooltip("Colour applied to the pool's SpriteRenderer when inactive (hidden).")]
    [SerializeField] private Color _inactiveColor = new Color(0.2f, 0.8f, 0.1f, 0f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _isActive;
    private Collider2D _collider;
    private SpriteRenderer _renderer;

    /// <summary>Per-target DoT coroutines so each entity in the pool is tracked independently.</summary>
    private readonly Dictionary<IDamageable, Coroutine> _dotCoroutines =
        new Dictionary<IDamageable, Coroutine>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        _renderer = GetComponent<SpriteRenderer>();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllDots();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Hazard Activation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void ActivateHazard()
    {
        _isActive = true;
        _collider.enabled = true;
        if (_renderer != null) _renderer.color = _activeColor;
        Debug.Log("[ToxicPoolHazard] Pool activated.");
    }

    /// <inheritdoc/>
    protected override void DeactivateHazard()
    {
        _isActive = false;
        _collider.enabled = false;
        if (_renderer != null) _renderer.color = _inactiveColor;
        StopAllDots();
        Debug.Log("[ToxicPoolHazard] Pool deactivated.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Trigger Detection
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive) return;
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null && !_dotCoroutines.ContainsKey(target))
            _dotCoroutines[target] = StartCoroutine(PoisonRoutine(target));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
            StopDotForTarget(target);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator PoisonRoutine(IDamageable target)
    {
        while (_isActive)
        {
            target.TakeDamage(Mathf.RoundToInt(_dotDamagePerSecond));
            yield return new WaitForSeconds(1f);
        }
        _dotCoroutines.Remove(target);
    }

    private void StopDotForTarget(IDamageable target)
    {
        if (_dotCoroutines.TryGetValue(target, out Coroutine c))
        {
            if (c != null) StopCoroutine(c);
            _dotCoroutines.Remove(target);
        }
    }

    private void StopAllDots()
    {
        foreach (Coroutine c in _dotCoroutines.Values)
        {
            if (c != null) StopCoroutine(c);
        }
        _dotCoroutines.Clear();
    }
}
