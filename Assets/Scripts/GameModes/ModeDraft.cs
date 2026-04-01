using UnityEngine;

/// <summary>
/// Draft Mode — strategy mode focusing on deck synergies.
///
/// Rules:
/// <list type="bullet">
///   <item>At the start, the player is shown 5 cards and picks a full starting deck of 5.</item>
///   <item>No additional cards mid-run — you must work with your starting hand.</item>
///   <item>Enemies drop gold only (no ability-card drops).</item>
///   <item>Win condition: reach wave 30 with your draft deck.</item>
/// </list>
/// </summary>
public class ModeDraft : GameModeBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Number of cards offered and picked at the start of a Draft run.</summary>
    public const int DraftHandSize = 5;

    /// <summary>Wave the player must reach to win.</summary>
    public const int WinWave = 30;

    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ModeName => "Draft";

    /// <inheritdoc/>
    public override string ModeDescription =>
        "Pick 5 starting cards — no more mid-run. Reach wave 30 to win.";

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary><c>true</c> after the player has confirmed their draft picks.</summary>
    public bool DraftComplete { get; private set; }

    /// <summary>Current highest wave reached.</summary>
    public int HighestWave { get; private set; }

    /// <summary><c>true</c> once the run has ended.</summary>
    private bool _ended;

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the mode is ready for the player to pick their starting deck.
    /// Subscribe from <see cref="DraftPickerUI"/> to open the picker panel.
    /// </summary>
    public event System.Action OnDraftPickerRequested;

    // ─────────────────────────────────────────────────────────────────────────
    //  GameModeBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void OnModeStart()
    {
        DraftComplete = false;
        HighestWave = 1;
        _ended = false;
        Debug.Log("[Draft] Mode started — showing 5-card draft picker.");
        OnDraftPickerRequested?.Invoke();
    }

    /// <inheritdoc/>
    public override void OnModeEnd(bool victory)
    {
        _ended = true;
        string result = victory ? "VICTORY" : "DEFEAT";
        Debug.Log($"[Draft] Run ended ({result}). Highest wave: {HighestWave}");
    }

    /// <inheritdoc/>
    public override void OnWaveComplete(int wave)
    {
        if (wave > HighestWave)
            HighestWave = wave;

        if (wave >= WinWave)
        {
            Debug.Log("[Draft] Wave 30 reached — victory!");
            GameModeManager.Instance?.EndCurrentMode(true);
        }
        else
        {
            Debug.Log($"[Draft] Wave {wave} cleared. {WinWave - wave} waves to go.");
        }
    }

    /// <inheritdoc/>
    public override void OnPlayerDeath()
    {
        if (_ended) return;
        Debug.Log("[Draft] Player died — defeat.");
        GameModeManager.Instance?.EndCurrentMode(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="DraftPickerUI"/> once the player has locked in their 5 cards.
    /// Resumes normal wave spawning.
    /// </summary>
    public void ConfirmDraft()
    {
        DraftComplete = true;
        Debug.Log("[Draft] Draft confirmed — let the run begin!");
    }
}
