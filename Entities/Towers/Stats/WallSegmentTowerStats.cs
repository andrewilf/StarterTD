using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class WallSegmentTowerStats
{
    public static readonly TowerStats Stats = new(
        Name: "Wall",
        Range: 0f,
        Damage: 0f,
        FireRate: float.MaxValue, // never fires
        BaseCooldown: 0f,
        CooldownPenalty: 0f,
        MovementCost: 10000,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.Green,
        MaxHealth: 30,
        BlockCapacity: 5,
        FootprintTiles: new Point(1, 1),
        PlaceholderDrawSize: new Vector2(32f, 32f),
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: false,
        AbilityDuration: 0f,
        AbilityEffect: null
    );
}
