using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the ultimate-ability cooldown ring UI.
/// Attach to a Canvas GameObject that has a radial-fill Image, a name label,
/// and a countdown timer label as children.
///
/// <list type="bullet">
///   <item>The fill image fills from 0 to 1 as the cooldown expires.</item>
///   <item>The image grays out when the player does not have enough mana.</item>
///   <item>Pressing Q while the ultimate is available triggers a brief flash.</item>
/// </list>
/// </summary>
public class UltimateCooldownUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector References
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI Components")]
    [Tooltip("Radial-fill Image that represents how much of the cooldown has elapsed.")]
    [SerializeField] private Image cooldownFill;

    [Tooltip("Text label that shows the ultimate's display name.")]
    [SerializeField] private TextMeshProUGUI ultimateNameText;

    [Tooltip("Text label that shows the remaining cooldown in seconds (e.g. '3.2s').")]
    [SerializeField] private TextMeshProUGUI cooldownTimerText;

    [Header("Colors")]
    [Tooltip("Normal tint when the ultimate is ready.")]
    [SerializeField] private Color _readyColor = Color.white;

    [Tooltip("Tint applied when the player lacks mana.")]
    [SerializeField] private Color _noManaColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Tooltip("Tint applied when the cooldown is still running.")]
    [SerializeField] private Color _cooldownColor = new Color(0.6f, 0.6f, 1f, 1f);

    [Header("Flash Animation")]
    [Tooltip("Duration in seconds of the Q-press activation flash.")]
    [SerializeField] private float _flashDuration = 0.25f;

    [Tooltip("Color of the brief flash shown when Q is pressed.")]
    [SerializeField] private Color _flashColor = new Color(1f, 1f, 0.5f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private CharacterAbilityHandler _handler;
    private float _flashTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Find the handler on the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _handler = player.GetComponent<CharacterAbilityHandler>();

        if (_handler == null)
            Debug.LogWarning("[UltimateCooldownUI] No CharacterAbilityHandler found on the Player.");

        // Show ultimate name if CharacterData is available via GameManager
        if (ultimateNameText != null &&
            GameManager.Instance != null &&
            GameManager.Instance.SelectedCharacter != null)
        {
            ultimateNameText.text = GameManager.Instance.SelectedCharacter.ultimateName;
        }
    }

    private void Update()
    {
        if (_handler == null) return;

        UpdateFill();
        UpdateTimerText();
        UpdateColor();
        HandleFlash();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the radial fill amount based on how much of the cooldown has elapsed.
    /// Fill = 1 when ready, fill = 0 immediately after use.
    /// </summary>
    private void UpdateFill()
    {
        if (cooldownFill == null) return;

        float cooldownDuration = _handler.UltimateCooldown;
        if (cooldownDuration <= 0f)
        {
            cooldownFill.fillAmount = 1f;
            return;
        }

        float remaining = _handler.RemainingCooldown;
        cooldownFill.fillAmount = 1f - Mathf.Clamp01(remaining / cooldownDuration);
    }

    /// <summary>
    /// Updates the countdown text, showing "Ready!" when available or "X.Xs" when on cooldown.
    /// </summary>
    private void UpdateTimerText()
    {
        if (cooldownTimerText == null) return;

        float remaining = _handler.RemainingCooldown;
        cooldownTimerText.text = remaining > 0f ? $"{remaining:F1}s" : "Ready!";
    }

    /// <summary>
    /// Tints the fill image to communicate availability: white = ready,
    /// gray = no mana, blue = cooling down.
    /// </summary>
    private void UpdateColor()
    {
        if (cooldownFill == null) return;

        if (_flashTimer > 0f) return; // color is handled by HandleFlash during flash

        if (_handler.RemainingCooldown > 0f)
            cooldownFill.color = _cooldownColor;
        else if (!_handler.CanUseUltimate)
            cooldownFill.color = _noManaColor;
        else
            cooldownFill.color = _readyColor;
    }

    /// <summary>
    /// Triggers and ticks the brief activation flash when the player presses Q
    /// while the ultimate is available.
    /// </summary>
    private void HandleFlash()
    {
        if (Input.GetKeyDown(KeyCode.Q) && _handler.CanUseUltimate)
            _flashTimer = _flashDuration;

        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;

            if (cooldownFill != null)
                cooldownFill.color = _flashTimer > 0f ? _flashColor : _readyColor;
        }
    }
}
