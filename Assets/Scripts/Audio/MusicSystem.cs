using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton that manages the game's dynamic layered music.
/// Four <see cref="AudioSource"/> layers (ambient, rhythm, melody, intensity) are
/// cross-faded based on a 0–1 intensity value that is driven by the number of
/// enemies on screen, boss phase, and the player's combo multiplier.
///
/// Attach to: A dedicated "Audio" manager GameObject in the scene.
/// </summary>
public class MusicSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The scene-global <see cref="MusicSystem"/> instance.</summary>
    public static MusicSystem Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Music Layers (0=Ambient, 1=Rhythm, 2=Melody, 3=Intensity)")]
    [Tooltip("Four AudioSource components – one per music layer. Order: ambient, rhythm, melody, intensity.")]
    [SerializeField] private AudioSource[] musicLayers = new AudioSource[4];

    [Header("Intensity Settings")]
    [Tooltip("Maximum number of simultaneous enemies considered for a 1.0 intensity reading.")]
    [SerializeField] private int maxEnemiesForFullIntensity = 20;
    [Tooltip("Maximum combo count considered for a 0.3 intensity contribution.")]
    [SerializeField] private int maxComboForIntensityBonus = 50;
    [Tooltip("Speed at which intensity lerps toward its target value per second.")]
    [SerializeField] private float intensityLerpSpeed = 1.5f;

    [Header("Fade Settings")]
    [Tooltip("Default duration in seconds for track cross-fade transitions.")]
    [SerializeField] private float defaultFadeDuration = 2f;

    [Header("Stinger")]
    [Tooltip("AudioSource used exclusively for one-shot stinger clips (boss spawns, level-up).")]
    [SerializeField] private AudioSource stingerSource;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _currentIntensity;
    private float _targetIntensity;
    private Coroutine _transitionCoroutine;

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
        // Ensure all layers are playing (initially at volume 0 except ambient)
        foreach (var source in musicLayers)
        {
            if (source != null && !source.isPlaying)
                source.Play();
        }
        ApplyLayerVolumes(0f);
    }

    private void Update()
    {
        _targetIntensity = CalculateTargetIntensity();
        _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity,
                                               intensityLerpSpeed * Time.deltaTime);
        ApplyLayerVolumes(_currentIntensity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Manually sets the music intensity (0–1). Bypasses the automatic enemy/combo
    /// calculation until the next <c>Update</c> tick recalculates.
    /// </summary>
    /// <param name="value">Intensity in the range [0, 1].</param>
    public void SetIntensity(float value)
    {
        _currentIntensity = Mathf.Clamp01(value);
        ApplyLayerVolumes(_currentIntensity);
    }

    /// <summary>
    /// Cross-fades the current music layers out and the new layers in over
    /// <paramref name="fadeDuration"/> seconds. Used by <c>WorldManager</c> when
    /// the player transitions to a new world.
    /// </summary>
    /// <param name="newLayers">Array of <see cref="AudioClip"/> objects (one per layer).</param>
    /// <param name="fadeDuration">Crossfade duration in seconds.</param>
    public void TransitionToTrack(AudioClip[] newLayers, float fadeDuration)
    {
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(CrossFadeCoroutine(newLayers, fadeDuration));
    }

    /// <summary>
    /// Plays a one-shot stinger clip over the current music (e.g. boss spawn, level-up fanfare).
    /// </summary>
    /// <param name="stinger">The <see cref="AudioClip"/> to play as an overlay.</param>
    public void PlayStinger(AudioClip stinger)
    {
        if (stinger == null) return;

        if (stingerSource != null)
        {
            stingerSource.PlayOneShot(stinger);
        }
        else
        {
            // Fallback: use the ambient layer's AudioSource for a one-shot
            if (musicLayers != null && musicLayers.Length > 0 && musicLayers[0] != null)
                musicLayers[0].PlayOneShot(stinger);
        }
    }

    /// <summary>
    /// Updates the master music volume applied to all layers.
    /// Called by <see cref="SettingsMenu"/> when the music slider changes.
    /// </summary>
    /// <param name="volume">New master music volume [0, 1].</param>
    public void SetMusicVolume(float volume)
    {
        foreach (var source in musicLayers)
        {
            if (source != null)
                source.volume = Mathf.Clamp01(volume) * GetLayerTargetVolume(System.Array.IndexOf(musicLayers, source), _currentIntensity);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the desired intensity in [0,1] based on live game state.
    /// Queries <see cref="WaveManager"/> enemy count and <see cref="ComboSystem"/> combo.
    /// </summary>
    private float CalculateTargetIntensity()
    {
        float enemyFactor = 0f;
        if (WaveManager.Instance != null && maxEnemiesForFullIntensity > 0)
            enemyFactor = Mathf.Clamp01((float)WaveManager.Instance.EnemiesAlive / maxEnemiesForFullIntensity);

        float comboFactor = 0f;
        // ComboSystem is a component – search if not cached
        var comboSystem = FindObjectOfType<ComboSystem>();
        if (comboSystem != null && maxComboForIntensityBonus > 0)
            comboFactor = Mathf.Clamp01((float)comboSystem.CurrentCombo / maxComboForIntensityBonus) * 0.3f;

        return Mathf.Clamp01(enemyFactor * 0.7f + comboFactor);
    }

    /// <summary>
    /// Applies layer volumes according to the current intensity value using the
    /// four-tier breakpoints defined in the specification.
    /// <list type="bullet">
    ///   <item>0.0–0.3: ambient only</item>
    ///   <item>0.3–0.6: ambient + rhythm</item>
    ///   <item>0.6–0.8: ambient + rhythm + melody</item>
    ///   <item>0.8–1.0: all four layers at full intensity</item>
    /// </list>
    /// </summary>
    private void ApplyLayerVolumes(float intensity)
    {
        for (int i = 0; i < musicLayers.Length; i++)
        {
            if (musicLayers[i] == null) continue;
            musicLayers[i].volume = GetLayerTargetVolume(i, intensity);
        }
    }

    /// <summary>Returns the desired volume for layer <paramref name="layerIndex"/> at the given intensity.</summary>
    private static float GetLayerTargetVolume(int layerIndex, float intensity)
    {
        switch (layerIndex)
        {
            case 0: // ambient – always on
                return 1f;
            case 1: // rhythm – fades in at 0.3
                return Mathf.Clamp01((intensity - 0.3f) / 0.3f);
            case 2: // melody – fades in at 0.6
                return Mathf.Clamp01((intensity - 0.6f) / 0.2f);
            case 3: // intensity – fades in at 0.8
                return Mathf.Clamp01((intensity - 0.8f) / 0.2f);
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Coroutine that fades out all current layers, swaps their clips, then fades back in.
    /// </summary>
    private IEnumerator CrossFadeCoroutine(AudioClip[] newLayers, float fadeDuration)
    {
        float half = fadeDuration * 0.5f;

        // Fade out
        float elapsed = 0f;
        float[] startVolumes = new float[musicLayers.Length];
        for (int i = 0; i < musicLayers.Length; i++)
            startVolumes[i] = musicLayers[i] != null ? musicLayers[i].volume : 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            for (int i = 0; i < musicLayers.Length; i++)
            {
                if (musicLayers[i] != null)
                    musicLayers[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
            }
            yield return null;
        }

        // Swap clips
        if (newLayers != null)
        {
            for (int i = 0; i < musicLayers.Length && i < newLayers.Length; i++)
            {
                if (musicLayers[i] == null || newLayers[i] == null) continue;
                musicLayers[i].clip = newLayers[i];
                musicLayers[i].Play();
            }
        }

        // Fade back in based on current intensity
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            for (int i = 0; i < musicLayers.Length; i++)
            {
                if (musicLayers[i] == null) continue;
                float target = GetLayerTargetVolume(i, _currentIntensity);
                musicLayers[i].volume = Mathf.Lerp(0f, target, t);
            }
            yield return null;
        }

        _transitionCoroutine = null;
    }
}
