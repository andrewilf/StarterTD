using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StarterTD.Entities;

namespace StarterTD.Managers;

/// <summary>
/// Manages champion placement rules and cooldowns:
/// - Global 10s cooldown blocks all champion placements after any champion is placed
/// - Individual 15s respawn cooldowns prevent re-placement of dead champions
/// - Generic tower placement requires their champion variant to be alive
/// - Triggers debuff callbacks on generics when their champion dies
/// </summary>
public class ChampionManager
{
    /// <summary>Champion types that currently have an alive tower on the map.</summary>
    private readonly HashSet<TowerType> _aliveChampions = new();

    /// <summary>
    /// Blocks all champion placement for 10 seconds after any champion is placed.
    /// Decrements each frame via Update().
    /// </summary>
    private float _globalPlacementCooldown = 0f;

    /// <summary>
    /// Respawn timers for each champion type that has died (15s per type).
    /// Removed from dict when timer reaches 0.
    /// </summary>
    private readonly Dictionary<TowerType, float> _respawnCooldowns = new();

    /// <summary>Per-champion ability cooldowns (15s after use). Removed from dict when expired.</summary>
    private readonly Dictionary<TowerType, float> _abilityCooldowns = new();

    private const float GLOBAL_PLACEMENT_COOLDOWN = 10.0f;
    private const float RESPAWN_COOLDOWN = 15.0f;

    /// <summary>
    /// Check if a champion tower can be placed.
    /// Returns false if:
    /// - A champion of this type is already alive, OR
    /// - Global placement cooldown is active, OR
    /// - This champion's individual respawn cooldown is active
    /// </summary>
    public bool CanPlaceChampion(TowerType type)
    {
        if (_aliveChampions.Contains(type))
            return false;

        if (_globalPlacementCooldown > 0)
            return false;

        if (_respawnCooldowns.TryGetValue(type, out var cooldown) && cooldown > 0)
            return false;

        return true;
    }

    /// <summary>
    /// Check if a generic tower can be placed.
    /// Returns false if the corresponding champion variant is not alive.
    /// </summary>
    public bool CanPlaceGeneric(TowerType type)
    {
        var championVariant = type.GetChampionVariant();
        return _aliveChampions.Contains(championVariant);
    }

    /// <summary>
    /// Get the remaining global placement cooldown time (0 if no active cooldown).
    /// Used by UI to display cooldown feedback to players.
    /// </summary>
    public float GlobalCooldownRemaining => Math.Max(0, _globalPlacementCooldown);

    /// <summary>
    /// Get the remaining respawn cooldown for a specific champion type (0 if none).
    /// Used by UI to display per-champion respawn timers.
    /// </summary>
    public float GetRespawnCooldown(TowerType type)
    {
        return _respawnCooldowns.TryGetValue(type, out var cooldown) ? Math.Max(0, cooldown) : 0f;
    }

    /// <summary>
    /// Check if a specific champion type currently has an alive tower on the map.
    /// Used by UI to determine button states (e.g., "Limit Reached").
    /// </summary>
    public bool IsChampionAlive(TowerType type)
    {
        return _aliveChampions.Contains(type);
    }

    /// <summary>Start the 15s ability cooldown after the player triggers the super ability.</summary>
    public void StartAbilityCooldown(TowerType championType)
    {
        _abilityCooldowns[championType] = TowerData.GetStats(championType).AbilityCooldown;
    }

    /// <summary>Remaining seconds before the ability can be used again (0 if ready).</summary>
    public float GetAbilityCooldownRemaining(TowerType championType)
    {
        return _abilityCooldowns.TryGetValue(championType, out var cd) ? Math.Max(0f, cd) : 0f;
    }

    /// <summary>True when the champion is alive and the ability cooldown has expired.</summary>
    public bool IsAbilityReady(TowerType championType)
    {
        return IsChampionAlive(championType) && GetAbilityCooldownRemaining(championType) <= 0f;
    }

    /// <summary>
    /// Called when a champion tower is successfully placed.
    /// Marks it alive, starts global cooldown, and clears any respawn cooldown.
    /// </summary>
    public void OnChampionPlaced(TowerType type)
    {
        _aliveChampions.Add(type);
        _globalPlacementCooldown = GLOBAL_PLACEMENT_COOLDOWN;
        _respawnCooldowns.Remove(type);
    }

    /// <summary>
    /// Called when a champion tower dies.
    /// Marks it dead, starts respawn cooldown, and notifies all matching generic towers
    /// to apply champion-death debuffs via UpdateChampionStatus(false).
    /// </summary>
    public void OnChampionDeath(TowerType type, List<Tower> allTowers)
    {
        _aliveChampions.Remove(type);
        _respawnCooldowns[type] = RESPAWN_COOLDOWN;

        var genericVariant = type.GetGenericVariant();

        foreach (var tower in allTowers)
        {
            if (tower.TowerType == genericVariant && !tower.IsDead)
                tower.UpdateChampionStatus(false);
        }
    }

    /// <summary>
    /// Update all timers (called once per frame).
    /// Decrements global and respawn cooldowns, removing respawn entries when they hit 0.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_globalPlacementCooldown > 0)
            _globalPlacementCooldown -= dt;

        List<TowerType>? keysToRemove = null;
        foreach (var type in _respawnCooldowns.Keys)
        {
            _respawnCooldowns[type] -= dt;
            if (_respawnCooldowns[type] <= 0)
                (keysToRemove ??= []).Add(type);
        }

        if (keysToRemove != null)
            foreach (var type in keysToRemove)
                _respawnCooldowns.Remove(type);

        List<TowerType>? abilityKeysToRemove = null;
        foreach (var type in _abilityCooldowns.Keys)
        {
            _abilityCooldowns[type] -= dt;
            if (_abilityCooldowns[type] <= 0)
                (abilityKeysToRemove ??= []).Add(type);
        }

        if (abilityKeysToRemove != null)
            foreach (var type in abilityKeysToRemove)
                _abilityCooldowns.Remove(type);
    }
}
