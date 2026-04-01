using UnityEngine;

/// <summary>
/// Enumeration of all 30 achievements in Crazy-Toxic.
/// The integer value of each entry is used as the index into
/// <see cref="SaveData.achievementsUnlocked"/>.
/// </summary>
public enum AchievementID
{
    FirstBlood         =  0,
    WaveRider          =  1,
    Unstoppable        =  2,
    LegendaryRun       =  3,
    Centurion          =  4,
    ComboKing          =  5,
    Untouchable        =  6,
    BossSlayer         =  7,
    DragonSlayer       =  8,
    Collector          =  9,
    Hoarder            = 10,
    ManaMaster         = 11,
    FullHouse          = 12,
    ExplosivePersonality = 13,
    SpeedDemon         = 14,
    IronWill           = 15,
    Pacifist           = 16,
    GlassCannon        = 17,
    Cursed             = 18,
    Unkillable         = 19,
    LegendaryFind      = 20,
    AllIn              = 21,
    NoHits             = 22,
    Speedrunner        = 23,
    Veteran            = 24,
    Millionaire        = 25,
    AmmoExpert         = 26,
    PerfectScore       = 27,
    CoopChampion       = 28,
    Prestige           = 29,
}

/// <summary>
/// Data for a single achievement (name, description, unlock state).
/// </summary>
[System.Serializable]
public class Achievement
{
    /// <summary>Unique identifier for this achievement.</summary>
    public AchievementID id;

    /// <summary>Short display name shown in the UI.</summary>
    public string name;

    /// <summary>Longer description of the unlock condition.</summary>
    public string description;

    /// <summary>Whether the player has already unlocked this achievement.</summary>
    public bool isUnlocked;
}

