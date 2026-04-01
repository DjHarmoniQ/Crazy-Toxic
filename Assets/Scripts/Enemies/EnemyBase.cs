using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base class for all enemy types.
/// Handles the core state machine, health/damage integration, WaveManager
/// registration, wave-difficulty scaling, and knockback-on-hit.
///
/// Concrete enemy classes must implement <see cref="OnAttack"/> and may override
/// <see cref="OnDeath"/> and <see cref="UpdateState"/>.
///
/// Attach alongside a <see cref="Health"/> component and a <see cref="Rigidbody2D"/>.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Enemy State Enum
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>All possible states for the enemy AI state machine.</summary>
    public enum EnemyState
    {
        /// <summary>Standing still, no nearby threat detected.</summary>
        Idle,
        /// <summary>Moving along a patrol path.</summary>
        Patrol,
        /// <summary>Moving toward the player.</summary>
        Chase,
        /// <summary>Performing an attack action.</summary>
        Attack,
        /// <summary>Briefly stunned after taking damage.</summary>
        Hurt,
        /// <summary>Enemy is dead (death animation/cleanup in progress).</summary>
        Dead
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Base Stats")]
    [Tooltip("Hit points before wave-difficulty scaling is applied.")]
    [SerializeField] protected float baseHealth = 50f;

    [Tooltip("Damage dealt per attack before wave-difficulty scaling.")]
    [SerializeField] protected float baseDamage = 10f;

    [Tooltip("Movement speed in units per second.")]
    [SerializeField] protected float moveSpeed = 3f;

    [Header("Detection")]
    [Tooltip("Distance at which this enemy notices the player and transitions to Chase.")]
    [SerializeField] protected float detectionRange = 8f;

    [Tooltip("Distance at which this enemy can attack the player.")]
    [SerializeField] protected float attackRange = 1.5f;

    [Header("Attack")]
    [Tooltip("Minimum seconds between consecutive attacks.")]
    [SerializeField] protected float attackCooldown = 1f;

    [Header("Knockback on Hit")]
    [Tooltip("Force magnitude applied to this enemy when it takes damage.")]
    [SerializeField] protected float hitKnockbackForce = 4f;

    [Tooltip("Duration in seconds of the knockback impulse when hit.")]
    [SerializeField] protected float hitKnockbackDuration = 0.15f;

    [Header("Hurt Flash")]
    [Tooltip("Color the sprite flashes when hit.")]
    [SerializeField] protected Color hurtFlashColor = Color.red;

    [Tooltip("Duration in seconds of the hurt-flash and stun.")]
    [SerializeField] protected float hurtDuration = 0.2f;

    [Header("Loot")]
    [Tooltip("Loot table ScriptableObject used to generate drops on death. Optional.")]
    [SerializeField] protected LootTable lootTable;

    [Header("VFX")]
    [Tooltip("Particle effect prefab spawned at the enemy's position on death. Optional.")]
    [SerializeField] protected GameObject deathEffectPrefab;

    // ─────────────────────────────────────────────────────────────────────────
    //  Protected State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The Health component on this GameObject.</summary>
    protected Health _health;

    /// <summary>The Rigidbody2D used for movement and knockback.</summary>
    protected Rigidbody2D _rb;

    /// <summary>The SpriteRenderer used for hurt-flash visual feedback.</summary>
    protected SpriteRenderer _spriteRenderer;

    /// <summary>Cached reference to the player's Transform (found by tag).</summary>
    protected Transform _player;

    /// <summary>Current AI state.</summary>
    protected EnemyState _currentState = EnemyState.Idle;

    /// <summary>Scaled damage value after wave-difficulty multiplier is applied.</summary>
    protected float _scaledDamage;

    /// <summary>Scaled max-health value set during <see cref="ApplyDifficultyScaling"/>.</summary>
    protected float _scaledMaxHealth;

    /// <summary>Timer tracking remaining cooldown before next attack.</summary>
    protected float _attackCooldownTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Color _originalColor;
    private bool _isDead;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Caches components, applies difficulty scaling, registers with WaveManager.</summary>
    protected virtual void Start()
    {
        _health = GetComponent<Health>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColor = _spriteRenderer.color;

        // Enemies dying must NOT trigger GameOver
        _health.TriggerGameOverOnDeath = false;

        // Find the player by tag
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;

        // Apply wave-difficulty multipliers
        ApplyDifficultyScaling();

        // Subscribe to death event
        _health.OnDeath += HandleDeath;

        // Register with WaveManager
        if (WaveManager.Instance != null)
            WaveManager.Instance.RegisterEnemy();
    }

    /// <summary>Ticks the AI state machine every frame.</summary>
    protected virtual void Update()
    {
        if (_isDead) return;

        _attackCooldownTimer -= Time.deltaTime;
        UpdateState();
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IDamageable Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies damage, triggers the Hurt state, flashes the sprite red,
    /// and applies a knockback impulse away from the hit source.
    /// </summary>
    /// <param name="amount">Hit points of damage to deal.</param>
    public virtual void TakeDamage(int amount)
    {
        if (_isDead) return;

        _health.TakeDamage(amount);

        if (!_isDead)
        {
            EnterHurtState(transform.position);
        }
    }

    /// <summary>
    /// Overload that accepts a world-space damage source position so that
    /// knockback is directed away from the correct origin.
    /// </summary>
    /// <param name="amount">Hit points of damage to deal.</param>
    /// <param name="sourcePosition">World-space position of the damage source.</param>
    public virtual void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (_isDead) return;

        _health.TakeDamage(amount);

        if (!_isDead)
        {
            EnterHurtState(sourcePosition);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Abstract / Virtual Behaviour Hooks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when the enemy should execute its attack.
    /// Concrete subclasses must implement this with their attack logic.
    /// </summary>
    protected abstract void OnAttack();

    /// <summary>
    /// Called when the enemy's health reaches zero.
    /// Notifies WaveManager, drops loot, and spawns the death VFX.
    /// Override to add enemy-specific death behaviour (e.g. Slime splitting).
    /// Remember to call <c>base.OnDeath()</c> to ensure registration/loot are handled.
    /// </summary>
    protected virtual void OnDeath()
    {
        // Notify WaveManager
        if (WaveManager.Instance != null)
            WaveManager.Instance.EnemyKilled();

        // Drop loot
        DropLoot();

        // Spawn death VFX
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    /// <summary>
    /// State machine tick — evaluates transitions between Idle, Patrol, Chase, and Attack.
    /// Override in subclasses to add custom state behaviour (e.g. retreat, stealth).
    /// </summary>
    protected virtual void UpdateState()
    {
        if (_currentState == EnemyState.Hurt || _currentState == EnemyState.Dead)
            return;

        float distToPlayer = _player != null
            ? Vector2.Distance(transform.position, _player.position)
            : float.MaxValue;

        switch (_currentState)
        {
            case EnemyState.Idle:
            case EnemyState.Patrol:
                if (distToPlayer <= detectionRange)
                    _currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                if (distToPlayer <= attackRange && _attackCooldownTimer <= 0f)
                {
                    _currentState = EnemyState.Attack;
                    _attackCooldownTimer = attackCooldown;
                    OnAttack();
                    _currentState = EnemyState.Chase;
                }
                else if (distToPlayer > detectionRange)
                {
                    _currentState = EnemyState.Idle;
                }
                else
                {
                    MoveTowardPlayer();
                }
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Protected Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Moves the enemy toward the player at <see cref="moveSpeed"/>.</summary>
    protected virtual void MoveTowardPlayer()
    {
        if (_player == null || _rb == null) return;

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
    }

    /// <summary>Stops the enemy's movement by zeroing its velocity.</summary>
    protected virtual void StopMovement()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads wave-difficulty multipliers from <see cref="WaveDifficultyScaler"/>
    /// and applies them to the <see cref="Health"/> component and <see cref="_scaledDamage"/>.
    /// </summary>
    private void ApplyDifficultyScaling()
    {
        float healthMult = 1f;
        float damageMult = 1f;

        if (WaveDifficultyScaler.Instance != null && WaveManager.Instance != null)
        {
            int wave = WaveManager.Instance.CurrentWave;
            healthMult = WaveDifficultyScaler.Instance.GetEnemyHealthMultiplier(wave);
            damageMult = WaveDifficultyScaler.Instance.GetEnemyDamageMultiplier(wave);
        }

        _health.SetMaxHealth(baseHealth * healthMult);
        _scaledMaxHealth = baseHealth * healthMult;
        _scaledDamage = baseDamage * damageMult;
    }

    /// <summary>Enters the Hurt state, flashes the sprite, and applies knockback.</summary>
    private void EnterHurtState(Vector2 sourcePosition)
    {
        KnockbackSystem.ApplyKnockback(_rb, sourcePosition, hitKnockbackForce, hitKnockbackDuration);
        StartCoroutine(HurtFlashCoroutine());
    }

    /// <summary>Briefly sets the sprite to the hurt color, then restores the original.</summary>
    private IEnumerator HurtFlashCoroutine()
    {
        _currentState = EnemyState.Hurt;
        _spriteRenderer.color = hurtFlashColor;
        yield return new WaitForSeconds(hurtDuration);
        _spriteRenderer.color = _originalColor;

        if (!_isDead)
            _currentState = EnemyState.Chase;
    }

    /// <summary>Handles the OnDeath event from the Health component.</summary>
    private void HandleDeath()
    {
        if (_isDead) return;
        _isDead = true;
        _currentState = EnemyState.Dead;
        StopMovement();
        OnDeath();
    }

    /// <summary>
    /// Rolls the <see cref="lootTable"/> (if assigned) and spawns the resulting pickups.
    /// Loot GameObjects are expected to have a prefab reference in the LootEntry.
    /// </summary>
    private void DropLoot()
    {
        if (lootTable == null) return;

        LootTable.LootEntry[] drops = lootTable.Roll();
        foreach (LootTable.LootEntry entry in drops)
        {
            if (entry.dropPrefab != null)
                Instantiate(entry.dropPrefab, transform.position, Quaternion.identity);
        }
    }
}
