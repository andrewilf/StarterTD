using Microsoft.Xna.Framework;
using StarterTD.Engine;

namespace StarterTD.Entities;

public static class ChampionHealingTowerStats
{
    public static readonly TowerStats Stats = new(
        Name: "Champion Healing",
        Range: 320f,
        Damage: 100f,
        FireRate: 2.4f,
        BaseCooldown: 10f,
        CooldownPenalty: 3f,
        MovementCost: 300,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.Yellow,
        MaxHealth: 160,
        BlockCapacity: 1,
        FootprintTiles: new Point(2, 2),
        PlaceholderDrawSize: new Vector2(GameSettings.TileSize * 2f, GameSettings.TileSize * 2f),
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: true,
        MoveSpeed: 120f,
        CooldownDuration: 2.0f,
        AbilityDuration: 15f,
        AbilityCooldown: 50f,
        Targeting: TargetingStrategy.Closest,
        AbilityEffect: null
    );
}
