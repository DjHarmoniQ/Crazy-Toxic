using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton that tracks which of the 5 themed worlds is currently active and
/// drives the wave-triggered transition pipeline:
/// fade out → swap background / lights / fog / music → activate hazards → fade in.
///
/// Setup:
///   1. Attach to a persistent manager GameObject.
///   2. Assign the 5 <see cref="WorldData"/> assets in the <c>_worlds</c> array (Inspector).
///   3. Assign the scene <see cref="WorldTransitionUI"/> reference.
///   4. Assign the <see cref="AudioSource"/> used for background music.
///   5. Assign the SpriteRenderer used as the scrolling background.
/// </summary>
public class WorldManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single WorldManager instance.</summary>
    public static WorldManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("World Data")]
    [Tooltip("All 5 WorldData assets sorted in the order they should activate " +
             "(ascending startWave). Assign in the Inspector.")]
    [SerializeField] private WorldData[] _worlds;

    [Header("Scene References")]
    [Tooltip("WorldTransitionUI component that handles the full-screen fade overlay.")]
    [SerializeField] private WorldTransitionUI _transitionUI;

    [Tooltip("AudioSource used for background music. Should be set to loop.")]
    [SerializeField] private AudioSource _musicSource;

    [Tooltip("SpriteRenderer used as the world background.")]
    [SerializeField] private SpriteRenderer _backgroundRenderer;

    [Header("Transition")]
    [Tooltip("Duration in seconds for the fade-to-black and fade-from-black animations.")]
    [SerializeField] private float _fadeDuration = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The world that is currently active.</summary>
    public WorldData CurrentWorld { get; private set; }

    /// <summary>The variation that was randomly chosen for the current run.</summary>
    public WorldVariation ActiveVariation { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _isTransitioning;

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
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged += OnWaveChanged;

        // Activate the first world immediately (wave 1)
        if (_worlds != null && _worlds.Length > 0)
            ApplyWorldImmediate(_worlds[0]);
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= OnWaveChanged;

        if (Instance == this)
            Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the scene's ambient light to the given <paramref name="color"/> and
    /// <paramref name="intensity"/>.
    /// </summary>
    /// <param name="color">Ambient light colour.</param>
    /// <param name="intensity">Ambient light intensity (0–8 typical range).</param>
    public void SetAmbientLight(Color color, float intensity)
    {
        RenderSettings.ambientLight = color * intensity;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private – Wave Listener
    // ─────────────────────────────────────────────────────────────────────────

    private void OnWaveChanged(int wave)
    {
        if (_worlds == null) return;

        // Find the highest-priority world whose startWave ≤ current wave
        WorldData target = null;
        foreach (WorldData w in _worlds)
        {
            if (w == null) continue;
            if (wave >= w.startWave)
            {
                if (target == null || w.startWave > target.startWave)
                    target = w;
            }
        }

        if (target != null && target != CurrentWorld && !_isTransitioning)
            StartCoroutine(TransitionToWorld(target));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private – Transition Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator TransitionToWorld(WorldData world)
    {
        _isTransitioning = true;
        Debug.Log($"[WorldManager] Transitioning to world: {world.worldName}");

        // 1. Deactivate hazards from the old world
        DeactivateCurrentHazards();

        // 2. Show world name and fade to black
        if (_transitionUI != null)
        {
            _transitionUI.SetWorldName(world.worldName);
            yield return _transitionUI.FadeIn(_fadeDuration);
        }

        // 3. Pick a random variation
        ActiveVariation = PickVariation(world);

        // 4. Swap visuals
        ApplyVisuals(world, ActiveVariation);

        // 5. Apply fog
        RenderSettings.fog = world.fogDensity > 0f;
        RenderSettings.fogColor = world.fogColor;
        RenderSettings.fogDensity = world.fogDensity;

        // 6. Swap music
        if (_musicSource != null && world.backgroundMusic != null)
        {
            _musicSource.Stop();
            _musicSource.clip = world.backgroundMusic;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        CurrentWorld = world;

        // 7. Fade back in
        if (_transitionUI != null)
        {
            yield return _transitionUI.FadeOut(_fadeDuration);
            _transitionUI.SetWorldName(world.worldName, visible: false);
        }

        // 8. Activate new hazards
        ActivateCurrentHazards();

        _isTransitioning = false;
        Debug.Log($"[WorldManager] Now in world: {world.worldName}" +
                  $" (variation: {ActiveVariation?.variationName ?? "none"})");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private – Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the first world without any fade (used at game start).
    /// </summary>
    private void ApplyWorldImmediate(WorldData world)
    {
        ActiveVariation = PickVariation(world);
        ApplyVisuals(world, ActiveVariation);

        RenderSettings.fog = world.fogDensity > 0f;
        RenderSettings.fogColor = world.fogColor;
        RenderSettings.fogDensity = world.fogDensity;

        if (_musicSource != null && world.backgroundMusic != null)
        {
            _musicSource.clip = world.backgroundMusic;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        CurrentWorld = world;
        ActivateCurrentHazards();
    }

    private void ApplyVisuals(WorldData world, WorldVariation variation)
    {
        // Stop previous variation's weather effect before starting the new one
        if (ActiveVariation?.weatherEffect != null)
            ActiveVariation.weatherEffect.Stop();

        // Background sprite
        Sprite bg = (variation != null && variation.backgroundOverride != null)
            ? variation.backgroundOverride
            : world.backgroundSprite;

        if (_backgroundRenderer != null && bg != null)
            _backgroundRenderer.sprite = bg;

        // Ambient light
        Color lightColor = (variation != null) ? variation.lightColorOverride : world.ambientLightColor;
        float lightIntensity = (variation != null) ? variation.lightIntensityOverride : world.ambientLightIntensity;
        SetAmbientLight(lightColor, lightIntensity);

        // Weather particle effect
        if (variation?.weatherEffect != null)
            variation.weatherEffect.Play();
    }

    private WorldVariation PickVariation(WorldData world)
    {
        if (world.variations == null || world.variations.Length == 0)
            return null;
        return world.variations[Random.Range(0, world.variations.Length)];
    }

    private void ActivateCurrentHazards()
    {
        if (CurrentWorld?.hazards == null) return;
        foreach (EnvironmentalHazardBase hazard in CurrentWorld.hazards)
        {
            if (hazard != null)
                hazard.gameObject.SetActive(true);
        }
    }

    private void DeactivateCurrentHazards()
    {
        if (CurrentWorld?.hazards == null) return;
        foreach (EnvironmentalHazardBase hazard in CurrentWorld.hazards)
        {
            if (hazard != null)
                hazard.gameObject.SetActive(false);
        }
    }
}
