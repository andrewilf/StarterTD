using System;
using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

/// <summary>
/// Enum of available tower types for selection.
/// </summary>
public enum TowerType
{
    Gun,
    Cannon,
}

/// <summary>
/// Static data definitions for each tower type and level.
/// This acts like a JSON config file — change values here to balance the game.
/// </summary>
public static class TowerData
{
    public record TowerStats(
        string Name,
        float Range,
        float Damage,
        float FireRate,
        int Cost,
        int MovementCost,
        bool IsAOE,
        float AOERadius,
        Color Color,
        int MaxHealth,
        int BlockCapacity
    );

    /// <summary>
    /// Get stats for a tower type.
    /// </summary>
    public static TowerStats GetStats(TowerType type)
    {
        return type switch
        {
            // Gun Tower: Fast fire rate, low damage, short range
            TowerType.Gun => new TowerStats(
                "Gun Tower",
                Range: 120f,
                Damage: 3f,
                FireRate: 0.3f,
                Cost: 50,
                MovementCost: 300,
                IsAOE: false,
                AOERadius: 0f,
                Color: Color.Orange,
                MaxHealth: 100,
                BlockCapacity: 3
            ),

            // Cannon Tower: Slow fire rate, AOE damage, medium range
            TowerType.Cannon => new TowerStats(
                "Cannon Tower",
                Range: 100f,
                Damage: 2f,
                FireRate: 1.5f,
                Cost: 80,
                MovementCost: 500,
                IsAOE: true,
                AOERadius: 50f,
                Color: Color.Firebrick,
                MaxHealth: 150,
                BlockCapacity: 2
            ),

            _ => throw new ArgumentException($"No stats for {type}"),
        };
    }
}