/// <summary>
/// Singleton that defines, checks, and unlocks all 30 achievements.
/// Persists achievement progress via <see cref="SaveSystem"/>.
///
/// Usage:
/// <code>
///   AchievementSystem.Instance.CheckAchievement(AchievementID.FirstBlood);
/// </code>
/// </summary>
public class AchievementSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="AchievementSystem"/> instance.</summary>
    public static AchievementSystem Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when an achievement is newly unlocked.
    /// Parameter: the unlocked <see cref="Achievement"/> data.
    /// </summary>
    public event System.Action<Achievement> OnAchievementUnlocked;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Achievement[] _achievements;

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
        BuildAchievements();
        LoadFromSave();
    }

    private void OnApplicationQuit()
    {
        PersistToSave();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="Achievement"/> data for the given ID.
    /// </summary>
    /// <param name="id">The achievement to retrieve.</param>
    public Achievement GetAchievement(AchievementID id) => _achievements[(int)id];

    /// <summary>
    /// Marks the specified achievement as unlocked, saves the result, and fires
    /// <see cref="OnAchievementUnlocked"/>. Does nothing if already unlocked.
    /// </summary>
    /// <param name="id">The achievement to check and potentially unlock.</param>
    public void CheckAchievement(AchievementID id)
    {
        int index = (int)id;
        if (index < 0 || index >= _achievements.Length) return;

        Achievement ach = _achievements[index];
        if (ach.isUnlocked) return;

        ach.isUnlocked = true;
        Debug.Log($"[AchievementSystem] Achievement unlocked: {ach.name}");
        PersistToSave();
        OnAchievementUnlocked?.Invoke(ach);
    }

    /// <summary>
    /// Returns a snapshot array of all 30 achievements and their current state.
    /// </summary>
    public Achievement[] GetAllAchievements() => _achievements;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Initialises the static achievement catalogue.</summary>
    private void BuildAchievements()
    {
        _achievements = new Achievement[]
        {
            new Achievement { id = AchievementID.FirstBlood,          name = "First Blood",          description = "Kill your first enemy." },
            new Achievement { id = AchievementID.WaveRider,           name = "Wave Rider",            description = "Reach wave 10." },
            new Achievement { id = AchievementID.Unstoppable,         name = "Unstoppable",           description = "Reach wave 25." },
            new Achievement { id = AchievementID.LegendaryRun,        name = "Legendary Run",         description = "Reach wave 50." },
            new Achievement { id = AchievementID.Centurion,           name = "Centurion",             description = "Reach wave 100." },
            new Achievement { id = AchievementID.ComboKing,           name = "Combo King",            description = "Reach a 50x combo." },
            new Achievement { id = AchievementID.Untouchable,         name = "Untouchable",           description = "Complete a wave without taking damage." },
            new Achievement { id = AchievementID.BossSlayer,          name = "Boss Slayer",           description = "Kill your first boss." },
            new Achievement { id = AchievementID.DragonSlayer,        name = "Dragon Slayer",         description = "Kill the Void Dragon." },
            new Achievement { id = AchievementID.Collector,           name = "Collector",             description = "Collect 10 ability cards in one run." },
            new Achievement { id = AchievementID.Hoarder,             name = "Hoarder",               description = "Collect 20 ability cards in one run." },
            new Achievement { id = AchievementID.ManaMaster,          name = "Mana Master",           description = "Reach 200 max mana." },
            new Achievement { id = AchievementID.FullHouse,           name = "Full House",            description = "Unlock all 14 characters." },
            new Achievement { id = AchievementID.ExplosivePersonality,name = "Explosive Personality", description = "Kill 50 enemies with explosion damage." },
            new Achievement { id = AchievementID.SpeedDemon,          name = "Speed Demon",           description = "Complete wave 5 in under 60 seconds." },
            new Achievement { id = AchievementID.IronWill,            name = "Iron Will",             description = "Win a run with only 1 HP remaining." },
            new Achievement { id = AchievementID.Pacifist,            name = "Pacifist",              description = "Complete wave 3 using only melee." },
            new Achievement { id = AchievementID.GlassCannon,         name = "Glass Cannon",          description = "Deal 1,000 damage in a single hit." },
            new Achievement { id = AchievementID.Cursed,              name = "Cursed",                description = "Get hit by 100 status effects." },
            new Achievement { id = AchievementID.Unkillable,          name = "Unkillable",            description = "Activate the ReviveOnce card." },
            new Achievement { id = AchievementID.LegendaryFind,       name = "Legendary Find",        description = "Find a Legendary-rarity card." },
            new Achievement { id = AchievementID.AllIn,               name = "All In",                description = "Play as every character at least once." },
            new Achievement { id = AchievementID.NoHits,              name = "No Hits",               description = "Kill a boss without taking any damage." },
            new Achievement { id = AchievementID.Speedrunner,         name = "Speedrunner",           description = "Beat the game in under 90 minutes." },
            new Achievement { id = AchievementID.Veteran,             name = "Veteran",               description = "Complete 10 full runs." },
            new Achievement { id = AchievementID.Millionaire,         name = "Millionaire",           description = "Earn 10,000 total gold." },
            new Achievement { id = AchievementID.AmmoExpert,          name = "Ammo Expert",           description = "Use all 7 ammo types in one run." },
            new Achievement { id = AchievementID.PerfectScore,        name = "Perfect Score",         description = "Achieve max combo on a boss kill." },
            new Achievement { id = AchievementID.CoopChampion,        name = "Co-op Champion",        description = "Win a co-op run." },
            new Achievement { id = AchievementID.Prestige,            name = "Prestige",              description = "Reach wave 100 or beyond." },
        };
    }

    /// <summary>Reads the persisted unlock flags from the save file.</summary>
    private void LoadFromSave()
    {
        if (!SaveSystem.HasSaveData()) return;

        SaveData data = SaveSystem.Load();
        bool[] flags  = data.achievementsUnlocked;

        for (int i = 0; i < _achievements.Length && i < flags.Length; i++)
            _achievements[i].isUnlocked = flags[i];

        Debug.Log("[AchievementSystem] Achievement states loaded.");
    }

    /// <summary>Writes current unlock flags back to the save file.</summary>
    private void PersistToSave()
    {
        SaveData data = SaveSystem.Load();

        if (data.achievementsUnlocked == null || data.achievementsUnlocked.Length < _achievements.Length)
            data.achievementsUnlocked = new bool[_achievements.Length];

        for (int i = 0; i < _achievements.Length; i++)
            data.achievementsUnlocked[i] = _achievements[i].isUnlocked;

        SaveSystem.Save(data);
    }
}
