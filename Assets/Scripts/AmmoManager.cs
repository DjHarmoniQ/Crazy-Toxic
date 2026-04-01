using UnityEngine;

/// <summary>
/// Manages the player's ammo inventory: tracks the current ammo type, the
/// remaining round count, and allows switching between available types.
///
/// Attach to: The Player GameObject.
/// </summary>
public class AmmoManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ammo Types")]
    [Tooltip("All ammo types the player can cycle through during a run.")]
    [SerializeField] private AmmoType[] availableAmmoTypes;

    [Tooltip("The ammo type equipped at the start of the run.")]
    [SerializeField] private AmmoType defaultAmmoType;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private int[] _inventory; // round counts matching availableAmmoTypes indices
    private int _currentIndex;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The ammo type currently equipped by the player.</summary>
    public AmmoType CurrentAmmo { get; private set; }

    /// <summary>Number of rounds remaining for the currently equipped ammo type.</summary>
    public int CurrentAmmoCount { get; private set; }

    /// <summary>Zero-based index of the currently active ammo type in the available array.</summary>
    public int CurrentAmmoIndex => _currentIndex;

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised whenever the current ammo type or count changes.
    /// Parameters: new <see cref="AmmoType"/>, new round count.
    /// </summary>
    public event System.Action<AmmoType, int> OnAmmoChanged;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Build inventory array — full magazines for every ammo type
        if (availableAmmoTypes != null && availableAmmoTypes.Length > 0)
        {
            _inventory = new int[availableAmmoTypes.Length];
            for (int i = 0; i < availableAmmoTypes.Length; i++)
            {
                if (availableAmmoTypes[i] != null)
                    _inventory[i] = availableAmmoTypes[i].maxAmmo;
            }
        }
        else
        {
            _inventory = new int[0];
        }

        // Equip the default ammo type (or fall back to index 0)
        if (defaultAmmoType != null)
        {
            _currentIndex = FindAmmoIndex(defaultAmmoType);
            if (_currentIndex < 0) _currentIndex = 0;
        }
        else
        {
            _currentIndex = 0;
        }

        ApplyCurrentAmmo();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Switches the active ammo type to the entry at <paramref name="index"/>
    /// in <c>availableAmmoTypes</c>.
    /// </summary>
    /// <param name="index">Zero-based index into the available ammo array.</param>
    public void SwitchAmmo(int index)
    {
        if (availableAmmoTypes == null || availableAmmoTypes.Length == 0) return;
        if (index < 0 || index >= availableAmmoTypes.Length) return;

        _currentIndex = index;
        ApplyCurrentAmmo();
    }

    /// <summary>
    /// Switches the active ammo type to <paramref name="type"/> if it exists in the
    /// available ammo list.
    /// </summary>
    /// <param name="type">The <see cref="AmmoType"/> ScriptableObject to equip.</param>
    /// <returns><c>true</c> if the switch succeeded; <c>false</c> if the type was not found.</returns>
    public bool SwitchToAmmo(AmmoType type)
    {
        int idx = FindAmmoIndex(type);
        if (idx < 0) return false;
        SwitchAmmo(idx);
        return true;
    }

    /// <summary>
    /// Attempts to consume one round of the current ammo type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if a round was consumed (or the type is infinite);
    /// <c>false</c> if the magazine is empty.
    /// </returns>
    public bool TryConsumeAmmo()
    {
        if (CurrentAmmo == null) return false;
        if (CurrentAmmo.isInfinite) return true;

        if (CurrentAmmoCount <= 0)
        {
            Debug.Log($"[AmmoManager] Out of ammo: {CurrentAmmo.ammoName}");
            OnAmmoChanged?.Invoke(CurrentAmmo, 0);
            return false;
        }

        CurrentAmmoCount--;
        if (_inventory != null && _currentIndex < _inventory.Length)
            _inventory[_currentIndex] = CurrentAmmoCount;

        OnAmmoChanged?.Invoke(CurrentAmmo, CurrentAmmoCount);
        return true;
    }

    /// <summary>
    /// Adds <paramref name="amount"/> rounds of <paramref name="type"/> to the
    /// player's inventory. Capped at <c>maxAmmo</c>.
    /// </summary>
    /// <param name="type">The ammo type to replenish.</param>
    /// <param name="amount">Number of rounds to add.</param>
    public void AddAmmo(AmmoType type, int amount)
    {
        if (type == null || amount <= 0) return;

        int idx = FindAmmoIndex(type);
        if (idx < 0 || _inventory == null || idx >= _inventory.Length) return;

        _inventory[idx] = Mathf.Min(_inventory[idx] + amount, type.maxAmmo);

        // Refresh displayed count if this is the currently active type
        if (idx == _currentIndex)
        {
            CurrentAmmoCount = _inventory[idx];
            OnAmmoChanged?.Invoke(CurrentAmmo, CurrentAmmoCount);
        }
    }

    /// <summary>
    /// Refills the current ammo type to its maximum capacity.
    /// </summary>
    public void Reload()
    {
        if (CurrentAmmo == null) return;
        if (CurrentAmmo.isInfinite) return;

        CurrentAmmoCount = CurrentAmmo.maxAmmo;
        if (_inventory != null && _currentIndex < _inventory.Length)
            _inventory[_currentIndex] = CurrentAmmoCount;

        OnAmmoChanged?.Invoke(CurrentAmmo, CurrentAmmoCount);
        Debug.Log($"[AmmoManager] Reloaded {CurrentAmmo.ammoName} to {CurrentAmmoCount}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Applies the ammo type at <see cref="_currentIndex"/> as the active one.</summary>
    private void ApplyCurrentAmmo()
    {
        if (availableAmmoTypes == null || availableAmmoTypes.Length == 0)
        {
            CurrentAmmo = defaultAmmoType;
            CurrentAmmoCount = defaultAmmoType != null ? defaultAmmoType.maxAmmo : 0;
            OnAmmoChanged?.Invoke(CurrentAmmo, CurrentAmmoCount);
            return;
        }

        CurrentAmmo = availableAmmoTypes[_currentIndex];
        CurrentAmmoCount = (_inventory != null && _currentIndex < _inventory.Length)
            ? _inventory[_currentIndex]
            : 0;

        OnAmmoChanged?.Invoke(CurrentAmmo, CurrentAmmoCount);
    }

    /// <summary>Returns the index of <paramref name="type"/> in <c>availableAmmoTypes</c>, or -1.</summary>
    private int FindAmmoIndex(AmmoType type)
    {
        if (availableAmmoTypes == null) return -1;
        for (int i = 0; i < availableAmmoTypes.Length; i++)
        {
            if (availableAmmoTypes[i] == type) return i;
        }
        return -1;
    }
}
