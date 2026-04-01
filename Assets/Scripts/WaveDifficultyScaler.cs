using UnityEngine;

/// <summary>
/// Calculates enemy stat multipliers, spawn rates, and wave-type flags for any
/// given wave number according to the following scaling curve:
/// <list type="bullet">
///   <item>Waves  1–5   : Base / tutorial feel</item>
///   <item>Waves  6–15  : Linear ramp (+5 % HP/damage per wave)</item>
///   <item>Waves 16–30  : Accelerated ramp (+8 % per wave; elites from wave 20)</item>
///   <item>Waves 31–50  : Exponential curve (+12 % per wave; mini-boss every 10 waves)</item>
///   <item>Waves 51–100 : Hardcore (+15 % per wave; double spawn rate)</item>
///   <item>Wave  100+   : Prestige (multiplier capped at 20×; prestige modifier active)</item>
/// </list>
///
/// All multiplier curves are exposed as <see cref="AnimationCurve"/> fields so
/// designers can override the maths in the Inspector without touching code.
///
/// Singleton: access via <see cref="Instance"/>.
/// Attach to: A persistent manager GameObject in the game scene.
/// </summary>
public class WaveDifficultyScaler : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="WaveDifficultyScaler"/> instance.</summary>
    public static WaveDifficultyScaler Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Nested Types
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Snapshot of all difficulty parameters for a single wave.
    /// Returned by <see cref="GetWaveParams"/> and stored on <see cref="WaveManager"/>.
    /// </summary>
    public struct WaveParams
    {
        /// <summary>Multiplier applied to every enemy's maximum HP.</summary>
        public float HealthMultiplier;
        /// <summary>Multiplier applied to every enemy's damage output.</summary>
        public float DamageMultiplier;
        /// <summary>Multiplier that reduces enemy spawn interval (higher = faster spawns).</summary>
        public float SpawnRateMultiplier;
        /// <summary>Total number of enemies that should spawn in this wave.</summary>
        public int EnemiesPerWave;
        /// <summary>Whether elite enemies are allowed to spawn this wave.</summary>
        public bool IsElite;
        /// <summary>Whether a mini-boss should spawn this wave.</summary>
        public bool IsMiniBoss;
        /// <summary>Whether prestige (endless) mode is active.</summary>
        public bool IsPrestige;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Scaling Curves
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Health Multiplier Curve")]
    [Tooltip("X-axis = wave number (0–100+), Y-axis = health multiplier. " +
             "Overrides the built-in formula when evaluated; leave flat at 0 to use the formula.")]
    [SerializeField] private AnimationCurve healthMultiplierCurve = AnimationCurve.Linear(1, 1, 100, 20);

    [Header("Damage Multiplier Curve")]
    [Tooltip("X-axis = wave number, Y-axis = damage multiplier.")]
    [SerializeField] private AnimationCurve damageMultiplierCurve = AnimationCurve.Linear(1, 1, 100, 20);

    [Header("Spawn Rate Multiplier Curve")]
    [Tooltip("X-axis = wave number, Y-axis = spawn-rate multiplier (1 = normal, 2 = double speed).")]
    [SerializeField] private AnimationCurve spawnRateMultiplierCurve = AnimationCurve.Linear(1, 1, 100, 3);

    [Header("Enemies Per Wave Curve")]
    [Tooltip("X-axis = wave number, Y-axis = enemies to spawn.")]
    [SerializeField] private AnimationCurve enemiesPerWaveCurve = AnimationCurve.Linear(1, 5, 100, 50);

    [Header("Prestige Settings")]
    [Tooltip("Maximum multiplier cap applied during prestige (wave 100+) mode.")]
    [SerializeField] private float prestigeMultiplierCap = 20f;

    [Tooltip("Additional flat multiplier added on top of the cap during prestige waves.")]
    [SerializeField] private float prestigeModifier = 1.1f;

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Full Param Bundle
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a <see cref="WaveParams"/> struct populated with all difficulty
    /// values for the given <paramref name="wave"/> number.
    /// </summary>
    /// <param name="wave">Current wave number (1-based).</param>
    public WaveParams GetWaveParams(int wave)
    {
        return new WaveParams
        {
            HealthMultiplier    = GetEnemyHealthMultiplier(wave),
            DamageMultiplier    = GetEnemyDamageMultiplier(wave),
            SpawnRateMultiplier = GetSpawnRateMultiplier(wave),
            EnemiesPerWave      = GetEnemiesPerWave(wave),
            IsElite             = IsEliteWave(wave),
            IsMiniBoss          = IsMiniBossWave(wave),
            IsPrestige          = IsPrestigeWave(wave),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Individual Multipliers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the HP multiplier for enemies in the given wave.
    /// Uses the Inspector <see cref="healthMultiplierCurve"/> (designer-tweakable).
    /// For prestige waves the curve value is first capped at <see cref="prestigeMultiplierCap"/>
    /// and then the <see cref="prestigeModifier"/> is applied on top, ensuring the modifier
    /// always has a visible effect regardless of the curve's value at wave 100.
    /// </summary>
    /// <param name="wave">Wave number (1-based).</param>
    public float GetEnemyHealthMultiplier(int wave)
    {
        float raw = healthMultiplierCurve.Evaluate(wave);
        return IsPrestigeWave(wave) ? Mathf.Min(raw, prestigeMultiplierCap) * prestigeModifier : raw;
    }

    /// <summary>
    /// Returns the damage multiplier for enemies in the given wave.
    /// Uses the Inspector <see cref="damageMultiplierCurve"/>.
    /// For prestige waves the curve value is capped then multiplied by <see cref="prestigeModifier"/>.
    /// </summary>
    /// <param name="wave">Wave number (1-based).</param>
    public float GetEnemyDamageMultiplier(int wave)
    {
        float raw = damageMultiplierCurve.Evaluate(wave);
        return IsPrestigeWave(wave) ? Mathf.Min(raw, prestigeMultiplierCap) * prestigeModifier : raw;
    }

    /// <summary>
    /// Returns the spawn-rate multiplier for the given wave (1 = normal, 2 = double speed).
    /// Uses the Inspector <see cref="spawnRateMultiplierCurve"/>.
    /// For prestige waves the value is capped at <see cref="prestigeMultiplierCap"/>.
    /// </summary>
    /// <param name="wave">Wave number (1-based).</param>
    public float GetSpawnRateMultiplier(int wave)
    {
        float raw = spawnRateMultiplierCurve.Evaluate(wave);
        return IsPrestigeWave(wave) ? Mathf.Min(raw, prestigeMultiplierCap) : raw;
    }

    /// <summary>
    /// Returns the total number of enemies that should spawn in the given wave.
    /// Base is 5 at wave 1, scaling up via <see cref="enemiesPerWaveCurve"/>.
    /// </summary>
    /// <param name="wave">Wave number (1-based).</param>
    public int GetEnemiesPerWave(int wave)
    {
        return Mathf.Max(1, Mathf.RoundToInt(enemiesPerWaveCurve.Evaluate(wave)));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Wave Type Flags
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if the given wave is an elite wave (wave 20+, every 5 waves).
    /// </summary>
    /// <param name="wave">Wave number (1-based).</param>
    public bool IsEliteWave(int wave) => wave >= 20 && wave % 5 == 0;

    /// <summary>
    /// Returns <c>true</c> if a mini-boss should appear this wave (every 10 waves from wave 31+).
    /// </summary>
    /// <param name="wave">Wave number (1-based).</param>
    public bool IsMiniBossWave(int wave) => wave >= 31 && wave % 10 == 0;

    /// <summary>
    /// Returns <c>true</c> when the wave number exceeds 100, activating prestige (endless) mode.
    /// </summary>
    /// <param name="wave">Wave number (1-based).</param>
    public bool IsPrestigeWave(int wave) => wave > 100;
}
