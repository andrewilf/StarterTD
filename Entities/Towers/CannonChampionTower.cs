using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

/// <summary>
/// Champion cannon tower. Fires AoE projectiles and has a laser super ability
/// that suppresses normal firing for 21 seconds (1s wind-up + 20s beam).
/// </summary>
public class CannonChampionTower : Tower
{
    /// <summary>True while the particle cannon laser ability is active.</summary>
    public bool IsLaserActive { get; private set; }

    public CannonChampionTower(Point gridPosition)
        : base(TowerType.ChampionCannon, gridPosition) { }

    /// <summary>Suppresses normal projectile firing while the laser is active.</summary>
    protected override bool IsFiringSuppressed => IsLaserActive;

    /// <summary>
    /// Activates the particle cannon laser. Suppresses normal firing for AbilityDuration seconds
    /// (covers wind-up + beam). GameplayScene spawns the LaserEffect on receiving OnLaserActivated.
    /// </summary>
    public void ActivateLaser()
    {
        IsAbilityBuffActive = true;
        IsLaserActive = true;
        _abilityTimer = _abilityDuration;
    }

    protected override void OnAbilityDeactivated()
    {
        IsLaserActive = false;
    }
}
