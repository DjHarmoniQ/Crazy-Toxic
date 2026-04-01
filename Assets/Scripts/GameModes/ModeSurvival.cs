using UnityEngine;

/// <summary>
/// Survival Mode — Time Attack.
///
/// Rules:
/// <list type="bullet">
///   <item>Survive for 20 minutes.</item>
///   <item>Enemies spawn 50 % faster.</item>
///   <item>No card picker between waves — ability cards drop randomly from enemies (10 % drop rate).</item>
///   <item>Win condition: survive the full 20 minutes.</item>
/// </list>
/// </summary>
public class ModeSurvival : GameModeBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Total time the player must survive to win (seconds).</summary>
    public const float SurvivalDuration = 20f * 60f; // 20 minutes

    /// <summary>Multiplier applied to the enemy spawn rate.</summary>
    public const float SpawnRateMultiplier = 1.5f;

    /// <summary>Probability (0–1) that a killed enemy drops an ability card.</summary>
    public const float CardDropRate = 0.1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ModeName => "Survival";

    /// <inheritdoc/>
    public override string ModeDescription =>
        "Survive 20 minutes. Enemies spawn 50% faster. Cards drop from enemies only.";

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Elapsed time since the mode started (seconds).</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>Seconds remaining before the player wins.</summary>
    public float TimeRemaining => Mathf.Max(0f, SurvivalDuration - ElapsedTime);

    /// <summary><c>true</c> once the run has ended (win or loss).</summary>
    private bool _ended;

    // ─────────────────────────────────────────────────────────────────────────
    //  GameModeBase Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override void OnModeStart()
    {
        ElapsedTime = 0f;
        _ended = false;
        Debug.Log("[Survival] Mode started — survive for 20 minutes!");
    }

    /// <inheritdoc/>
    public override void OnModeEnd(bool victory)
    {
        _ended = true;
        string result = victory ? "VICTORY" : "DEFEAT";
        Debug.Log($"[Survival] Run ended ({result}). Survived: {ElapsedTime:F1}s");
    }

    /// <inheritdoc/>
    public override void OnWaveComplete(int wave)
    {
        // No card picker in Survival — cards come from enemy drops only.
        Debug.Log($"[Survival] Wave {wave} complete. No card picker.");
    }

    /// <inheritdoc/>
    public override void OnEnemyKilled(EnemyBase enemy)
    {
        // Card drop logic is handled by the enemy's LootTable / EnemySpawner.
        // This hook is available for additional mode-specific tracking.
    }

    /// <inheritdoc/>
    public override void OnPlayerDeath()
    {
        if (_ended) return;
        Debug.Log("[Survival] Player died before the timer expired — defeat.");
        GameModeManager.Instance?.EndCurrentMode(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Update
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_ended) return;

        ElapsedTime += Time.deltaTime;

        if (ElapsedTime >= SurvivalDuration)
        {
            Debug.Log("[Survival] 20 minutes survived — victory!");
            GameModeManager.Instance?.EndCurrentMode(true);
        }
    }
}
