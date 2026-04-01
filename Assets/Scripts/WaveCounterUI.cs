using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current wave number in the HUD.
/// Subscribes to <see cref="WaveManager.OnWaveChanged"/> and unsubscribes in
/// <see cref="OnDestroy"/> to prevent memory leaks.
///
/// Setup in the Inspector:
///   1. Attach to a UI GameObject that has a TextMeshProUGUI component.
///   2. Set the RectTransform anchor to <b>top-center</b>, position Y = -40.
///      (Comment reminder: "Set RectTransform anchor to top-center in the Inspector, position Y = -40")
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class WaveCounterUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI Reference")]
    [Tooltip("TextMeshProUGUI that shows the current wave. Auto-fetched from this GameObject if left empty.")]
    [SerializeField] private TextMeshProUGUI waveText;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Cache the TMP component if not assigned in the Inspector
        if (waveText == null)
            waveText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        // Subscribe to wave-change events so the display stays in sync
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged += OnWaveChanged;
            // Immediately display the current wave (set during WaveManager.Start)
            OnWaveChanged(WaveManager.Instance.CurrentWave);
        }
        else
        {
            Debug.LogWarning("[WaveCounterUI] WaveManager.Instance is null — wave display will not update.");
            SetWaveText(1);
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events in OnDestroy to prevent memory leaks
        // when this UI object is destroyed (scene change, pause-menu hide, etc.)
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= OnWaveChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Handler called by <see cref="WaveManager.OnWaveChanged"/>.</summary>
    private void OnWaveChanged(int wave)
    {
        SetWaveText(wave);
    }

    /// <summary>Updates the TMP text to display the wave number in bold.</summary>
    private void SetWaveText(int wave)
    {
        if (waveText != null)
            waveText.text = $"<b>WAVE {wave}</b>";
    }
}
