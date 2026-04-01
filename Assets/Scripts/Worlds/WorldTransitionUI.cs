using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay used by <see cref="WorldManager"/> to hide world transitions.
/// Attach to a Canvas child that contains a full-screen black <see cref="Image"/> and
/// an optional <see cref="TextMeshProUGUI"/> label.
///
/// Typical setup:
///   Canvas (Screen Space – Overlay, Sort Order 100)
///   └── WorldTransitionUI (this component)
///       ├── Image (black, stretch to fill, raycast off)
///       └── TextMeshProUGUI (centre-screen, "ENTERING: …")
/// </summary>
public class WorldTransitionUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Overlay")]
    [Tooltip("Full-screen black Image component used for the fade.")]
    [SerializeField] private Image _overlayImage;

    [Header("Text")]
    [Tooltip("TextMeshPro label that displays \"ENTERING: <World Name>\" during the transition.")]
    [SerializeField] private TextMeshProUGUI _worldNameText;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the world label to <c>"ENTERING: <paramref name="worldName"/>"</c> and
    /// shows or hides it.
    /// </summary>
    /// <param name="worldName">Name of the world being entered.</param>
    /// <param name="visible">Whether the label should be visible.</param>
    public void SetWorldName(string worldName, bool visible = true)
    {
        if (_worldNameText == null) return;
        _worldNameText.text = $"ENTERING: {worldName}";
        _worldNameText.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Fades the overlay from fully transparent to fully opaque over
    /// <paramref name="duration"/> seconds (alpha 0 → 1).
    /// </summary>
    /// <param name="duration">Duration of the fade in seconds.</param>
    /// <returns>Coroutine — yield this from a parent coroutine.</returns>
    public Coroutine FadeIn(float duration) =>
        StartCoroutine(FadeRoutine(0f, 1f, duration));

    /// <summary>
    /// Fades the overlay from fully opaque to fully transparent over
    /// <paramref name="duration"/> seconds (alpha 1 → 0).
    /// </summary>
    /// <param name="duration">Duration of the fade in seconds.</param>
    /// <returns>Coroutine — yield this from a parent coroutine.</returns>
    public Coroutine FadeOut(float duration) =>
        StartCoroutine(FadeRoutine(1f, 0f, duration));

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Start with the overlay fully transparent so it does not block the screen
        if (_overlayImage != null)
            SetAlpha(0f);

        if (_worldNameText != null)
            _worldNameText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (_overlayImage == null) yield break;

        float elapsed = 0f;
        SetAlpha(from);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        if (_overlayImage == null) return;
        Color c = _overlayImage.color;
        c.a = alpha;
        _overlayImage.color = c;
    }
}
