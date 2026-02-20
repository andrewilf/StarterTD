using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

public static class ChampionWallingTower
{
    public static readonly TowerStats Stats = new(
        Name: "Champion Wall",
        Range: 0f,
        Damage: 0f,
        FireRate: float.MaxValue, // never fires
        Cost: 0,
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
        AbilityDuration: 0f,
        AbilityCooldown: 0f,
        AbilityEffect: null
    );
}
