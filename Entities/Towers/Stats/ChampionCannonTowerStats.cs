using Microsoft.Xna.Framework;
using StarterTD.Engine;

namespace StarterTD.Entities;

public static class ChampionCannonTowerStats
{
    public static readonly TowerStats Stats = new(
        Name: "Champion Cannon",
        Range: 120f,
        Damage: 40f,
        FireRate: 1.0f,
        BaseCooldown: 10f,
        CooldownPenalty: 3f,
        MovementCost: 500,
        IsAOE: true,
        AOERadius: 30f,
        Color: Color.Firebrick,
        MaxHealth: 500,
        BlockCapacity: 1,
        FootprintTiles: new Point(2, 2),
        PlaceholderDrawSize: new Vector2(GameSettings.TileSize * 2f, GameSettings.TileSize * 2f),
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: true,
        MoveSpeed: 80f,
        CooldownDuration: 2.0f,
        AbilityDuration: 21f,
        AbilityCooldown: 50f,
        Targeting: TargetingStrategy.MostGrouped,
        AbilityEffect: tower => ((CannonChampionTower)tower).ActivateLaser()
    );
}
