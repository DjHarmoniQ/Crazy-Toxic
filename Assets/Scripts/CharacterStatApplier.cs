using UnityEngine;

/// <summary>
/// Reads the <see cref="CharacterData"/> chosen on the character-select screen from
/// <see cref="GameManager.Instance"/> and applies it to the player's components.
///
/// Knight buff: when the selected character's name is "Knight", base HP is
/// multiplied by 1.25 and base damage is multiplied by 1.15 before being applied.
///
/// Attach to: The Player GameObject (same as <see cref="Health"/> and <see cref="GunController"/>).
/// </summary>
public class CharacterStatApplier : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        ApplyStats();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the selected <see cref="CharacterData"/> from <see cref="GameManager"/>
    /// and pushes the values (with any class-specific bonuses) to the player components.
    /// </summary>
    private void ApplyStats()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CharacterStatApplier] GameManager not found — stats not applied.");
            return;
        }

        CharacterData data = GameManager.Instance.SelectedCharacter;
        if (data == null)
        {
            Debug.LogWarning("[CharacterStatApplier] No SelectedCharacter set in GameManager — using component defaults.");
            return;
        }

        // --- Start with base stats ---
        float finalMaxHealth = data.maxHealth;
        int finalDamage = Mathf.RoundToInt(data.damage);

        // --- Knight-specific buff: +25 % HP, +15 % damage ---
        if (data.characterName == "Knight")
        {
            finalMaxHealth *= 1.25f;
            finalDamage = Mathf.RoundToInt(finalDamage * 1.15f);
            Debug.Log("[CharacterStatApplier] Knight bonus applied (+25 % HP, +15 % damage).");
        }

        // --- Apply to Health component ---
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.SetMaxHealth(finalMaxHealth);
        }
        else
        {
            Debug.LogWarning("[CharacterStatApplier] Health component not found on player.");
        }

        // --- Apply to GunController component ---
        GunController gun = GetComponent<GunController>();
        if (gun != null)
        {
            gun.Damage = finalDamage;
        }
        else
        {
            Debug.LogWarning("[CharacterStatApplier] GunController component not found on player.");
        }

        // --- Apply move speed to PlayerController ---
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            // PlayerController exposes move speed via a public setter added in Phase 1
            pc.SetMoveSpeed(data.moveSpeed);
        }

        Debug.Log($"[CharacterStatApplier] Applied stats for '{data.characterName}': " +
                  $"HP={finalMaxHealth}, Damage={finalDamage}, Speed={data.moveSpeed}");
    }
}
