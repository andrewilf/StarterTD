using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class ChampionWallingTowerStats
{
    public static readonly TowerStats Stats = new(
        Name: "Champion Wall",
        Range: 0f,
        Damage: 3f,
        FireRate: 1.5f,
        BaseCooldown: 30f,
        CooldownPenalty: 10f,
        MovementCost: 500,
        IsAOE: false,
        AOERadius: 0f,
        Color: Color.DarkGreen,
        MaxHealth: 150,
        BlockCapacity: 5,
        DrawScale: new Vector2(1.0f, 1.5f),
        CanWalk: true,
        MoveSpeed: 80f,
        CooldownDuration: 2.0f,
        AbilityDuration: 5f, // Slow duration in seconds applied to enemies hit by spike attack
        AbilityCooldown: 20f,
        AbilityEffect: tower => ((WallingTower)tower).ActivateFrenzy(10f)
    );
}
