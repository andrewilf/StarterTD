using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class WallSegmentTower
{
    public static readonly TowerStats Stats = new(
        Name: "Wall",
        Range: 0f,
        Damage: 0f,
        FireRate: float.MaxValue, // never fires
        Cost: 0,
        MovementCost: 10000,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.DarkGreen,
        MaxHealth: 10,
        BlockCapacity: 20,
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: false,
        AbilityDuration: 0f,
        AbilityEffect: null
    );
}
