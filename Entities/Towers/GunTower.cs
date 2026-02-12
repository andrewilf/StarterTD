using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

/// <summary>
/// Gun Tower: fast-firing single-target tower.
/// High DPS (~10), short range, cheap.
/// </summary>
public static class GunTower
{
    public static readonly TowerStats Stats = new(
        Name: "Gun Tower",
        Range: 120f,
        Damage: 3f,
        FireRate: 0.3f,
        Cost: 50,
        MovementCost: 300,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.Orange,
        MaxHealth: 100,
        BlockCapacity: 3,
        DrawScale: new Vector2(1.0f, 1.0f)
    );
}
