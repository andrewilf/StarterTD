using System;
using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

/// <summary>
/// Immutable stat block shared by all tower types.
/// Each tower definition file creates one of these.
/// </summary>
public record TowerStats(
    string Name,
    float Range, // Attack range in pixels
    float Damage, // Damage per shot
    float FireRate, // Seconds between shots
    float BaseCooldown, // Base seconds added to this type's placement pool on placement
    float CooldownPenalty, // Additional seconds stacked on top of BaseCooldown per placement
    int MovementCost, // Pathfinding weight when placed on a tile
    bool IsAOE, // Area-of-effect vs single-target
    float AOERadius, // Splash radius in pixels (0 if not AOE)
    Color Color, // Visual color for placeholder sprite
    int MaxHealth, // Hit points
    int BlockCapacity, // Max enemies that can attack this tower at once
    Vector2 DrawScale, // Render scale (champions use 1.0, 1.5 to appear taller)
    bool CanWalk, // Whether this tower type is allowed to move on the map
    float MoveSpeed = 0f, // Pixels per second while in Moving state (only used when CanWalk is true)
    float CooldownDuration = 0f, // Seconds in Cooldown state after movement finishes (only used when CanWalk is true)
    float AbilityDuration = 5f, // How long the ability buff lasts in seconds (all towers that can receive the buff)
    float AbilityCooldown = 0f, // Seconds before the ability can be triggered again (champions only; generics omit this)
    TargetingStrategy Targeting = TargetingStrategy.Closest, // Which enemy-selection strategy this tower uses
    // Called on this tower instance when the champion super ability fires.
    // Action<Tower> is a C# delegate — like a typed function pointer or Python callable.
    // Storing it here keeps each tower type's ability self-contained in its own file.
    Action<Tower>? AbilityEffect = null
);

/// <summary>
/// Extension methods for mapping between Generic and Champion tower variants.
/// </summary>
public static class TowerTypeExtensions
{
    public static bool IsChampion(this TowerType type) =>
        type == TowerType.ChampionGun
        || type == TowerType.ChampionCannon
        || type == TowerType.ChampionWalling;

    public static bool IsWallingChampion(this TowerType type) => type == TowerType.ChampionWalling;

    public static bool IsWallingGeneric(this TowerType type) => type == TowerType.Walling;

    public static bool IsWallSegment(this TowerType type) => type == TowerType.WallSegment;

    /// <summary>
    /// Get the Generic variant of a Champion type.
    /// Example: ChampionGun → Gun
    /// </summary>
    public static TowerType GetGenericVariant(this TowerType championType)
    {
        return championType switch
        {
            TowerType.ChampionGun => TowerType.Gun,
            TowerType.ChampionCannon => TowerType.Cannon,
            TowerType.ChampionWalling => TowerType.Walling,
            _ => throw new ArgumentException($"{championType} is not a champion type"),
        };
    }

    /// <summary>
    /// Get the Champion variant of a Generic type.
    /// Example: Gun → ChampionGun
    /// </summary>
    public static TowerType GetChampionVariant(this TowerType genericType)
    {
        return genericType switch
        {
            TowerType.Gun => TowerType.ChampionGun,
            TowerType.Cannon => TowerType.ChampionCannon,
            TowerType.Walling => TowerType.ChampionWalling,
            _ => throw new ArgumentException($"{genericType} is not a generic type"),
        };
    }
}
