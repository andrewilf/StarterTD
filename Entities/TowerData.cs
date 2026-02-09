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
    Sniper
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
        bool IsAOE,
        float AOERadius,
        Color Color);

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
                "Gun Tower", Range: 120f, Damage: 8f, FireRate: 0.3f,
                Cost: 50, UpgradeCost: 40, IsAOE: false, AOERadius: 0f,
                Color: new Color(65, 105, 225)),  // Royal Blue

            (TowerType.Gun, 2) => new TowerStats(
                "Gun Tower Mk2", Range: 150f, Damage: 15f, FireRate: 0.2f,
                Cost: 50, UpgradeCost: 0, IsAOE: false, AOERadius: 0f,
                Color: new Color(30, 60, 180)),

            // Cannon Tower: Slow fire rate, AOE damage, medium range
            (TowerType.Cannon, 1) => new TowerStats(
                "Cannon Tower", Range: 100f, Damage: 25f, FireRate: 1.5f,
                Cost: 80, UpgradeCost: 60, IsAOE: true, AOERadius: 50f,
                Color: new Color(178, 34, 34)),  // Firebrick Red

            (TowerType.Cannon, 2) => new TowerStats(
                "Cannon Tower Mk2", Range: 120f, Damage: 45f, FireRate: 1.2f,
                Cost: 80, UpgradeCost: 0, IsAOE: true, AOERadius: 70f,
                Color: new Color(139, 0, 0)),

            // Sniper Tower: Very slow, high damage, long range
            (TowerType.Sniper, 1) => new TowerStats(
                "Sniper Tower", Range: 250f, Damage: 50f, FireRate: 2.0f,
                Cost: 100, UpgradeCost: 80, IsAOE: false, AOERadius: 0f,
                Color: new Color(148, 0, 211)),  // Dark Violet

            (TowerType.Sniper, 2) => new TowerStats(
                "Sniper Tower Mk2", Range: 300f, Damage: 90f, FireRate: 1.5f,
                Cost: 100, UpgradeCost: 0, IsAOE: false, AOERadius: 0f,
                Color: new Color(100, 0, 160)),

            _ => throw new ArgumentException($"No stats for {type} level {level}")
        };
    }
}
