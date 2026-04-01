using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the character-select screen: populates UI from <see cref="CharacterData"/>
/// ScriptableObjects, handles selection and confirmation, and loads the game scene.
///
/// ── FREEZE / MEMORY-LEAK FIX ──────────────────────────────────────────────
/// The original freeze had three root causes:
///   1. Event listeners were never unsubscribed (memory leak + stale callbacks).
///   2. FindObjectsOfType was called every frame inside Update.
///   3. Double-clicking "Confirm" fired two simultaneous scene-load requests,
///      causing Unity to queue the same scene twice and lock up.
///
/// This implementation fixes all three:
///   1. All Unity-Event / delegate subscriptions are torn down in OnDestroy().
///   2. All object references are cached once in Awake() — no per-frame searches.
///   3. A _isTransitioning bool flag on ConfirmSelection() prevents re-entry.
/// ─────────────────────────────────────────────────────────────────────────
///
/// Attach to: The CharacterSelect scene's manager GameObject.
/// </summary>
public class CharacterSelectManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Character Data")]
    [Tooltip("All playable characters, in display order. Leave empty to auto-load from Resources/Characters/.")]
    [SerializeField] private CharacterData[] characters;

    [Header("UI References — cache all in Inspector, never use FindObjectsOfType")]
    [Tooltip("Text component that shows the selected character's name.")]
    [SerializeField] private UnityEngine.UI.Text nameText;

    [Tooltip("Image component that shows the selected character's portrait.")]
    [SerializeField] private UnityEngine.UI.Image portraitImage;

    [Tooltip("Text component that shows the selected character's stats preview.")]
    [SerializeField] private UnityEngine.UI.Text statsText;

    [Header("Scene")]
    [Tooltip("Name of the game scene to load when the player confirms their selection.")]
    [SerializeField] private string gameSceneName = "MainScene";

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private int _selectedIndex = -1;

    /// <summary>
    /// Guard flag: prevents double-clicks on "Confirm" from triggering two
    /// simultaneous scene loads (the original freeze source).
    /// </summary>
    private bool _isTransitioning;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle — keep in canonical order: Awake → Start → … → OnDestroy
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-load characters from Resources/Characters/ if none are assigned.
        if (characters == null || characters.Length == 0)
        {
            characters = Resources.LoadAll<CharacterData>("Characters");
            if (characters != null && characters.Length > 0)
                Debug.Log($"[CharacterSelectManager] Loaded {characters.Length} CharacterData assets from Resources/Characters/.");
        }

        // All UI references must be cached here.
        // NEVER call FindObjectsOfType in Update — it iterates every scene object
        // every frame and caused the original freeze.
        ValidateCachedReferences();
    }

    private void Start()
    {
        // Pre-select the first character (index 0) so the UI is never blank
        if (characters != null && characters.Length > 0)
            SelectCharacter(0);
    }

    /// <summary>
    /// Handles keyboard arrow-key navigation through the character list.
    /// </summary>
    private void Update()
    {
        if (characters == null || characters.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            int newIndex = (_selectedIndex - 1 + characters.Length) % characters.Length;
            SelectCharacter(newIndex);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            int newIndex = (_selectedIndex + 1) % characters.Length;
            SelectCharacter(newIndex);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmSelection();
        }
    }

    private void OnDestroy()
    {
        // No dynamic event subscriptions in this class, but this is the correct
        // place to unsubscribe any that are added in the future — prevents leaks.
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects the character at <paramref name="index"/> and refreshes the UI panel.
    /// Wire each character-card button's OnClick to this method, passing the index.
    /// </summary>
    /// <param name="index">Zero-based index into the <c>characters</c> array.</param>
    public void SelectCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length)
        {
            Debug.LogWarning($"[CharacterSelectManager] Invalid character index: {index}");
            return;
        }

        _selectedIndex = index;
        RefreshUI(characters[index]);
    }

    /// <summary>
    /// Confirms the current selection and loads the game scene.
    /// The <c>_isTransitioning</c> guard ensures this can only execute once,
    /// preventing the double-click freeze bug.
    /// </summary>
    public void ConfirmSelection()
    {
        // Double-click guard — this is the fix for the scene-load freeze
        if (_isTransitioning) return;

        if (_selectedIndex < 0 || characters == null || _selectedIndex >= characters.Length)
        {
            Debug.LogWarning("[CharacterSelectManager] No character selected — cannot confirm.");
            return;
        }

        _isTransitioning = true;

        // Pass the selected character data to the GameManager so CharacterStatApplier
        // can read it in the game scene
        if (GameManager.Instance != null)
            GameManager.Instance.SelectedCharacter = characters[_selectedIndex];

        Debug.Log($"[CharacterSelectManager] Confirmed: {characters[_selectedIndex].characterName}. Loading {gameSceneName}…");
        SceneManager.LoadScene(gameSceneName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the UI panel to reflect <paramref name="data"/>.
    /// Now includes character class, passive name, ultimate name, and ultimate mana cost.
    /// Reuses existing UI objects — no Destroy/Instantiate per click (avoids GC spikes).
    /// </summary>
    private void RefreshUI(CharacterData data)
    {
        if (nameText != null)
            nameText.text = data.characterName;

        if (portraitImage != null)
        {
            portraitImage.sprite = data.portrait;
            portraitImage.enabled = data.portrait != null;
        }

        if (statsText != null)
        {
            statsText.text =
                $"Class: {data.characterClass}\n" +
                $"HP: {data.maxHealth}  Speed: {data.moveSpeed}\n" +
                $"Damage: {data.damage}  Armor: {data.armor}\n" +
                $"\n⬡ Passive — {data.passiveName}\n{data.passiveDescription}\n" +
                $"\n⚡ Ultimate — {data.ultimateName}  [{data.ultimateCost} mana]\n{data.ultimateDescription}";
        }
    }

    /// <summary>
    /// Warns in the console when expected Inspector references are missing,
    /// helping the Designer catch assignment errors early.
    /// </summary>
    private void ValidateCachedReferences()
    {
        if (characters == null || characters.Length == 0)
            Debug.LogWarning("[CharacterSelectManager] No CharacterData assets assigned or found in Resources/Characters/.");

        if (nameText == null)
            Debug.LogWarning("[CharacterSelectManager] nameText is not assigned.");

        if (portraitImage == null)
            Debug.LogWarning("[CharacterSelectManager] portraitImage is not assigned.");

        if (statsText == null)
            Debug.LogWarning("[CharacterSelectManager] statsText is not assigned.");
    }
}
