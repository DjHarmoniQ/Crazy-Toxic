using UnityEngine;

/// <summary>
/// Rushdown Mode — clear 10 waves as fast as possible.
///
/// Rules:
/// <list type="bullet">
///   <item>Clear 10 waves. The run timer is displayed prominently.</item>
///   <item>Gold-per-wave bonus: 500 − (elapsedSeconds × 10), min 0 — rewards speed.</item>
///   <item>Boss spawns on wave 5 and wave 10.</item>
///   <item>No mana regeneration — mana must be collected from orbs dropped by enemies.</item>
/// </list>
/// </summary>
public class ModeRushdown : GameModeBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Total waves required to complete the mode.</summary>
    public const int TotalWaves = 10;

    /// <summary>Base gold granted at the end of each wave before time penalty.</summary>
    public const float BaseWaveGold = 500f;

    /// <summary>Gold deducted per elapsed second at wave completion.</summary>
    public const float GoldTimePenaltyPerSecond = 10f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ModeName => "Rushdown";

    /// <inheritdoc/>
    public override string ModeDescription =>
        "Clear 10 waves as fast as possible. Boss on wave 5 & 10. No mana regen.";

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Total elapsed run time in seconds.</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>Number of waves cleared so far.</summary>
    public int WavesCleared { get; private set; }

    /// <summary>Total gold earned from wave completion bonuses.</summary>
    public int TotalBonusGold { get; private set; }

    /// <summary><c>true</c> once the run has ended.</summary>
    private bool _ended;

    // ─────────────────────────────────────────────────────────────────────────
    //  GameModeBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void OnModeStart()
    {
        ElapsedTime = 0f;
        WavesCleared = 0;
        TotalBonusGold = 0;
        _ended = false;
        Debug.Log("[Rushdown] Mode started — clear 10 waves as fast as you can!");
    }

    /// <inheritdoc/>
    public override void OnModeEnd(bool victory)
    {
        _ended = true;
        string result = victory ? "VICTORY" : "DEFEAT";
        Debug.Log($"[Rushdown] Run ended ({result}). " +
                  $"Time: {ElapsedTime:F1}s | Gold: {TotalBonusGold}");
    }

    /// <inheritdoc/>
    public override void OnWaveComplete(int wave)
    {
        WavesCleared++;

        int bonus = Mathf.Max(0, Mathf.RoundToInt(BaseWaveGold - ElapsedTime * GoldTimePenaltyPerSecond));
        TotalBonusGold += bonus;
        Debug.Log($"[Rushdown] Wave {wave} cleared. " +
                  $"Bonus gold: {bonus} | Total bonus gold: {TotalBonusGold}");

        if (WavesCleared >= TotalWaves)
        {
            Debug.Log("[Rushdown] All 10 waves cleared — victory!");
            GameModeManager.Instance?.EndCurrentMode(true);
        }
    }

    /// <inheritdoc/>
    public override void OnPlayerDeath()
    {
        if (_ended) return;
        Debug.Log("[Rushdown] Player died — defeat.");
        GameModeManager.Instance?.EndCurrentMode(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Update
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_ended)
            ElapsedTime += Time.deltaTime;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if <paramref name="wave"/> is a boss wave (5 or 10).
    /// Used by the spawner to decide whether to spawn a boss instead of normal enemies.
    /// </summary>
    /// <param name="wave">Wave number to check.</param>
    public static bool IsBossWave(int wave) => wave == 5 || wave == 10;
}
