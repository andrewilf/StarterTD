using System;

namespace StarterTD.Entities;

/// <summary>
/// Registry that maps TowerType → TowerStats.
/// Individual stats live in per-tower files (GunTowerStats.cs, CannonTowerStats.cs, etc.).
/// To add a new tower: create a definition file, add the enum value, and register it here.
/// </summary>
public static class TowerData
{
    public static TowerStats GetStats(TowerType type)
    {
        return type switch
        {
            TowerType.Gun => GunTowerStats.Stats,
            TowerType.Cannon => CannonTowerStats.Stats,
            TowerType.Walling => WallingTowerStats.Stats,
            TowerType.ChampionGun => ChampionGunTowerStats.Stats,
            TowerType.ChampionCannon => ChampionCannonTowerStats.Stats,
            TowerType.ChampionWalling => ChampionWallingTowerStats.Stats,
            TowerType.WallSegment => WallSegmentTowerStats.Stats,
            _ => throw new ArgumentException($"No stats for {type}"),
        };
    }
}
