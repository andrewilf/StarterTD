using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class WallingTowerStats
{
    public static readonly TowerStats Stats = new(
        Name: "Wall Tower",
        Range: 0f,
        Damage: 2f,
        FireRate: 1.8f,
        BaseCooldown: 15f,
        CooldownPenalty: 3f,
        MovementCost: 300,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.DarkGreen,
        MaxHealth: 80,
        BlockCapacity: 3,
        FootprintTiles: new Point(1, 1),
        PlaceholderDrawSize: new Vector2(32f, 32f),
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: false,
        AbilityDuration: 3f,
        AbilityEffect: tower => ((WallingTower)tower).ActivateFrenzy(10f)
    );
}
