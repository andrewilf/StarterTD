using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class CannonTower
{
    public static readonly TowerStats Stats = new(
        Name: "Cannon Tower",
        Range: 100f,
        Damage: 2f,
        FireRate: 1.5f,
        Cost: 80,
        MovementCost: 500,
        IsAOE: true,
        AOERadius: 50f,
        Color: Color.Firebrick,
        MaxHealth: 150,
        BlockCapacity: 2,
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: false,
        AbilityDuration: 6f,
        AbilityEffect: tower => tower.ActivateAbilityBuff(damageMult: 1f, fireRateSpeedMult: 1.2f)
    );
}
