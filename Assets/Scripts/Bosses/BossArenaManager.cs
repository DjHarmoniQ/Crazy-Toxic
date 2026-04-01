using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages the boss-fight arena: locks exits when a boss spawns, unlocks them on
/// boss death, and dispatches named arena mechanics (floor cracks, room darkening,
/// screen shake, minion summons) in response to boss phase-change events.
///
/// Place a single instance in the scene and wire up the boss prefab's
/// <see cref="BossBase.OnPhaseChanged"/> event (or call <see cref="RegisterBoss"/>
/// at runtime after the boss is instantiated).
///
/// Attach to: A dedicated "BossArenaManager" GameObject in the scene.
/// </summary>
public class BossArenaManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single BossArenaManager instance.</summary>
    public static BossArenaManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Arena Walls")]
    [Tooltip("Invisible collider GameObjects that block arena exits. Activated on boss spawn.")]
    [SerializeField] private GameObject[] arenaWalls;

    [Header("Crack Floor Hazard")]
    [Tooltip("Prefab spawned when the 'CrackFloor' mechanic fires. Represents a floor-hazard zone.")]
    [SerializeField] private GameObject crackFloorPrefab;

    [Tooltip("Number of crack-hazard tiles spawned per 'CrackFloor' event.")]
    [SerializeField] private int crackCount = 3;

    [Tooltip("Half-width of the area within which crack tiles are randomly placed.")]
    [SerializeField] private float crackSpawnHalfWidth = 5f;

    [Header("Ambient Lighting")]
    [Tooltip("The scene's global light source. Its intensity is reduced for the 'DarkenRoom' mechanic. Optional.")]
    [SerializeField] private Light2D globalLight;

    [Tooltip("Ambient intensity used during the 'DarkenRoom' effect (0-1).")]
    [SerializeField] private float darkenedIntensity = 0.2f;

    [Header("Phase → Arena Mechanic Mapping")]
    [Tooltip("Optional: arena mechanic name triggered when the boss enters each phase (index 0 = phase 1, etc.).")]
    [SerializeField] private string[] phaseMechanics = { "", "ScreenShake", "CrackFloor", "DarkenRoom", "SummonMinions" };

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private BossBase _currentBoss;
    private float _originalLightIntensity;
    private bool _arenaLocked;

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
        if (globalLight != null)
            _originalLightIntensity = globalLight.intensity;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromBoss();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a boss with the arena, subscribes to its phase-change events,
    /// and locks the arena exits.
    /// </summary>
    /// <param name="boss">The boss that just spawned.</param>
    public void RegisterBoss(BossBase boss)
    {
        UnsubscribeFromBoss();
        _currentBoss = boss;

        if (_currentBoss != null)
            _currentBoss.OnPhaseChanged += OnBossPhaseChanged;

        LockArena();
    }

    /// <summary>Activates invisible arena-wall colliders to prevent the player from leaving the boss room.</summary>
    public void LockArena()
    {
        _arenaLocked = true;
        if (arenaWalls == null) return;

        foreach (GameObject wall in arenaWalls)
        {
            if (wall != null)
                wall.SetActive(true);
        }

        Debug.Log("[BossArenaManager] Arena locked.");
    }

    /// <summary>Deactivates arena walls, called when the boss dies.</summary>
    public void UnlockArena()
    {
        _arenaLocked = false;
        if (arenaWalls == null) return;

        foreach (GameObject wall in arenaWalls)
        {
            if (wall != null)
                wall.SetActive(false);
        }

        // Restore lighting
        if (globalLight != null)
            globalLight.intensity = _originalLightIntensity;

        Debug.Log("[BossArenaManager] Arena unlocked.");
    }

    /// <summary>
    /// Dispatches a named arena mechanic.
    /// Supported names: <c>"CrackFloor"</c>, <c>"DarkenRoom"</c>, <c>"ScreenShake"</c>, <c>"SummonMinions"</c>.
    /// </summary>
    /// <param name="mechanicName">Name of the mechanic to trigger.</param>
    public void TriggerArenaMechanic(string mechanicName)
    {
        Debug.Log($"[BossArenaManager] Triggering mechanic: {mechanicName}");

        switch (mechanicName)
        {
            case "CrackFloor":
                SpawnCrackHazards();
                break;

            case "DarkenRoom":
                DarkenRoom();
                break;

            case "ScreenShake":
                if (CameraController.Instance != null)
                    CameraController.Instance.ShakeCamera(0.5f, 0.3f);
                break;

            case "SummonMinions":
                // EnemySpawner handles its own wave-change events; this is a hint
                // for any listener (e.g. a dedicated minion spawner) to force-spawn.
                Debug.Log("[BossArenaManager] SummonMinions mechanic fired. " +
                          "Connect an EnemySpawner listener to this event if needed.");
                break;

            default:
                Debug.LogWarning($"[BossArenaManager] Unknown mechanic: '{mechanicName}'");
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called when the subscribed boss transitions to a new phase.</summary>
    private void OnBossPhaseChanged(int newPhase)
    {
        Debug.Log($"[BossArenaManager] Boss entered phase {newPhase}.");

        // Trigger the mapped mechanic for this phase (0-indexed)
        int idx = newPhase - 1;
        if (phaseMechanics != null && idx >= 0 && idx < phaseMechanics.Length)
        {
            string mechanic = phaseMechanics[idx];
            if (!string.IsNullOrEmpty(mechanic))
                TriggerArenaMechanic(mechanic);
        }
    }

    /// <summary>Spawns crack-floor hazard objects at random positions within the arena.</summary>
    private void SpawnCrackHazards()
    {
        if (crackFloorPrefab == null) return;

        for (int i = 0; i < crackCount; i++)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(-crackSpawnHalfWidth, crackSpawnHalfWidth),
                0f);
            Instantiate(crackFloorPrefab, randomPos, Quaternion.identity);
        }
    }

    /// <summary>Reduces the global light intensity to simulate a darkened room.</summary>
    private void DarkenRoom()
    {
        if (globalLight != null)
        {
            globalLight.intensity = darkenedIntensity;
            Debug.Log($"[BossArenaManager] Room darkened to intensity {darkenedIntensity}.");
        }
        else
        {
            Debug.LogWarning("[BossArenaManager] DarkenRoom called but no Light2D assigned.");
        }
    }

    /// <summary>Safely unsubscribes from the current boss's events.</summary>
    private void UnsubscribeFromBoss()
    {
        if (_currentBoss != null)
        {
            _currentBoss.OnPhaseChanged -= OnBossPhaseChanged;
            _currentBoss = null;
        }
    }
}
