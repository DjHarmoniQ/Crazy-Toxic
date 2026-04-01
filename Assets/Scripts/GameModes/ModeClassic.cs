using UnityEngine;

/// <summary>
/// Classic Mode — standard endless run.
///
/// Rules:
/// <list type="bullet">
///   <item>Survive as many waves as possible.</item>
///   <item>Score = wave × 100 + kills × 10.</item>
///   <item>Run ends on player death.</item>
/// </list>
/// </summary>
public class ModeClassic : GameModeBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ModeName => "Classic";

    /// <inheritdoc/>
    public override string ModeDescription =>
        "Survive as many waves as possible. Score: Wave×100 + Kills×10.";

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Total enemies killed in the current run.</summary>
    public int TotalKills { get; private set; }

    /// <summary>Highest wave reached in the current run.</summary>
    public int HighestWave { get; private set; }

    /// <summary>Calculated score: wave × 100 + kills × 10.</summary>
    public int Score => HighestWave * 100 + TotalKills * 10;

    // ─────────────────────────────────────────────────────────────────────────
    //  GameModeBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void OnModeStart()
    {
        TotalKills = 0;
        HighestWave = 1;
        Debug.Log("[Classic] Mode started — endless run.");
    }

    /// <inheritdoc/>
    public override void OnModeEnd(bool victory)
    {
        Debug.Log($"[Classic] Run ended. Wave: {HighestWave} | Kills: {TotalKills} | Score: {Score}");
    }

    /// <inheritdoc/>
    public override void OnWaveComplete(int wave)
    {
        if (wave > HighestWave)
            HighestWave = wave;

        Debug.Log($"[Classic] Wave {wave} complete. Current score: {Score}");
    }

    /// <inheritdoc/>
    public override void OnEnemyKilled(EnemyBase enemy)
    {
        TotalKills++;
    }

    /// <inheritdoc/>
    public override void OnPlayerDeath()
    {
        Debug.Log("[Classic] Player died — run over.");
        GameModeManager.Instance?.EndCurrentMode(false);
    }
}
