using UnityEngine;

/// <summary>
/// Stores per-variation overrides for a <see cref="WorldData"/> world.
/// One of the three variations is randomly selected by <see cref="WorldManager"/> at
/// the start of each run, giving each world a fresh look every play-through.
/// </summary>
[System.Serializable]
public class WorldVariation
{
    [Header("Variation Identity")]
    [Tooltip("Friendly label shown in the Inspector, e.g. \"Rainy\", \"Sunset\", \"Stormy\".")]
    public string variationName;

    [Header("Visuals")]
    [Tooltip("Sprite that replaces the world's default background for this variation.")]
    public Sprite backgroundOverride;

    [Tooltip("Ambient-light colour override applied while this variation is active.")]
    public Color lightColorOverride = Color.white;

    [Tooltip("Ambient-light intensity override applied while this variation is active.")]
    public float lightIntensityOverride = 1f;

    [Header("Weather")]
    [Tooltip("Optional particle system that plays during this variation (rain, embers, snow, etc.). " +
             "Assign the prefab or a scene instance; WorldManager will Play/Stop it on transition.")]
    public ParticleSystem weatherEffect;
}
