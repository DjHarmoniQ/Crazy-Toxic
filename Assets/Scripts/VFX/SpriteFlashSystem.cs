using System.Collections;
using UnityEngine;

/// <summary>
/// Component that temporarily overrides a <see cref="SpriteRenderer"/>'s material colour
/// to produce a hit-flash effect, then smoothly lerps back to the original colour.
///
/// Attach to: Enemy and Player GameObjects that have a <see cref="SpriteRenderer"/>.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlashSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Flash Settings")]
    [Tooltip("Speed at which the flash colour lerps back to the original sprite colour after the peak.")]
    [SerializeField] private float _fadeSpeed = 8f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private Coroutine _flashCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColor  = _spriteRenderer.color;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately sets the sprite's colour to <paramref name="color"/>, then
    /// smoothly lerps it back to the original colour over <paramref name="duration"/> seconds.
    /// If a flash is already in progress it is cancelled and replaced.
    /// </summary>
    /// <param name="color">The flash colour to apply at full intensity.</param>
    /// <param name="duration">Duration in seconds before the colour returns to normal.</param>
    public void Flash(Color color, float duration)
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }

    /// <summary>
    /// Shortcut for a white flash – typically used for hit confirmation on enemies.
    /// </summary>
    /// <param name="duration">Duration in seconds.</param>
    public void FlashWhite(float duration)
    {
        Flash(Color.white, duration);
    }

    /// <summary>
    /// Shortcut for a red flash – typically used to indicate damage taken.
    /// </summary>
    /// <param name="duration">Duration in seconds.</param>
    public void FlashRed(float duration)
    {
        Flash(Color.red, duration);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the sprite colour immediately to <paramref name="flashColor"/>, then
    /// lerps back to <see cref="_originalColor"/> over <paramref name="duration"/> seconds.
    /// </summary>
    private IEnumerator FlashCoroutine(Color flashColor, float duration)
    {
        _spriteRenderer.color = flashColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _spriteRenderer.color = Color.Lerp(flashColor, _originalColor, elapsed / duration);
            yield return null;
        }

        _spriteRenderer.color = _originalColor;
        _flashCoroutine = null;
    }
}
