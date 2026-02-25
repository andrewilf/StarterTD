using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

/// <summary>
/// Walling tower (generic and champion variants). Places wall segments to form a network
/// and attacks enemies adjacent to that network with spike damage.
/// The frenzy ability hits all enemies in the attack zone simultaneously.
/// </summary>
public class WallingTower : Tower
{
    public WallingTower(TowerType type, Point gridPosition)
        : base(type, gridPosition) { }

    /// <summary>
    /// Activates the wall frenzy mode without modifying Damage or FireRate.
    /// The frenzy attack loop in TowerManager handles multi-target spike hits.
    /// </summary>
    public void ActivateFrenzy(float duration)
    {
        IsAbilityBuffActive = true;
        _abilityTimer = duration;
    }
}
