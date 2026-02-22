using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class WallingTower
{
    public static readonly TowerStats Stats = new(
        Name: "Wall Tower",
        Range: 0f,
        Damage: 2f,
        FireRate: 1.8f,
        Cost: 50,
        MovementCost: 300,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.Green,
        MaxHealth: 80,
        BlockCapacity: 3,
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: false,
        AbilityDuration: 3f,
        AbilityEffect: tower => tower.ActivateFrenzy(10f)
    );
}
