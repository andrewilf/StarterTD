using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class ChampionCannonTower
{
    public static readonly TowerStats Stats = new(
        Name: "Champion Cannon",
        Range: 120f,
        Damage: 4f,
        FireRate: 1.0f,
        Cost: 0,
        MovementCost: 500,
        IsAOE: true,
        AOERadius: 70f,
        Color: Color.Firebrick,
        MaxHealth: 200,
        BlockCapacity: 1,
        DrawScale: new Vector2(1.0f, 1.5f)
    );
}
