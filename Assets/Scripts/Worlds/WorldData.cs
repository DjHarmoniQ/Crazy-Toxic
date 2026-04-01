using UnityEngine;

/// <summary>
/// ScriptableObject that holds all data describing a single themed world:
/// visuals, audio, fog settings, the wave at which it activates, its
/// environmental hazards, and three layout/theme variations.
///
/// Create via: Assets → Create → Crazy-Toxic → WorldData
/// </summary>
[CreateAssetMenu(menuName = "Crazy-Toxic/WorldData", fileName = "NewWorldData")]
public class WorldData : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Identity")]
    [Tooltip("Display name of the world, e.g. \"Bone Crypt\".")]
    public string worldName;

    [Tooltip("Short flavour description shown in the transition screen.")]
    public string worldDescription;

    // ─────────────────────────────────────────────────────────────────────────
    //  Visuals
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Visuals")]
    [Tooltip("Default background sprite used when no variation overrides it.")]
    public Sprite backgroundSprite;

    [Tooltip("Ambient light colour applied when entering this world.")]
    public Color ambientLightColor = Color.white;

    [Tooltip("Ambient light intensity applied when entering this world.")]
    public float ambientLightIntensity = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Atmosphere
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Atmosphere")]
    [Tooltip("Fog tint colour used while this world is active.")]
    public Color fogColor = Color.grey;

    [Tooltip("Exponential fog density; higher values create denser fog.")]
    public float fogDensity = 0.01f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Audio
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Audio")]
    [Tooltip("Background music track played throughout this world.")]
    public AudioClip backgroundMusic;

    // ─────────────────────────────────────────────────────────────────────────
    //  Wave Range
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Wave Range")]
    [Tooltip("Wave number at which this world becomes active. " +
             "WorldManager activates the world whose startWave is the highest value " +
             "that is still ≤ the current wave.")]
    public int startWave = 1;

    // ─────────────────────────────────────────────────────────────────────────
    //  Hazards & Variations
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Environmental Hazards")]
    [Tooltip("All hazard GameObjects/prefabs that become active in this world. " +
             "WorldManager enables them on entry and disables them on exit.")]
    public EnvironmentalHazardBase[] hazards;

    [Header("Variations")]
    [Tooltip("Exactly three layout/theme variants. WorldManager picks one randomly each run.")]
    public WorldVariation[] variations = new WorldVariation[3];
}
