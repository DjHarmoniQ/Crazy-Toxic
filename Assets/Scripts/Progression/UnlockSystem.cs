using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes the condition required to unlock a character or card.
/// </summary>
[Serializable]
public struct UnlockCondition
{
    /// <summary>Human-readable description of the unlock requirement.</summary>
    public string description;
}

/// <summary>
/// Singleton that tracks which characters and ability cards the player has unlocked.
/// Persists state via <see cref="SaveSystem"/> and broadcasts unlock events.
///
/// Usage:
/// <code>
///   if (UnlockSystem.Instance.IsCharacterUnlocked("Mage")) { … }
///   UnlockSystem.Instance.UnlockCharacter("Rogue");
/// </code>
/// </summary>
public class UnlockSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="UnlockSystem"/> instance.</summary>
    public static UnlockSystem Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Default Unlocks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Characters available from the very first session.</summary>
    private static readonly string[] DefaultCharacters = { "Knight", "Archer" };

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when a character is newly unlocked.
    /// Parameter: the character's name.
    /// </summary>
    public event Action<string> OnCharacterUnlocked;

    /// <summary>
    /// Fired when a card is newly unlocked.
    /// Parameter: the card's name.
    /// </summary>
    public event Action<string> OnCardUnlocked;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unlock Conditions (read-only reference data)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps character names to their unlock condition descriptions.
    /// Checked externally (e.g., wave clear, kill milestones) via
    /// <see cref="UnlockCharacter"/>.
    /// </summary>
    public static readonly Dictionary<string, UnlockCondition> CharacterConditions =
        new Dictionary<string, UnlockCondition>
        {
            { "CharRogue",      new UnlockCondition { description = "Reach wave 15" } },
            { "CharMage",       new UnlockCondition { description = "Deal 10,000 total damage in a run" } },
            { "CharPaladin",    new UnlockCondition { description = "Survive 30 waves" } },
            { "CharBerserker",  new UnlockCondition { description = "Kill 500 enemies total (cumulative)" } },
            { "CharVoidWalker", new UnlockCondition { description = "Kill a boss without taking damage" } },
            { "CharNinja",      new UnlockCondition { description = "Reach wave 20" } },
            { "CharPriest",     new UnlockCondition { description = "Heal for a total of 1,000 HP in one run" } },
            { "CharElementalist", new UnlockCondition { description = "Kill 100 enemies with elemental damage" } },
            { "CharSummoner",   new UnlockCondition { description = "Have 5 summons active simultaneously" } },
            { "CharAlchemist",  new UnlockCondition { description = "Collect 15 ability cards in one run" } },
            { "CharBard",       new UnlockCondition { description = "Complete a run with only 1 HP remaining" } },
            { "CharDruid",      new UnlockCondition { description = "Kill 200 enemies with poison damage" } },
            { "CharTech",       new UnlockCondition { description = "Use all 7 ammo types in one run" } },
            { "CharDemonHunter",new UnlockCondition { description = "Kill the Void Dragon boss" } },
        };

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private readonly HashSet<string> _unlockedCharacters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unlockedCards      = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        PersistToSave();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Characters
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the named character has been unlocked.
    /// </summary>
    /// <param name="characterName">Case-insensitive character identifier.</param>
    public bool IsCharacterUnlocked(string characterName) =>
        _unlockedCharacters.Contains(characterName);

    /// <summary>
    /// Unlocks the named character, persists the change, and fires
    /// <see cref="OnCharacterUnlocked"/> if the character was not already unlocked.
    /// </summary>
    /// <param name="name">Case-insensitive character identifier.</param>
    public void UnlockCharacter(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_unlockedCharacters.Add(name))
        {
            Debug.Log($"[UnlockSystem] Character unlocked: {name}");
            PersistToSave();
            OnCharacterUnlocked?.Invoke(name);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Cards
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the named ability card has been unlocked.
    /// </summary>
    /// <param name="cardName">Case-insensitive card identifier.</param>
    public bool IsCardUnlocked(string cardName) =>
        _unlockedCards.Contains(cardName);

    /// <summary>
    /// Unlocks the named ability card, persists the change, and fires
    /// <see cref="OnCardUnlocked"/> if it was not already unlocked.
    /// </summary>
    /// <param name="name">Case-insensitive card identifier.</param>
    public void UnlockCard(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_unlockedCards.Add(name))
        {
            Debug.Log($"[UnlockSystem] Card unlocked: {name}");
            PersistToSave();
            OnCardUnlocked?.Invoke(name);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Writes current unlock state into the shared save file.</summary>
    private void PersistToSave()
    {
        SaveData data = SaveSystem.Load();

        var chars = new string[_unlockedCharacters.Count];
        _unlockedCharacters.CopyTo(chars);
        data.unlockedCharacters = chars;

        var cards = new string[_unlockedCards.Count];
        _unlockedCards.CopyTo(cards);
        data.unlockedCards = cards;

        SaveSystem.Save(data);
    }

    /// <summary>Restores unlocked characters and cards from the save file.</summary>
    private void LoadFromSave()
    {
        // Always start with the defaults
        foreach (string c in DefaultCharacters)
            _unlockedCharacters.Add(c);

        if (!SaveSystem.HasSaveData()) return;

        SaveData data = SaveSystem.Load();

        foreach (string c in data.unlockedCharacters)
            if (!string.IsNullOrWhiteSpace(c))
                _unlockedCharacters.Add(c);

        foreach (string card in data.unlockedCards)
            if (!string.IsNullOrWhiteSpace(card))
                _unlockedCards.Add(card);

        Debug.Log($"[UnlockSystem] Loaded — {_unlockedCharacters.Count} characters, " +
                  $"{_unlockedCards.Count} cards unlocked.");
    }
}
