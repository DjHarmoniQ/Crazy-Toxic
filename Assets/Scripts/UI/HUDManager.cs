using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Coordinates all HUD elements shown during gameplay.
///
/// Layout:
/// <list type="bullet">
///   <item>Top-left    — player portrait, HP bar, mana bar.</item>
///   <item>Top-center  — wave counter, run timer.</item>
///   <item>Top-right   — ammo type icon + count.</item>
///   <item>Bottom-center — ultimate cooldown button (Q).</item>
///   <item>Bottom-right  — last 5 active card icons.</item>
///   <item>Top overlay  — boss HP bar (slides in).</item>
///   <item>Center      — combo counter (fades when combo resets).</item>
/// </list>
///
/// Attach to: The HUD root panel inside the gameplay Canvas.
/// </summary>
public class HUDManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="HUDManager"/> instance.</summary>
    public static HUDManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Player Info — Top Left")]
    [Tooltip("Portrait image for the active character.")]
    [SerializeField] private Image playerPortrait;

    [Tooltip("Slider representing the player's current HP.")]
    [SerializeField] private Slider hpBar;

    [Tooltip("Slider representing the player's current mana.")]
    [SerializeField] private Slider manaBar;

    [Header("Wave Info — Top Center")]
    [Tooltip("Label showing the current wave number.")]
    [SerializeField] private TextMeshProUGUI waveCounterText;

    [Tooltip("Label showing elapsed run time (mm:ss).")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Ammo Info — Top Right")]
    [Tooltip("Icon image for the currently selected ammo type.")]
    [SerializeField] private Image ammoIcon;

    [Tooltip("Label showing the remaining ammo count.")]
    [SerializeField] private TextMeshProUGUI ammoCountText;

    [Header("Active Cards — Bottom Right")]
    [Tooltip("Up to 5 small icon images for the player's active ability cards.")]
    [SerializeField] private Image[] cardIcons;

    [Header("Combo — Center Screen")]
    [Tooltip("Root GameObject for the combo counter (enabled/disabled on milestone).")]
    [SerializeField] private GameObject comboRoot;

    [Tooltip("Label showing the current combo number.")]
    [SerializeField] private TextMeshProUGUI comboText;

    [Tooltip("Seconds the combo display lingers before fading after the combo resets.")]
    [SerializeField] private float comboFadeDelay = 1.5f;

    [Header("Boss HP Bar")]
    [Tooltip("Root panel for the boss HP bar — starts hidden, slides in when a boss spawns.")]
    [SerializeField] private GameObject bossHPBarRoot;

    [Tooltip("Slider that fills with the current boss HP percentage.")]
    [SerializeField] private Slider bossHPSlider;

    [Tooltip("Label showing the boss name.")]
    [SerializeField] private TextMeshProUGUI bossNameText;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _comboFadeTimer;
    private bool  _comboVisible;

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
    }

    private void Start()
    {
        // Boss bar starts hidden
        bossHPBarRoot?.SetActive(false);
        comboRoot?.SetActive(false);
    }

    private void Update()
    {
        UpdateTimer();
        UpdateWaveCounter();
        TickComboFade();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Visibility
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Makes the entire HUD visible.</summary>
    public void ShowHUD() => gameObject.SetActive(true);

    /// <summary>Hides the entire HUD.</summary>
    public void HideHUD() => gameObject.SetActive(false);

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — HP / Mana
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Updates the HP bar fill ratio.</summary>
    /// <param name="current">Current HP value.</param>
    /// <param name="max">Maximum HP value.</param>
    public void UpdateHP(float current, float max)
    {
        if (hpBar == null) return;
        hpBar.value = max > 0f ? current / max : 0f;
    }

    /// <summary>Updates the mana bar fill ratio.</summary>
    /// <param name="current">Current mana value.</param>
    /// <param name="max">Maximum mana value.</param>
    public void UpdateMana(float current, float max)
    {
        if (manaBar == null) return;
        manaBar.value = max > 0f ? current / max : 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Ammo
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Refreshes the ammo icon and count label.</summary>
    /// <param name="icon">Sprite for the current ammo type.</param>
    /// <param name="count">Remaining ammo count (-1 = infinite).</param>
    public void UpdateAmmo(Sprite icon, int count)
    {
        if (ammoIcon != null)
        {
            ammoIcon.sprite  = icon;
            ammoIcon.enabled = icon != null;
        }
        if (ammoCountText != null)
            ammoCountText.text = count < 0 ? "∞" : count.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Combo
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the combo display.  Scales the text up on milestone values (10, 25, 50 …).
    /// Starts a fade-out timer so the display disappears after the combo expires.
    /// </summary>
    /// <param name="combo">Current combo count (0 = reset).</param>
    public void UpdateComboDisplay(int combo)
    {
        if (comboRoot == null) return;

        if (combo <= 0)
        {
            // Begin fade
            _comboFadeTimer = comboFadeDelay;
            return;
        }

        comboRoot.SetActive(true);
        _comboVisible   = true;
        _comboFadeTimer = 0f;

        if (comboText != null)
            comboText.text = $"x{combo} COMBO";

        // Scale punch on milestone counts
        if (combo % 10 == 0)
            StartCoroutine(PunchScaleCombo());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Combo punch scale coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator PunchScaleCombo()
    {
        if (comboText == null) yield break;
        float duration = 0.1f;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            comboText.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.4f, t);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            comboText.transform.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, t);
            yield return null;
        }
        comboText.transform.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Boss HP Bar
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Shows the boss HP bar and sets the boss name.</summary>
    /// <param name="bossName">Display name of the boss.</param>
    public void ShowBossBar(string bossName)
    {
        if (bossHPBarRoot == null) return;
        bossHPBarRoot.SetActive(true);
        if (bossNameText != null) bossNameText.text = bossName;
    }

    /// <summary>Hides the boss HP bar.</summary>
    public void HideBossBar() => bossHPBarRoot?.SetActive(false);

    /// <summary>Updates the boss HP bar fill ratio.</summary>
    /// <param name="current">Current boss HP.</param>
    /// <param name="max">Boss maximum HP.</param>
    public void UpdateBossHP(float current, float max)
    {
        if (bossHPSlider == null) return;
        bossHPSlider.value = max > 0f ? current / max : 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Card Icons
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Refreshes the bottom-right card icon strip with the most recent cards.</summary>
    /// <param name="icons">Ordered list of card sprites (newest first, up to 5).</param>
    public void UpdateCardIcons(List<Sprite> icons)
    {
        if (cardIcons == null) return;
        for (int i = 0; i < cardIcons.Length; i++)
        {
            if (cardIcons[i] == null) continue;
            bool hasIcon = icons != null && i < icons.Count && icons[i] != null;
            cardIcons[i].sprite  = hasIcon ? icons[i] : null;
            cardIcons[i].enabled = hasIcon;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Player Portrait
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Sets the player portrait image.</summary>
    /// <param name="portrait">The character portrait sprite.</param>
    public void SetPlayerPortrait(Sprite portrait)
    {
        if (playerPortrait != null)
            playerPortrait.sprite = portrait;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads elapsed time from <see cref="RunStatsTracker"/> and updates the timer label.</summary>
    private void UpdateTimer()
    {
        if (timerText == null) return;
        float elapsed = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.RunDuration : 0f;
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    /// <summary>Reads the current wave from <see cref="WaveManager"/> and updates the wave label.</summary>
    private void UpdateWaveCounter()
    {
        if (waveCounterText == null) return;
        int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
        waveCounterText.text = $"Wave {wave}";
    }

    /// <summary>Ticks the combo fade-out timer and hides the combo display when it expires.</summary>
    private void TickComboFade()
    {
        if (!_comboVisible || _comboFadeTimer <= 0f) return;

        _comboFadeTimer -= Time.deltaTime;
        if (_comboFadeTimer <= 0f)
        {
            comboRoot?.SetActive(false);
            _comboVisible = false;
        }
    }
}
