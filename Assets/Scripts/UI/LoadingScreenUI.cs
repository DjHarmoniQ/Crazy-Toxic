using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Full-screen loading overlay that loads a Unity scene asynchronously while
/// displaying an animated progress bar and a random gameplay tip.
///
/// Usage:
/// <code>
///   StartCoroutine(LoadingScreenUI.Instance.LoadSceneAsync("MainScene"));
/// </code>
///
/// Attach to: A persistent Canvas GameObject (DontDestroyOnLoad recommended).
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="LoadingScreenUI"/> instance.</summary>
    public static LoadingScreenUI Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("Slider that fills as the scene loads (0 → 1).")]
    [SerializeField] private Slider loadingBar;

    [Tooltip("Text label that displays a random gameplay tip during loading.")]
    [SerializeField] private TextMeshProUGUI tipText;

    [Tooltip("The root canvas group — fades in/out with the loading screen.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [Tooltip("Duration in seconds of the fade-in and fade-out transitions.")]
    [SerializeField] private float fadeDuration = 0.4f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Tips Data
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly string[] _tips =
    {
        "Tip: Hold the fire button to charge your shot for extra damage.",
        "Tip: Collecting cards with the same rarity increases their bonus effects.",
        "Tip: The Knight's passive reduces the first hit of every wave to 1 damage.",
        "Tip: Poison ammo stacks — keep hitting to ramp up DPS!",
        "Tip: Every 10th wave is a mini-boss wave. Save your ultimate for it.",
        "Tip: Picking up gold lets you purchase cards from the mid-run shop.",
        "Tip: The Mage deals bonus damage to enemies affected by status effects.",
        "Tip: Enemy projectiles can be parried by dashing through them (Rogue).",
        "Tip: Wave 50 and Wave 100 each trigger a legendary boss encounter.",
        "Tip: The Void Walker can teleport through walls — use it to dodge.",
        "Tip: Combo multipliers raise the XP you earn at the end of a run.",
        "Tip: Environmental hazards damage enemies too — lure them into spikes!",
        "Tip: The Priest can revive co-op teammates once per wave.",
        "Tip: Ice ammo slows enemies; pair it with the Berserker's passive for huge DPS.",
        "Tip: Ability cards have 5 rarity tiers: Common, Rare, Epic, Legendary, Mythic.",
        "Tip: Turn off music in Settings to reduce CPU load on lower-end devices.",
        "Tip: Check the leaderboard to see how your best run compares globally.",
        "Tip: The Gunslinger's ultimate doubles fire rate and ammo capacity for 8 seconds.",
        "Tip: Falling rocks in the cave world deal damage — watch your positioning.",
        "Tip: Achievements grant permanent stat boosts — unlock them all!",
        "Tip: Daily Challenge mode offers a unique seed every 24 hours.",
        "Tip: Draft Mode means you can only pick from a random pool of cards — choose wisely.",
        "Tip: Hardcore Mode disables healing and card rerolls. Good luck!",
    };

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

        // Start hidden
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fades in the loading screen, loads <paramref name="sceneName"/> asynchronously
    /// while updating the progress bar, then fades out.
    /// </summary>
    /// <param name="sceneName">Name of the Unity scene to load.</param>
    public IEnumerator LoadSceneAsync(string sceneName)
    {
        gameObject.SetActive(true);

        // Show a random tip
        if (tipText != null)
            tipText.text = _tips[Random.Range(0, _tips.Length)];

        // Reset bar
        if (loadingBar != null) loadingBar.value = 0f;

        // Fade in
        yield return StartCoroutine(Fade(0f, 1f));

        // Begin async load
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[LoadingScreenUI] Scene '{sceneName}' not found.");
            yield return StartCoroutine(Fade(1f, 0f));
            gameObject.SetActive(false);
            yield break;
        }

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            // Progress goes 0 → 0.9 during load, then jumps to 1 on activation
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (loadingBar != null) loadingBar.value = progress;

            if (op.progress >= 0.9f)
            {
                if (loadingBar != null) loadingBar.value = 1f;
                // Small pause so the player sees the completed bar
                yield return new WaitForSecondsRealtime(0.3f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f));
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
