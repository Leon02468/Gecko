using UnityEngine;

public interface IDamageable
{
    /// <summary>
    /// Apply damage to this object.
    /// </summary>
    /// <param name="amount">HP to subtract (positive integer).</param>
    /// <param name="knockback">Optional immediate velocity to apply (world units/sec).</param>
    void TakeDamage(int amount, Vector2? knockback = null);
}