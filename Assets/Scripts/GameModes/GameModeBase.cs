using UnityEngine;

/// <summary>
/// Abstract base class for all game modes.
/// Concrete mode classes override the abstract lifecycle hooks to implement
/// mode-specific rules, scoring, and win/loss conditions.
/// </summary>
public abstract class GameModeBase : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Display name shown in the mode select screen.</summary>
    public abstract string ModeName { get; }

    /// <summary>Short description shown beneath the mode name in the UI.</summary>
    public abstract string ModeDescription { get; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle Hooks — must be implemented by every mode
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called once when this game mode becomes active.
    /// Use this to apply mode-specific modifiers (e.g. enemy stat buffs,
    /// spawn-rate changes, timer initialisation).
    /// </summary>
    public abstract void OnModeStart();

    /// <summary>
    /// Called when the current run ends, whether by victory or defeat.
    /// Use this to record scores, persist stats, and clean up mode state.
    /// </summary>
    /// <param name="victory"><c>true</c> if the player won; <c>false</c> on defeat.</param>
    public abstract void OnModeEnd(bool victory);

    /// <summary>
    /// Called by <see cref="GameModeManager"/> whenever a wave is fully cleared.
    /// </summary>
    /// <param name="wave">The wave number that was just completed.</param>
    public abstract void OnWaveComplete(int wave);

    // ─────────────────────────────────────────────────────────────────────────
    //  Optional Event Hooks — may be overridden by modes that need them
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called each time an enemy is killed. Override to update kill counters,
    /// score, or trigger special mode logic.
    /// </summary>
    /// <param name="enemy">The <see cref="EnemyBase"/> that was killed.</param>
    public virtual void OnEnemyKilled(EnemyBase enemy) { }

    /// <summary>
    /// Called when the player takes damage. Override for modes that react to
    /// damage (e.g. Hardcore mode preventing health regen).
    /// </summary>
    /// <param name="damage">Amount of damage dealt to the player.</param>
    public virtual void OnPlayerDamaged(float damage) { }

    /// <summary>
    /// Called when the player's health reaches zero. Override to implement
    /// mode-specific death handling (e.g. permadeath, co-op downed state).
    /// </summary>
    public virtual void OnPlayerDeath() { }
}
