using UnityEngine;

/// <summary>
/// Singleton that manages the player's persistent XP and level progression.
/// Persists across scenes via <c>DontDestroyOnLoad</c> and auto-saves on changes.
///
/// Usage:
/// <code>
///   XPSystem.Instance.AddXP(25);
/// </code>
/// </summary>
public class XPSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Maximum level a player can reach.</summary>
    public const int MaxLevel = 50;

    // ── Enemy XP Rewards ─────────────────────────────────────────────────────

    /// <summary>XP awarded for killing a Common-tier enemy.</summary>
    public const int XP_Common    = 5;

    /// <summary>XP awarded for killing an Uncommon-tier enemy.</summary>
    public const int XP_Uncommon  = 10;

    /// <summary>XP awarded for killing an Elite-tier enemy.</summary>
    public const int XP_Elite     = 25;

    /// <summary>XP awarded for killing a Boss-tier enemy.</summary>
    public const int XP_Boss      = 100;

    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="XPSystem"/> instance.</summary>
    public static XPSystem Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Total XP accumulated by the player across all runs.</summary>
    public int TotalXP { get; private set; }

    /// <summary>Current player level (1 – <see cref="MaxLevel"/>).</summary>
    public int PlayerLevel { get; private set; } = 1;

    /// <summary>
    /// XP required to reach the next level.
    /// Formula: <c>round(100 × 1.15^level)</c> — exponential difficulty curve.
    /// Returns 0 when the player is at <see cref="MaxLevel"/>.
    /// </summary>
    public int XPToNextLevel
    {
        get
        {
            if (PlayerLevel >= MaxLevel) return 0;
            return Mathf.RoundToInt(100f * Mathf.Pow(1.15f, PlayerLevel));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when the player levels up.
    /// Parameter: the new level number.
    /// </summary>
    public event System.Action<int> OnLevelUp;

    /// <summary>
    /// Fired whenever XP is added.
    /// Parameters: (amount gained, new total XP).
    /// </summary>
    public event System.Action<int, int> OnXPGained;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>XP accumulated toward the next level-up threshold.</summary>
    private int _xpInCurrentLevel;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromSave();
    }

    private void OnApplicationQuit()
    {
        SaveToFile();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds <paramref name="amount"/> XP to the player's total, triggers any
    /// level-ups that result, then persists the new state.
    /// </summary>
    /// <param name="amount">Non-negative amount of XP to award.</param>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        TotalXP += amount;
        _xpInCurrentLevel += amount;
        OnXPGained?.Invoke(amount, TotalXP);

        // Check for one or more level-ups
        while (PlayerLevel < MaxLevel && _xpInCurrentLevel >= XPToNextLevel)
        {
            _xpInCurrentLevel -= XPToNextLevel;
            PlayerLevel++;
            Debug.Log($"[XPSystem] Level up! Now level {PlayerLevel}.");
            OnLevelUp?.Invoke(PlayerLevel);
        }

        // Cap overflow XP at max level
        if (PlayerLevel >= MaxLevel) _xpInCurrentLevel = 0;

        SaveToFile();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Persists XP/level data into the shared save file.</summary>
    private void SaveToFile()
    {
        SaveData data = SaveSystem.Load();
        data.totalXP     = TotalXP;
        data.playerLevel = PlayerLevel;
        SaveSystem.Save(data);
    }

    /// <summary>Restores XP/level from the save file on startup.</summary>
    private void LoadFromSave()
    {
        SaveData data = SaveSystem.Load();
        TotalXP     = data.totalXP;
        PlayerLevel = Mathf.Clamp(data.playerLevel, 1, MaxLevel);

        // Recompute _xpInCurrentLevel from TotalXP and all previous thresholds
        int xpSpent = 0;
        for (int lvl = 1; lvl < PlayerLevel; lvl++)
            xpSpent += Mathf.RoundToInt(100f * Mathf.Pow(1.15f, lvl));

        _xpInCurrentLevel = TotalXP - xpSpent;

        Debug.Log($"[XPSystem] Loaded — Level {PlayerLevel}, TotalXP {TotalXP}.");
    }
}
