using UnityEngine;

/// <summary>
/// Co-Op Mode — 2-player local co-op.
///
/// Rules:
/// <list type="bullet">
///   <item>Player 1: WASD + keyboard. Player 2: Arrow keys + numpad or controller.</item>
///   <item>Shared wave progression, separate HP bars.</item>
///   <item>If one player dies they enter a "downed" state; the other can revive them
///         by standing nearby for 3 seconds.</item>
///   <item>Double enemy count per wave.</item>
///   <item>Shared card picker — both players vote; majority wins, Player 1 decides on tie.</item>
///   <item>Combined score.</item>
/// </list>
/// </summary>
public class ModeCoOp : GameModeBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Multiplier applied to the enemy count each wave.</summary>
    public const float EnemyCountMultiplier = 2f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ModeName => "Co-Op";

    /// <inheritdoc/>
    public override string ModeDescription =>
        "2-player local co-op. Double enemies, shared waves, revive your partner!";

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Co-Op References")]
    [Tooltip("Reference to the CoOpPlayerManager that handles player 2 spawning and downed state.")]
    [SerializeField] private CoOpPlayerManager _coOpPlayerManager;

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Combined kill count for both players.</summary>
    public int TotalKills { get; private set; }

    /// <summary>Highest wave reached.</summary>
    public int HighestWave { get; private set; }

    /// <summary>Combined score: wave × 100 + kills × 10.</summary>
    public int Score => HighestWave * 100 + TotalKills * 10;

    /// <summary>Tracks whether each player is currently downed (indexed 0 and 1).</summary>
    private readonly bool[] _playerDowned = new bool[2];

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
        _playerDowned[0] = false;
        _playerDowned[1] = false;
        _ended = false;

        // Spawn second player.
        if (_coOpPlayerManager != null)
            _coOpPlayerManager.SpawnPlayer2();
        else
            Debug.LogWarning("[Co-Op] CoOpPlayerManager not assigned — Player 2 will not spawn.");

        Debug.Log("[Co-Op] Mode started — good luck, team!");
    }

    /// <inheritdoc/>
    public override void OnModeEnd(bool victory)
    {
        _ended = true;
        string result = victory ? "VICTORY" : "DEFEAT";
        Debug.Log($"[Co-Op] Run ended ({result}). Score: {Score}");
    }

    /// <inheritdoc/>
    public override void OnWaveComplete(int wave)
    {
        if (wave > HighestWave)
            HighestWave = wave;

        Debug.Log($"[Co-Op] Wave {wave} cleared. Combined score: {Score}");
    }

    /// <inheritdoc/>
    public override void OnEnemyKilled(EnemyBase enemy)
    {
        TotalKills++;
    }

    /// <inheritdoc/>
    public override void OnPlayerDeath()
    {
        // Player deaths are handled per-player via NotifyPlayerDowned.
        // A full game over only happens when both players are downed simultaneously.
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks <paramref name="playerIndex"/> as downed (0 = P1, 1 = P2).
    /// Triggers a game over if both players are downed at the same time.
    /// </summary>
    /// <param name="playerIndex">0-based player index.</param>
    public void NotifyPlayerDowned(int playerIndex)
    {
        if (_ended) return;

        _playerDowned[playerIndex] = true;
        Debug.Log($"[Co-Op] Player {playerIndex + 1} is downed!");

        if (_playerDowned[0] && _playerDowned[1])
        {
            Debug.Log("[Co-Op] Both players downed — defeat.");
            GameModeManager.Instance?.EndCurrentMode(false);
        }
    }

    /// <summary>
    /// Marks <paramref name="playerIndex"/> as revived and back in the fight.
    /// </summary>
    /// <param name="playerIndex">0-based player index.</param>
    public void NotifyPlayerRevived(int playerIndex)
    {
        _playerDowned[playerIndex] = false;
        Debug.Log($"[Co-Op] Player {playerIndex + 1} has been revived!");
    }

    /// <summary>
    /// Returns <c>true</c> if the player at <paramref name="playerIndex"/> is currently downed.
    /// </summary>
    /// <param name="playerIndex">0-based player index.</param>
    public bool IsPlayerDowned(int playerIndex) => _playerDowned[playerIndex];
}
