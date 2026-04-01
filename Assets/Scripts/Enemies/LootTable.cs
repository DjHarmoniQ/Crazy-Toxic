using UnityEngine;

/// <summary>
/// ScriptableObject that defines a weighted loot table for enemy drops.
/// Create instances via <c>Assets → Create → Crazy-Toxic → LootTable</c>.
/// Each entry specifies an optional drop prefab, an <see cref="AmmoType"/>
/// reference, an amount, and a drop-chance probability (0–1).
/// </summary>
[CreateAssetMenu(menuName = "Crazy-Toxic/LootTable")]
public class LootTable : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Nested Types
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a single entry in the loot table.
    /// </summary>
    [System.Serializable]
    public class LootEntry
    {
        [Header("Drop Identity")]
        [Tooltip("The prefab to instantiate when this entry is rolled successfully. " +
                 "Can be an ammo pickup, mana orb, or health pack.")]
        public GameObject dropPrefab;

        [Tooltip("Ammo type reference (used by ammo pickup prefabs to know which ammo to grant).")]
        public AmmoType ammoType;

        [Header("Amount")]
        [Tooltip("Number of units/charges the pickup grants (e.g. 10–30 ammo, 10–20 HP).")]
        public int amount = 10;

        [Header("Probability")]
        [Tooltip("Drop chance in the range 0 (never) to 1 (always).")]
        [Range(0f, 1f)]
        public float dropChance = 0.5f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Data
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Loot Entries")]
    [Tooltip("Array of possible drops. Each entry is checked independently against its drop chance.")]
    [SerializeField] public LootEntry[] entries = System.Array.Empty<LootEntry>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates each <see cref="LootEntry"/> against its <c>dropChance</c> and returns
    /// the subset of entries that successfully rolled.
    /// </summary>
    /// <returns>
    /// An array of <see cref="LootEntry"/> instances that passed their individual
    /// drop-chance check. May be empty if nothing drops.
    /// </returns>
    public LootEntry[] Roll()
    {
        if (entries == null || entries.Length == 0)
            return System.Array.Empty<LootEntry>();

        var results = new System.Collections.Generic.List<LootEntry>(entries.Length);
        foreach (LootEntry entry in entries)
        {
            if (Random.value <= entry.dropChance)
                results.Add(entry);
        }
        return results.ToArray();
    }
}
