namespace StarterTD.Entities;

/// <summary>
/// Available tower types. Each has a corresponding definition file
/// (e.g., GunTower.cs) with its stats.
/// Generic types (Gun, Cannon) cost gold. Champion variants are free.
/// </summary>
public enum TowerType
{
    Gun,
    Cannon,
    Walling,
    ChampionGun,
    ChampionCannon,
    ChampionWalling,
    WallSegment,
}
