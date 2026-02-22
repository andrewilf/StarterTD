using System;

namespace StarterTD.Entities;

/// <summary>
/// Registry that maps TowerType → TowerStats.
/// Individual stats live in per-tower files (GunTower.cs, CannonTower.cs, etc.).
/// To add a new tower: create a definition file, add the enum value, and register it here.
/// </summary>
public static class TowerData
{
    public static TowerStats GetStats(TowerType type)
    {
        return type switch
        {
            TowerType.Gun => GunTower.Stats,
            TowerType.Cannon => CannonTower.Stats,
            TowerType.Walling => WallingTower.Stats,
            TowerType.ChampionGun => ChampionGunTower.Stats,
            TowerType.ChampionCannon => ChampionCannonTower.Stats,
            TowerType.ChampionWalling => ChampionWallingTower.Stats,
            TowerType.WallSegment => WallSegmentTower.Stats,
            _ => throw new ArgumentException($"No stats for {type}"),
        };
    }
}
