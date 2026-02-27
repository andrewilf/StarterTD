using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class GunTowerStats
{
    public static readonly TowerStats Stats = new(
        Name: "Gun Tower",
        Range: 120f,
        Damage: 3f,
        FireRate: 0.3f,
        BaseCooldown: 15f,
        CooldownPenalty: 5f,
        MovementCost: 300,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.Orange,
        MaxHealth: 100,
        BlockCapacity: 3,
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: false,
        AbilityDuration: 4f,
        Targeting: TargetingStrategy.LowestHP,
        AbilityEffect: tower => tower.ActivateAbilityBuff(damageMult: 1f, fireRateSpeedMult: 1.2f)
    );
}
