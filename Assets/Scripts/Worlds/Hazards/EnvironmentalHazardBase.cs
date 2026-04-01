using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base class for all environmental hazards.
/// Subclasses implement <see cref="ActivateHazard"/> and <see cref="DeactivateHazard"/>;
/// this base class drives the repeating activation cycle automatically.
///
/// The cycle runs as long as the GameObject is active:
///   wait <see cref="_activationInterval"/> seconds →
///   call <see cref="ActivateHazard"/> →
///   wait <see cref="_hazardDuration"/> seconds →
///   call <see cref="DeactivateHazard"/> →
///   repeat.
/// </summary>
public abstract class EnvironmentalHazardBase : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Hazard Timing")]
    [Tooltip("Seconds between the end of one hazard event and the start of the next.")]
    [SerializeField] protected float _activationInterval = 5f;

    [Tooltip("How long the hazard stays active each cycle (seconds).")]
    [SerializeField] protected float _hazardDuration = 2f;

    [Header("Damage")]
    [Tooltip("Damage dealt to the player by this hazard each activation.")]
    [SerializeField] protected int _damage = 15;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private Coroutine _cycleCoroutine;

    /// <summary>Starts the hazard cycle when the GameObject becomes active.</summary>
    protected virtual void OnEnable()
    {
        _cycleCoroutine = StartCoroutine(HazardCycleRoutine());
    }

    /// <summary>Stops the hazard cycle when the GameObject is disabled.</summary>
    protected virtual void OnDisable()
    {
        if (_cycleCoroutine != null)
        {
            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }
        DeactivateHazard();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Abstract Methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called when the hazard becomes active. Override to implement the effect.</summary>
    protected abstract void ActivateHazard();

    /// <summary>Called when the hazard deactivates. Override to clean up the effect.</summary>
    protected abstract void DeactivateHazard();

    // ─────────────────────────────────────────────────────────────────────────
    //  Private – Cycle Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator HazardCycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_activationInterval);
            ActivateHazard();
            yield return new WaitForSeconds(_hazardDuration);
            DeactivateHazard();
        }
    }
}
