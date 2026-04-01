using UnityEngine;

/// <summary>
/// Hardcore Mode — permadeath with boosted enemy stats and a 3× score multiplier.
///
/// Rules:
/// <list type="bullet">
///   <item>One life — no revive cards.</item>
///   <item>All enemies have +50 % HP and +50 % damage.</item>
///   <item>No health pickups from enemies.</item>
///   <item>Score multiplier: 3×.</item>
///   <item>Completion unlocks the Hardcore badge on the leaderboard.</item>
/// </list>
/// </summary>
public class ModeHardcore : GameModeBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Additive percentage added to enemy HP and damage.</summary>
    public const float EnemyStatBonus = 0.5f; // +50 %

    /// <summary>Multiplier applied to the player's final score.</summary>
    public const float ScoreMultiplier = 3f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ModeName => "Hardcore";

    /// <inheritdoc/>
    public override string ModeDescription =>
        "One life. Enemies +50% HP & damage. No health pickups. 3× score.";

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Total enemies killed during the run.</summary>
    public int TotalKills { get; private set; }

    /// <summary>Highest wave reached.</summary>
    public int HighestWave { get; private set; }

    /// <summary>Raw score before the 3× multiplier is applied.</summary>
    public int RawScore => HighestWave * 100 + TotalKills * 10;

    /// <summary>Final score with the 3× Hardcore multiplier.</summary>
    public int FinalScore => Mathf.RoundToInt(RawScore * ScoreMultiplier);

    /// <summary>
    /// <c>true</c> when the Hardcore badge has been earned (player died or won
    /// and the badge condition was met — currently awarded on any run completion).
    /// </summary>
    public bool HardcoreBadgeEarned { get; private set; }

    /// <summary><c>true</c> once the run has ended.</summary>
    private bool _ended;

    // ─────────────────────────────────────────────────────────────────────────
    //  GameModeBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void OnModeStart()
    {
        TotalKills = 0;
        HighestWave = 1;
        HardcoreBadgeEarned = false;
        _ended = false;
        Debug.Log("[Hardcore] Mode started — one life, no mercy.");
    }

    /// <inheritdoc/>
    public override void OnModeEnd(bool victory)
    {
        _ended = true;
        HardcoreBadgeEarned = true; // Awarded for completing any Hardcore run.
        string result = victory ? "VICTORY" : "DEFEAT";
        Debug.Log($"[Hardcore] Run ended ({result}). " +
                  $"Wave: {HighestWave} | Kills: {TotalKills} | Score: {FinalScore} " +
                  $"| Badge: {HardcoreBadgeEarned}");
    }

    /// <inheritdoc/>
    public override void OnWaveComplete(int wave)
    {
        if (wave > HighestWave)
            HighestWave = wave;

        Debug.Log($"[Hardcore] Wave {wave} cleared. Score so far: {FinalScore}");
    }

    /// <inheritdoc/>
    public override void OnEnemyKilled(EnemyBase enemy)
    {
        TotalKills++;
    }

    /// <inheritdoc/>
    public override void OnPlayerDeath()
    {
        if (_ended) return;
        Debug.Log("[Hardcore] Player died — permanent death, run over.");
        GameModeManager.Instance?.EndCurrentMode(false);
    }
}
