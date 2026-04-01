using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Continuously scrolls a set of <see cref="ParallaxLayer"/> images from right to left,
/// wrapping each layer by keeping two copies side-by-side so the seam is invisible.
///
/// Attach to: The root GameObject of the main-menu background canvas panel.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Nested Types
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pairs an <see cref="Image"/> with the speed at which it scrolls.
    /// The image should be wide enough that, together with its copy, it covers
    /// the full screen width without gaps.
    /// </summary>
    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("The UI Image used as the background layer.")]
        public Image image;

        [Tooltip("Scroll speed in pixels per second (positive = scroll left).")]
        public float scrollSpeed = 50f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Layers")]
    [Tooltip("The three parallax layers, ordered back-to-front (index 0 = farthest back).")]
    [SerializeField] private ParallaxLayer[] layers;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    // Second copies of each layer image (placed immediately to the right)
    private RectTransform[] _primaryRects;
    private RectTransform[] _copyRects;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (layers == null || layers.Length == 0) return;

        _primaryRects = new RectTransform[layers.Length];
        _copyRects    = new RectTransform[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            ParallaxLayer layer = layers[i];
            if (layer?.image == null) continue;

            RectTransform primary = layer.image.rectTransform;
            _primaryRects[i] = primary;

            // Duplicate the image and place it directly to the right of the original
            Image copy = Instantiate(layer.image, primary.parent);
            copy.name  = layer.image.name + "_Copy";
            RectTransform copyRect = copy.rectTransform;
            copyRect.anchorMin = primary.anchorMin;
            copyRect.anchorMax = primary.anchorMax;
            copyRect.pivot     = primary.pivot;
            copyRect.sizeDelta = primary.sizeDelta;
            copyRect.anchoredPosition = primary.anchoredPosition + new Vector2(primary.sizeDelta.x, 0f);
            _copyRects[i] = copyRect;
        }
    }

    private void Update()
    {
        if (layers == null) return;

        for (int i = 0; i < layers.Length; i++)
        {
            if (_primaryRects == null || i >= _primaryRects.Length) break;
            if (_primaryRects[i] == null || _copyRects[i] == null) continue;

            float delta = layers[i].scrollSpeed * Time.deltaTime;
            _primaryRects[i].anchoredPosition -= new Vector2(delta, 0f);
            _copyRects[i].anchoredPosition    -= new Vector2(delta, 0f);

            // When the primary image has scrolled fully off screen to the left,
            // wrap it around to the right of the copy
            float imageWidth = _primaryRects[i].sizeDelta.x;
            if (_primaryRects[i].anchoredPosition.x <= -imageWidth)
            {
                _primaryRects[i].anchoredPosition += new Vector2(imageWidth * 2f, 0f);
            }
            if (_copyRects[i].anchoredPosition.x <= -imageWidth)
            {
                _copyRects[i].anchoredPosition += new Vector2(imageWidth * 2f, 0f);
            }
        }
    }
}
