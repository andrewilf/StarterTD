using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Data-driven map configuration. Defines terrain layout via rectangles.
/// Dijkstra computes the actual enemy route at runtime from tile costs.
///
/// Python/TS analogy: Like a config dict { spawn, exit, walkable_rects, ... }
/// that a Map class reads to build a 2D grid.
/// </summary>
public record MapData(
    string Name,                        // Map display name
    string Id,                          // Unique identifier
    Point SpawnPoint,                   // Where enemies enter the map
    Point ExitPoint,                    // Where enemies leave the map
    List<Rectangle> WalkableAreas,      // Rectangles of Path tiles (walkable + buildable terrain)
    int Columns = 20,                   // Grid width
    int Rows = 15                       // Grid height
)
{
    /// <summary>
    /// Validates map data. Throws ArgumentException if invalid.
    /// </summary>
    public void Validate()
    {
        if (SpawnPoint.X < 0 || SpawnPoint.X >= Columns || SpawnPoint.Y < 0 || SpawnPoint.Y >= Rows)
            throw new ArgumentException($"Map '{Name}': SpawnPoint ({SpawnPoint.X}, {SpawnPoint.Y}) is out of bounds");

        if (ExitPoint.X < 0 || ExitPoint.X >= Columns || ExitPoint.Y < 0 || ExitPoint.Y >= Rows)
            throw new ArgumentException($"Map '{Name}': ExitPoint ({ExitPoint.X}, {ExitPoint.Y}) is out of bounds");

        if (SpawnPoint == ExitPoint)
            throw new ArgumentException($"Map '{Name}': SpawnPoint and ExitPoint cannot be the same");

        // Spawn and exit must be inside a walkable area
        if (!IsInWalkableArea(SpawnPoint))
            throw new ArgumentException($"Map '{Name}': SpawnPoint ({SpawnPoint.X}, {SpawnPoint.Y}) is not inside any WalkableArea");

        if (!IsInWalkableArea(ExitPoint))
            throw new ArgumentException($"Map '{Name}': ExitPoint ({ExitPoint.X}, {ExitPoint.Y}) is not inside any WalkableArea");

        // All walkable areas must be in bounds
        foreach (var area in WalkableAreas)
        {
            if (area.Left < 0 || area.Right > Columns || area.Top < 0 || area.Bottom > Rows)
                throw new ArgumentException(
                    $"Map '{Name}': WalkableArea ({area.X},{area.Y},{area.Width},{area.Height}) is out of bounds");
        }
    }

    /// <summary>
    /// Check if a point falls inside any walkable area rectangle.
    /// </summary>
    public bool IsInWalkableArea(Point p)
    {
        foreach (var area in WalkableAreas)
        {
            if (p.X >= area.Left && p.X < area.Right &&
                p.Y >= area.Top && p.Y < area.Bottom)
                return true;
        }
        return false;
    }
};

/// <summary>
/// Static repository of predefined maps. Think of this like TowerData.GetStats().
/// </summary>
public static class MapDataRepository
{
    /// <summary>
    /// Gets a predefined map by ID. Throws if not found.
    /// </summary>
    public static MapData GetMap(string mapId) => mapId switch
    {
        "classic_s" => CreateClassicSPath(),
        "straight" => CreateStraightPath(),
        "maze_test" => CreateMazeTestPath(),
        _ => throw new ArgumentException($"Unknown map ID: {mapId}")
    };

    /// <summary>
    /// Lists all available map IDs.
    /// </summary>
    public static List<string> GetAvailableMaps() => new()
    {
        "classic_s",
        "straight",
        "maze_test"
    };

    /// <summary>
    /// Classic S-path: corridors forming a serpentine route.
    /// </summary>
    private static MapData CreateClassicSPath()
    {
        // S-path defined as corridor rectangles:
        //   Row 2: x=0-17   (left to right)
        //   Col 17: y=2-6   (down)
        //   Row 6: x=2-17   (right to left)
        //   Col 2: y=6-10   (down)
        //   Row 10: x=2-17  (left to right)
        //   Col 17: y=10-13 (down)
        //   Row 13: x=0-17  (right to left)
        var walkableAreas = new List<Rectangle>
        {
            new Rectangle(0, 2, 18, 1),   // Row 2: x=0 to x=17
            new Rectangle(17, 2, 1, 5),   // Col 17: y=2 to y=6
            new Rectangle(2, 6, 16, 1),   // Row 6: x=2 to x=17
            new Rectangle(2, 6, 1, 5),    // Col 2: y=6 to y=10
            new Rectangle(2, 10, 16, 1),  // Row 10: x=2 to x=17
            new Rectangle(17, 10, 1, 4),  // Col 17: y=10 to y=13
            new Rectangle(0, 13, 18, 1),  // Row 13: x=0 to x=17
        };

        var mapData = new MapData(
            Name: "Classic S-Path",
            Id: "classic_s",
            SpawnPoint: new Point(0, 2),
            ExitPoint: new Point(0, 13),
            WalkableAreas: walkableAreas
        );

        mapData.Validate();
        return mapData;
    }

    /// <summary>
    /// Simple straight horizontal path for testing.
    /// </summary>
    private static MapData CreateStraightPath()
    {
        var walkableAreas = new List<Rectangle>
        {
            new Rectangle(0, 7, 20, 1)  // Full width horizontal line on row 7
        };

        var mapData = new MapData(
            Name: "Straight Path",
            Id: "straight",
            SpawnPoint: new Point(0, 7),
            ExitPoint: new Point(19, 7),
            WalkableAreas: walkableAreas
        );

        mapData.Validate();
        return mapData;
    }

    /// <summary>
    /// Maze test map: straight path through a large open buildable area.
    /// Players build towers anywhere in the area to force enemies into detours.
    /// </summary>
    private static MapData CreateMazeTestPath()
    {
        var walkableAreas = new List<Rectangle>
        {
            new Rectangle(0, 7, 20, 1),     // Horizontal path across row 7 (full width for spawn/exit)
            new Rectangle(2, 1, 15, 13),     // Large buildable area (x=2-16, y=1-13)
        };

        var mapData = new MapData(
            Name: "Maze Test",
            Id: "maze_test",
            SpawnPoint: new Point(0, 7),
            ExitPoint: new Point(19, 7),
            WalkableAreas: walkableAreas
        );

        mapData.Validate();
        return mapData;
    }
}
