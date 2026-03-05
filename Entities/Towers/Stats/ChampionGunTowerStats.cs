using Microsoft.Xna.Framework;
using StarterTD.Engine;

namespace StarterTD.Entities;

public static class ChampionGunTowerStats
{
    public static readonly TowerStats Stats = new(
        Name: "Champion Gun",
        Range: 150f,
        Damage: 5f,
        FireRate: 0.4f,
        BaseCooldown: 30f,
        CooldownPenalty: 10f,
        MovementCost: 300,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.Orange,
        MaxHealth: 80,
        BlockCapacity: 2,
        FootprintTiles: new Point(2, 2),
        PlaceholderDrawSize: new Vector2(GameSettings.TileSize * 2f, GameSettings.TileSize * 2f),
        DrawScale: new Vector2(1.0f, 1.0f),
        CanWalk: true,
        MoveSpeed: 150f,
        CooldownDuration: 2.0f,
        AbilityDuration: 5f,
        AbilityCooldown: 15f,
        Targeting: TargetingStrategy.LowestHP,
        AbilityEffect: tower => tower.ActivateAbilityBuff(damageMult: 2f, fireRateSpeedMult: 1.4f)
    );
}
