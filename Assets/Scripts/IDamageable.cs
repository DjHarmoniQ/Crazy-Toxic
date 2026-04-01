/// <summary>
/// Implement this interface on any GameObject that can receive damage
/// (players, enemies, destructible objects, etc.).
/// </summary>
public interface IDamageable
{
    /// <summary>Applies <paramref name="amount"/> damage to this object.</summary>
    void TakeDamage(int amount);
}
