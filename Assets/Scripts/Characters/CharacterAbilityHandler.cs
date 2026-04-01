using UnityEngine;

/// <summary>
/// Abstract base component for all character-specific ability handlers.
/// Attach alongside <see cref="PlayerController"/> on the Player GameObject.
///
/// Each concrete subclass implements:
/// <list type="bullet">
///   <item><see cref="UltimateCost"/> — mana required to fire the ultimate.</item>
///   <item><see cref="ActivateUltimate"/> — ultimate ability logic.</item>
///   <item><see cref="ApplyPassive"/> — passive ability logic (ticked every frame).</item>
/// </list>
/// </summary>
public abstract class CharacterAbilityHandler : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Cached Component References
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reference to the player's mana pool.</summary>
    protected ManaSystem _mana;

    /// <summary>Reference to the player's active card collection.</summary>
    protected PlayerCardCollection _cards;

    // ─────────────────────────────────────────────────────────────────────────
    //  Cooldown State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Counts down from <see cref="UltimateCooldown"/>; ultimate fires when this reaches 0.</summary>
    protected float _ultimateCooldownTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Abstract API — implemented by each character
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Mana cost required to activate this character's ultimate ability.</summary>
    public abstract float UltimateCost { get; }

    /// <summary>Cooldown duration in seconds after the ultimate is used.</summary>
    public abstract float UltimateCooldown { get; }

    /// <summary>Executes the character's unique ultimate ability.</summary>
    public abstract void ActivateUltimate();

    /// <summary>
    /// Called every frame to apply/tick passive effects.
    /// Override this for persistent or frame-driven passives.
    /// </summary>
    public abstract void ApplyPassive();

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when the cooldown has expired <em>and</em> the player has
    /// enough mana to activate the ultimate.
    /// </summary>
    public bool CanUseUltimate =>
        _ultimateCooldownTimer <= 0f && _mana != null && _mana.CurrentMana >= UltimateCost;

    /// <summary>
    /// Remaining cooldown time in seconds.
    /// Useful for driving cooldown-bar UI.
    /// </summary>
    public float RemainingCooldown => _ultimateCooldownTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Caches required sibling components.
    /// Subclasses that override <c>Awake</c> must call <c>base.Awake()</c>.
    /// </summary>
    protected virtual void Awake()
    {
        _mana = GetComponent<ManaSystem>();
        _cards = GetComponent<PlayerCardCollection>();

        if (_mana == null)
            Debug.LogWarning($"[{GetType().Name}] ManaSystem component not found on {gameObject.name}.");

        if (_cards == null)
            Debug.LogWarning($"[{GetType().Name}] PlayerCardCollection component not found on {gameObject.name}.");
    }

    /// <summary>
    /// Ticks the cooldown timer, calls <see cref="ApplyPassive"/> each frame,
    /// and listens for the Q key to fire <see cref="ActivateUltimate"/>.
    /// Subclasses that override <c>Update</c> must call <c>base.Update()</c>.
    /// </summary>
    protected virtual void Update()
    {
        // Tick cooldown
        if (_ultimateCooldownTimer > 0f)
            _ultimateCooldownTimer -= Time.deltaTime;

        // Passive tick
        ApplyPassive();

        // Ultimate activation on Q key
        if (Input.GetKeyDown(KeyCode.Q) && CanUseUltimate)
        {
            if (_mana.TrySpendMana(UltimateCost))
            {
                _ultimateCooldownTimer = UltimateCooldown;
                ActivateUltimate();
                Debug.Log($"[{GetType().Name}] Ultimate activated.");
            }
        }
    }
}
