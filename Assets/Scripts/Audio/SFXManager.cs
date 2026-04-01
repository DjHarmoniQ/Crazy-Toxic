using UnityEngine;

/// <summary>
/// Singleton SFX manager that maintains a round-robin pool of
/// <see cref="AudioSource"/> components, enabling concurrent sound playback
/// without allocating at runtime.
///
/// Volume categories (master, sfx, music, ui) are all serialized and can be
/// updated live from <see cref="SettingsMenu"/>.
///
/// Attach to: A dedicated "Audio" manager GameObject in the scene.
/// </summary>
public class SFXManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The scene-global <see cref="SFXManager"/> instance.</summary>
    public static SFXManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Pool Settings")]
    [Tooltip("Number of pooled AudioSource components pre-created at startup.")]
    [SerializeField] private int poolSize = 20;

    [Header("Volume Multipliers")]
    [Tooltip("Overall master volume applied on top of every category.")]
    [SerializeField] private float masterVolume = 1f;

    [Tooltip("Volume multiplier for all in-world SFX.")]
    [SerializeField] private float sfxVolume = 1f;

    [Tooltip("Volume multiplier applied to the MusicSystem layers.")]
    [SerializeField] private float musicVolume = 0.7f;

    [Tooltip("Volume multiplier for 2-D UI sounds.")]
    [SerializeField] private float uiVolume = 0.8f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private AudioSource[] _pool;
    private int _poolIndex;

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

        // Build pool: create child GameObjects each with one AudioSource
        _pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var child = new GameObject($"SFXSource_{i}");
            child.transform.SetParent(transform);
            _pool[i] = child.AddComponent<AudioSource>();
            _pool[i].playOnAwake = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Plays a sound effect at a given world position with optional volume and pitch overrides.
    /// Uses the 3-D AudioSource spatial blend so the sound attenuates with distance.
    /// </summary>
    /// <param name="clip">The <see cref="AudioClip"/> to play.</param>
    /// <param name="position">World-space position of the sound emitter.</param>
    /// <param name="volume">Volume scale (0–1) before master / sfx multipliers.</param>
    /// <param name="pitch">Playback pitch (1 = normal speed).</param>
    public void PlaySFX(AudioClip clip, Vector2 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        AudioSource source = NextSource();
        source.transform.position = position;
        source.spatialBlend = 1f;
        source.pitch = pitch;
        source.volume = volume * sfxVolume * masterVolume;
        source.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays a sound effect at the world origin with a random pitch in
    /// [<paramref name="pitchMin"/>, <paramref name="pitchMax"/>] to reduce audio repetitiveness.
    /// </summary>
    /// <param name="clip">The <see cref="AudioClip"/> to play.</param>
    /// <param name="pitchMin">Minimum pitch value (default 0.9).</param>
    /// <param name="pitchMax">Maximum pitch value (default 1.1).</param>
    public void PlaySFXWithVariation(AudioClip clip, float pitchMin = 0.9f, float pitchMax = 1.1f)
    {
        if (clip == null) return;
        float randomPitch = Random.Range(pitchMin, pitchMax);
        AudioSource source = NextSource();
        source.transform.position = Vector3.zero;
        source.spatialBlend = 1f;
        source.pitch = randomPitch;
        source.volume = sfxVolume * masterVolume;
        source.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays a 2-D (non-spatial) UI sound, unaffected by listener position.
    /// Uses the <see cref="uiVolume"/> multiplier.
    /// </summary>
    /// <param name="clip">The <see cref="AudioClip"/> to play.</param>
    public void PlayUISound(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource source = NextSource();
        source.spatialBlend = 0f; // 2-D
        source.pitch = 1f;
        source.volume = uiVolume * masterVolume;
        source.PlayOneShot(clip);
    }

    /// <summary>
    /// Updates the master volume and propagates the change to <see cref="MusicSystem"/>.
    /// Called by <see cref="SettingsMenu"/> in real-time.
    /// </summary>
    /// <param name="value">New master volume [0, 1].</param>
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        if (MusicSystem.Instance != null)
            MusicSystem.Instance.SetMusicVolume(musicVolume * masterVolume);
    }

    /// <summary>
    /// Updates the SFX category volume multiplier.
    /// Called by <see cref="SettingsMenu"/> in real-time.
    /// </summary>
    /// <param name="value">New SFX volume [0, 1].</param>
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Updates the music category volume multiplier and propagates it to <see cref="MusicSystem"/>.
    /// Called by <see cref="SettingsMenu"/> in real-time.
    /// </summary>
    /// <param name="value">New music volume [0, 1].</param>
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        if (MusicSystem.Instance != null)
            MusicSystem.Instance.SetMusicVolume(musicVolume * masterVolume);
    }

    /// <summary>
    /// Updates the UI sound category volume multiplier.
    /// Called by <see cref="SettingsMenu"/> in real-time.
    /// </summary>
    /// <param name="value">New UI volume [0, 1].</param>
    public void SetUIVolume(float value)
    {
        uiVolume = Mathf.Clamp01(value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the next available <see cref="AudioSource"/> in the pool (round-robin).</summary>
    private AudioSource NextSource()
    {
        AudioSource source = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _pool.Length;
        return source;
    }
}
