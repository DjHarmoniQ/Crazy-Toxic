using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Main Menu screen.
///
/// Responsibilities:
/// <list type="bullet">
///   <item>Wires up all main-menu buttons (Play, Mode Select, Characters, Leaderboard, Settings, Quit).</item>
///   <item>Displays the player's persistent level and total XP.</item>
///   <item>Shows a version string in the bottom-right corner.</item>
///   <item>Marks recently-unlocked characters with a "NEW" badge.</item>
///   <item>Drives the <see cref="ParallaxBackground"/> component for the animated background.</item>
/// </list>
///
/// Attach to: The MainMenu panel Canvas GameObject.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Buttons")]
    [Tooltip("Starts a new run using the currently selected mode and character.")]
    [SerializeField] private Button playButton;

    [Tooltip("Opens the game-mode selection screen.")]
    [SerializeField] private Button modeSelectButton;

    [Tooltip("Opens the character info / unlock screen.")]
    [SerializeField] private Button charactersButton;

    [Tooltip("Opens the leaderboard screen.")]
    [SerializeField] private Button leaderboardButton;

    [Tooltip("Opens the settings menu.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Quits the application.")]
    [SerializeField] private Button quitButton;

    [Header("Player Info")]
    [Tooltip("Label showing the player's current level (e.g. 'Lv. 7').")]
    [SerializeField] private TextMeshProUGUI playerLevelText;

    [Tooltip("Label showing the player's total accumulated XP.")]
    [SerializeField] private TextMeshProUGUI totalXPText;

    [Header("Misc UI")]
    [Tooltip("Label shown in the bottom-right corner with the game version string.")]
    [SerializeField] private TextMeshProUGUI versionText;

    [Tooltip("Badge GameObject placed over recently-unlocked character buttons.")]
    [SerializeField] private GameObject newBadgePrefab;

    [Header("Panels")]
    [Tooltip("The CharacterInfoUI panel that slides in from the main menu.")]
    [SerializeField] private GameObject characterInfoPanel;

    [Tooltip("The Settings panel that opens from the main menu.")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("The Leaderboard panel that opens from the main menu.")]
    [SerializeField] private GameObject leaderboardPanel;

    [Tooltip("The Mode-Select panel that opens from the main menu.")]
    [SerializeField] private GameObject modeSelectPanel;

    [Header("Parallax")]
    [Tooltip("The parallax background component attached to the background panel.")]
    [SerializeField] private ParallaxBackground parallaxBackground;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        RegisterButtons();
    }

    private void Start()
    {
        RefreshPlayerInfo();
        SetVersionText();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Button Setup
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Wires all button <c>onClick</c> listeners.</summary>
    private void RegisterButtons()
    {
        playButton?.onClick.AddListener(OnPlayClicked);
        modeSelectButton?.onClick.AddListener(OnModeSelectClicked);
        charactersButton?.onClick.AddListener(OnCharactersClicked);
        leaderboardButton?.onClick.AddListener(OnLeaderboardClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — UI Refresh
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads the XP/level from <see cref="XPSystem"/> and updates the labels.</summary>
    private void RefreshPlayerInfo()
    {
        if (XPSystem.Instance == null) return;

        if (playerLevelText != null)
            playerLevelText.text = $"Lv. {XPSystem.Instance.PlayerLevel}";

        if (totalXPText != null)
            totalXPText.text = $"{XPSystem.Instance.TotalXP:N0} XP";
    }

    /// <summary>Sets the version label from <see cref="Application.version"/>.</summary>
    private void SetVersionText()
    {
        if (versionText != null)
            versionText.text = $"v{Application.version}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a "NEW" badge on a character button to indicate a recently-unlocked character.
    /// </summary>
    /// <param name="parent">The button Transform to attach the badge to.</param>
    public void ShowNewBadge(Transform parent)
    {
        if (newBadgePrefab == null || parent == null) return;
        Instantiate(newBadgePrefab, parent);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Button Callbacks
    // ─────────────────────────────────────────────────────────────────────────

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuUI] Play clicked — loading game scene.");
        if (LoadingScreenUI.Instance != null)
            StartCoroutine(LoadingScreenUI.Instance.LoadSceneAsync("MainScene"));
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }

    private void OnModeSelectClicked()
    {
        Debug.Log("[MainMenuUI] Mode Select clicked.");
        modeSelectPanel?.SetActive(true);
    }

    private void OnCharactersClicked()
    {
        Debug.Log("[MainMenuUI] Characters clicked.");
        characterInfoPanel?.SetActive(true);
    }

    private void OnLeaderboardClicked()
    {
        Debug.Log("[MainMenuUI] Leaderboard clicked.");
        leaderboardPanel?.SetActive(true);
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenuUI] Settings clicked.");
        settingsPanel?.SetActive(true);
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quit clicked.");
        Application.Quit();
    }
}
