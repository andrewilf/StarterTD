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
    Sniper,
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
        int UpgradeCost,
        int MovementCost,
        bool IsAOE,
        float AOERadius,
        Color Color,
        int MaxHealth
    );

    /// <summary>
    /// Get stats for a tower type at a given level.
    /// Level 1 = base stats, Level 2 = upgraded stats.
    /// </summary>
    public static TowerStats GetStats(TowerType type, int level)
    {
        return (type, level) switch
        {
            // Gun Tower: Fast fire rate, low damage, short range
            (TowerType.Gun, 1) => new TowerStats(
                "Gun Tower",
                Range: 120f,
                Damage: 3f,
                FireRate: 0.3f,
                Cost: 50,
                UpgradeCost: 40,
                MovementCost: 300,
                IsAOE: false,
                AOERadius: 0f,
                Color: new Color(65, 105, 225),
                MaxHealth: 100
            ), // Royal Blue

            (TowerType.Gun, 2) => new TowerStats(
                "Gun Tower Mk2",
                Range: 150f,
                Damage: 6f,
                FireRate: 0.2f,
                Cost: 50,
                UpgradeCost: 0,
                MovementCost: 300,
                IsAOE: false,
                AOERadius: 0f,
                Color: new Color(30, 60, 180),
                MaxHealth: 120
            ),

            // Cannon Tower: Slow fire rate, AOE damage, medium range
            (TowerType.Cannon, 1) => new TowerStats(
                "Cannon Tower",
                Range: 100f,
                Damage: 2f,
                FireRate: 1.5f,
                Cost: 80,
                UpgradeCost: 60,
                MovementCost: 500,
                IsAOE: true,
                AOERadius: 50f,
                Color: new Color(178, 34, 34),
                MaxHealth: 150
            ), // Firebrick Red

            (TowerType.Cannon, 2) => new TowerStats(
                "Cannon Tower Mk2",
                Range: 120f,
                Damage: 4f,
                FireRate: 1.2f,
                Cost: 80,
                UpgradeCost: 0,
                MovementCost: 500,
                IsAOE: true,
                AOERadius: 70f,
                Color: new Color(139, 0, 0),
                MaxHealth: 180
            ),

            // Sniper Tower: Very slow, high damage, long range
            (TowerType.Sniper, 1) => new TowerStats(
                "Sniper Tower",
                Range: 250f,
                Damage: 10f,
                FireRate: 2.0f,
                Cost: 100,
                UpgradeCost: 80,
                MovementCost: 700,
                IsAOE: false,
                AOERadius: 0f,
                Color: new Color(148, 0, 211),
                MaxHealth: 80
            ), // Dark Violet

            (TowerType.Sniper, 2) => new TowerStats(
                "Sniper Tower Mk2",
                Range: 300f,
                Damage: 25f,
                FireRate: 1.5f,
                Cost: 100,
                UpgradeCost: 0,
                MovementCost: 700,
                IsAOE: false,
                AOERadius: 0f,
                Color: new Color(100, 0, 160),
                MaxHealth: 100
            ),

            _ => throw new ArgumentException($"No stats for {type} level {level}"),
        };
    }
}
