using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class ChampionGunTower
{
    public static readonly TowerStats Stats = new(
        Name: "Champion Gun",
        Range: 150f,
        Damage: 5f,
        FireRate: 0.4f,
        Cost: 0,
        MovementCost: 300,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.Orange,
        MaxHealth: 80,
        BlockCapacity: 2,
        DrawScale: new Vector2(1.0f, 1.5f)
    );
}
