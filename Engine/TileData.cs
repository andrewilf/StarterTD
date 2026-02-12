using System;

namespace StarterTD.Engine;

/// <summary>
/// Registry that maps TileType → TileStats.
/// Individual stats live in per-tile files (HighGroundTile.cs, PathTile.cs, etc.).
/// To add a new tile type: create a definition file, add the enum value, and register it here.
/// </summary>
public static class TileData
{
    public static TileStats GetStats(TileType type)
    {
        return type switch
        {
            TileType.HighGround => HighGroundTile.Stats,
            TileType.Path => PathTile.Stats,
            TileType.Rock => RockTile.Stats,
            _ => throw new ArgumentException($"No stats for {type}"),
        };
    }
}
