using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that manages full-screen post-processing effects implemented
/// purely in UI space (no post-processing stack required):
/// <list type="bullet">
///   <item>Screen flash – overlay Image alpha spike then fade.</item>
///   <item>Vignette – darkens screen edges via a radial gradient Image.</item>
///   <item>Chromatic aberration – sprite-based fake CA on screen corners.</item>
///   <item>Slow motion – <c>Time.timeScale</c> manipulation with smooth recovery.</item>
///   <item>Death screen effect – red vignette + slow motion + greyscale tint.</item>
/// </list>
///
/// Attach to: A Canvas/UI overlay GameObject.  Assign the Image references in the Inspector.
/// </summary>
public class ScreenEffectsManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The scene-global <see cref="ScreenEffectsManager"/> instance.</summary>
    public static ScreenEffectsManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Flash Overlay")]
    [Tooltip("Full-screen Image used for the flash effect.  Must cover the entire canvas.")]
    [SerializeField] private Image flashOverlay;

    [Header("Vignette")]
    [Tooltip("Radial-gradient Image stretched to fill the canvas for vignette darkening.")]
    [SerializeField] private Image vignetteOverlay;

    [Header("Chromatic Aberration")]
    [Tooltip("Image placed on the screen corners to simulate chromatic aberration fringing.")]
    [SerializeField] private Image caOverlay;

    [Header("Death / Greyscale")]
    [Tooltip("Full-screen greyscale tint Image (dark grey, low alpha) shown on death.")]
    [SerializeField] private Image greyscaleOverlay;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Coroutine _flashCoroutine;
    private Coroutine _vignetteCoroutine;
    private Coroutine _caCoroutine;
    private Coroutine _slowMoCoroutine;

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

        // Ensure overlays start invisible
        SetAlpha(flashOverlay,     0f);
        SetAlpha(vignetteOverlay,  0f);
        SetAlpha(caOverlay,        0f);
        SetAlpha(greyscaleOverlay, 0f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately sets <paramref name="flashOverlay"/> to <paramref name="color"/>
    /// at full alpha, then fades it back to transparent over <paramref name="duration"/> seconds.
    /// </summary>
    /// <param name="color">Flash tint colour.</param>
    /// <param name="duration">Time in seconds to fade the flash back to invisible.</param>
    public void FlashScreen(Color color, float duration)
    {
        if (flashOverlay == null) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    /// <summary>
    /// Fades the vignette overlay in to <paramref name="intensity"/> alpha, holds it,
    /// then fades it back out over <paramref name="duration"/> seconds total.
    /// </summary>
    /// <param name="intensity">Target vignette alpha [0, 1].</param>
    /// <param name="duration">Total duration in seconds.</param>
    public void VignetteEffect(float intensity, float duration)
    {
        if (vignetteOverlay == null) return;
        if (_vignetteCoroutine != null) StopCoroutine(_vignetteCoroutine);
        _vignetteCoroutine = StartCoroutine(FadeOverlayCoroutine(vignetteOverlay, intensity, duration));
    }

    /// <summary>
    /// Flashes the chromatic-aberration corner overlay at <paramref name="intensity"/>
    /// alpha, then fades it back out over <paramref name="duration"/> seconds.
    /// </summary>
    /// <param name="intensity">Target CA alpha [0, 1].</param>
    /// <param name="duration">Total duration in seconds.</param>
    public void ChromaticAberration(float intensity, float duration)
    {
        if (caOverlay == null) return;
        if (_caCoroutine != null) StopCoroutine(_caCoroutine);
        _caCoroutine = StartCoroutine(FadeOverlayCoroutine(caOverlay, intensity, duration));
    }

    /// <summary>
    /// Sets <c>Time.timeScale</c> to <paramref name="timeScale"/>, then smoothly
    /// interpolates it back to 1 over <paramref name="duration"/> seconds (real time).
    /// </summary>
    /// <param name="timeScale">Target time scale (e.g. 0.3 for slow motion).</param>
    /// <param name="duration">Real-time seconds before returning to normal speed.</param>
    public void SlowMotion(float timeScale, float duration)
    {
        if (_slowMoCoroutine != null) StopCoroutine(_slowMoCoroutine);
        _slowMoCoroutine = StartCoroutine(SlowMotionCoroutine(timeScale, duration));
    }

    /// <summary>
    /// Plays the death screen effect: red vignette, slow motion to 0.3×,
    /// and a greyscale tint that persists until the scene changes.
    /// </summary>
    public void DeathScreenEffect()
    {
        VignetteEffect(0.8f, 1.5f);
        SlowMotion(0.3f, 2f);

        if (greyscaleOverlay != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FadeOverlayCoroutine(greyscaleOverlay, 0.6f, 1f));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Immediately sets the flash colour at full alpha, then fades it out.</summary>
    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        color.a = 1f;
        flashOverlay.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            flashOverlay.color = color;
            yield return null;
        }

        SetAlpha(flashOverlay, 0f);
        _flashCoroutine = null;
    }

    /// <summary>Fades an overlay Image in to <paramref name="targetAlpha"/>, then back out.</summary>
    private IEnumerator FadeOverlayCoroutine(Image overlay, float targetAlpha, float duration)
    {
        float half = duration * 0.5f;

        // Fade in
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(overlay, Mathf.Lerp(0f, targetAlpha, elapsed / half));
            yield return null;
        }
        SetAlpha(overlay, targetAlpha);

        // Fade out
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(overlay, Mathf.Lerp(targetAlpha, 0f, elapsed / half));
            yield return null;
        }
        SetAlpha(overlay, 0f);
    }

    /// <summary>Sets <c>Time.timeScale</c> and smoothly returns it to 1 using real time.</summary>
    private IEnumerator SlowMotionCoroutine(float targetScale, float duration)
    {
        Time.timeScale = targetScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            Time.timeScale = Mathf.Lerp(targetScale, 1f, t);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        _slowMoCoroutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Sets the alpha of an Image without changing its RGB channels.</summary>
    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}
