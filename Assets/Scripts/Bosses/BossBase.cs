using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base class for all boss enemies.
/// Inherits from <see cref="EnemyBase"/> and adds 5-phase attack logic,
/// health-threshold–driven phase transitions, arena event hooks, and a
/// dedicated boss HP-bar event channel.
///
/// Concrete boss classes must implement <see cref="ExecutePhaseAttack"/>.
/// </summary>
public abstract class BossBase : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Boss Phases")]
    [Tooltip("Total number of attack phases for this boss.")]
    [SerializeField] protected int totalPhases = 5;

    [Tooltip("Health percentage thresholds that trigger phase changes (descending order). " +
             "E.g. {0.8, 0.6, 0.4, 0.2} means phases 2-5 start at 80%, 60%, 40%, 20% HP.")]
    [SerializeField] protected float[] _phaseHealthThresholds = { 0.8f, 0.6f, 0.4f, 0.2f };

    [Header("Phase Transition")]
    [Tooltip("Duration in seconds the boss is invincible during a phase transition.")]
    [SerializeField] private float _phaseTransitionInvincibilityDuration = 1f;

    [Tooltip("Color the screen flashes on a phase transition (uses SpriteRenderer tint).")]
    [SerializeField] private Color _phaseTransitionFlashColor = Color.white;

    [Header("Death Cinematic")]
    [Tooltip("Delay in seconds between the boss dying and the next wave starting.")]
    [SerializeField] private float _deathCinematicDelay = 3f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever the boss transitions to a new attack phase.
    /// Parameter: the new phase number (1–5).
    /// Bind <see cref="BossArenaManager"/> and <see cref="BossHPBarUI"/> to this event.
    /// </summary>
    public event System.Action<int> OnPhaseChanged;

    /// <summary>
    /// Fired whenever the boss's health changes.
    /// Parameters: <c>currentHealth</c>, <c>maxHealth</c>.
    /// Bind <see cref="BossHPBarUI"/> to this event for a dedicated boss HP bar.
    /// </summary>
    public event System.Action<float, float> OnBossHealthChanged;

    // ─────────────────────────────────────────────────────────────────────────
    //  Protected State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The phase the boss is currently executing (1-based, max = <see cref="totalPhases"/>).</summary>
    protected int _currentPhase = 1;

    /// <summary>True while a phase-transition sequence is running (brief invincibility window).</summary>
    protected bool _isTransitioningPhase;

    /// <summary>Set to true once the death cinematic sequence has begun.</summary>
    protected bool _isDeathCinematic;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extends base Start: subscribes to the Health component so the boss can
    /// monitor its own HP and trigger phase changes automatically.
    /// </summary>
    protected override void Start()
    {
        base.Start();

        // Forward health-change events to the boss HP-bar channel.
        if (_health != null)
            _health.OnHealthChanged += HandleBossHealthChanged;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnHealthChanged -= HandleBossHealthChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IDamageable Override
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void TakeDamage(int amount)
    {
        if (_isTransitioningPhase) return; // invincible during transition
        base.TakeDamage(amount);
        CheckPhaseTransition();
    }

    /// <inheritdoc/>
    public override void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (_isTransitioningPhase) return;
        base.TakeDamage(amount, sourcePosition);
        CheckPhaseTransition();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Abstract Boss Hook
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called each attack cycle with the current phase number so each boss can
    /// implement its own phase-specific attack patterns.
    /// </summary>
    /// <param name="phase">Current phase (1–5).</param>
    protected abstract void ExecutePhaseAttack(int phase);

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Routes the base class attack call through <see cref="ExecutePhaseAttack"/>.</summary>
    protected override void OnAttack()
    {
        ExecutePhaseAttack(_currentPhase);
    }

    /// <summary>
    /// Overrides base death to start the death cinematic sequence and notify
    /// <see cref="WaveManager"/> after <see cref="_deathCinematicDelay"/> seconds.
    /// Also unlocks the arena.
    /// </summary>
    protected override void OnDeath()
    {
        _isDeathCinematic = true;
        StopMovement();

        if (HitEffectManager.Instance != null)
            HitEffectManager.Instance.PlayExplosionEffect(transform.position, 2f);

        if (BossArenaManager.Instance != null)
            BossArenaManager.Instance.UnlockArena();

        StartCoroutine(DeathCinematicCoroutine());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Phase Transition
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the current HP percentage has crossed the next phase
    /// threshold and, if so, triggers a phase transition.
    /// </summary>
    private void CheckPhaseTransition()
    {
        if (_currentPhase > _phaseHealthThresholds.Length) return;
        if (_health == null || _scaledMaxHealth <= 0f) return;

        float hpPct = _scaledMaxHealth > 0f
            ? _health.CurrentHealth / _scaledMaxHealth
            : 0f;
        int nextPhase = _currentPhase + 1;

        // Threshold index is (nextPhase - 2) because phase 1 starts at full HP.
        int thresholdIndex = nextPhase - 2;
        if (thresholdIndex < 0 || thresholdIndex >= _phaseHealthThresholds.Length) return;

        if (hpPct <= _phaseHealthThresholds[thresholdIndex])
        {
            StartCoroutine(PhaseTransitionCoroutine(nextPhase));
        }
    }

    /// <summary>
    /// Handles the phase transition: brief invincibility, flash, SFX log,
    /// event dispatch, and arena notification.
    /// </summary>
    /// <param name="newPhase">The phase number to transition into.</param>
    protected virtual IEnumerator PhaseTransitionCoroutine(int newPhase)
    {
        if (_isTransitioningPhase) yield break;
        _isTransitioningPhase = true;

        TransitionToPhase(newPhase);

        yield return new WaitForSeconds(_phaseTransitionInvincibilityDuration);

        _currentPhase = newPhase;
        _isTransitioningPhase = false;
    }

    /// <summary>
    /// Fires visual/audio cues for a phase transition: screen flash via sprite tint,
    /// logs the roar, and invokes <see cref="OnPhaseChanged"/>.
    /// Override to add more dramatic effects in specific boss classes.
    /// </summary>
    /// <param name="phase">The phase being entered.</param>
    protected virtual void TransitionToPhase(int phase)
    {
        Debug.Log($"[{gameObject.name}] entering phase {phase}");

        // Flash the sprite to indicate the transition visually.
        if (_spriteRenderer != null)
            StartCoroutine(FlashSpriteCoroutine(_phaseTransitionFlashColor, _phaseTransitionInvincibilityDuration));

        // Notify subscribers (BossArenaManager, BossHPBarUI, etc.)
        OnPhaseChanged?.Invoke(phase);
    }

    /// <summary>Briefly tints the sprite with <paramref name="flashColor"/> then restores it.</summary>
    private IEnumerator FlashSpriteCoroutine(Color flashColor, float duration)
    {
        if (_spriteRenderer == null) yield break;

        Color originalColor = _spriteRenderer.color;
        float elapsed = 0f;

        // Blink several times during the transition window.
        while (elapsed < duration)
        {
            _spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            _spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }

        _spriteRenderer.color = originalColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Forwards Health component events to <see cref="OnBossHealthChanged"/>.</summary>
    private void HandleBossHealthChanged(float current, float max)
    {
        OnBossHealthChanged?.Invoke(current, max);
    }

    /// <summary>Plays the death cinematic then triggers the next wave.</summary>
    private IEnumerator DeathCinematicCoroutine()
    {
        Debug.Log($"[{gameObject.name}] defeated! Starting death cinematic…");

        // Drop loot / VFX are handled by the base class pool; the boss version
        // just waits for the dramatic pause before notifying WaveManager.
        yield return new WaitForSeconds(_deathCinematicDelay);

        if (WaveManager.Instance != null)
            WaveManager.Instance.StartNextWave();

        Destroy(gameObject);
    }
}
