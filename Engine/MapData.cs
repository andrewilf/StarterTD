using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Represents a rectangular zone where players can build towers to create mazes.
/// Enemies follow the predefined path through these zones, but players can
/// place towers within zones to force dynamic pathfinding (future feature).
/// </summary>
public record MazeZone(
    Rectangle Bounds,         // Grid-based rectangle (e.g., X=5, Y=3, Width=6, Height=4)
    string Name = "Maze Zone" // Optional name for debugging/UI
)
{
    /// <summary>
    /// Check if a grid point is inside this zone's bounds.
    /// Think of it like Python's `point in rect` — checks X in [Left, Right) and Y in [Top, Bottom).
    /// </summary>
    public bool ContainsPoint(Point p) =>
        p.X >= Bounds.Left && p.X < Bounds.Right &&
        p.Y >= Bounds.Top && p.Y < Bounds.Bottom;
};

/// <summary>
/// Data-driven map configuration. Defines path, maze zones, and metadata.
/// Similar to TowerData pattern - can be hardcoded or loaded from JSON.
/// </summary>
public record MapData(
    string Name,                  // Map display name (e.g., "Classic S-Path", "Spiral")
    string Id,                    // Unique identifier (e.g., "classic_s", "spiral_01")
    List<Point> PathPoints,       // Ordered waypoints (grid coordinates)
    List<MazeZone> MazeZones,     // Buildable zones within/around path
    int Columns = 20,             // Grid width (defaults to GameSettings)
    int Rows = 15                 // Grid height (defaults to GameSettings)
)
{
    /// <summary>
    /// Validates map data. Throws ArgumentException if invalid.
    /// Call this in MapData constructor or factory methods.
    /// </summary>
    public void Validate()
    {
        // 1. Path must have at least 2 points
        if (PathPoints.Count < 2)
            throw new ArgumentException($"Map '{Name}': Path must have at least 2 points");

        // 2. All path points must be in bounds
        foreach (var point in PathPoints)
        {
            if (point.X < 0 || point.X >= Columns || point.Y < 0 || point.Y >= Rows)
                throw new ArgumentException(
                    $"Map '{Name}': Path point ({point.X}, {point.Y}) is out of bounds");
        }

        // 3. Path must be contiguous (adjacent tiles only, no gaps)
        for (int i = 0; i < PathPoints.Count - 1; i++)
        {
            var current = PathPoints[i];
            var next = PathPoints[i + 1];
            int dx = Math.Abs(next.X - current.X);
            int dy = Math.Abs(next.Y - current.Y);

            // Must be adjacent or same (orthogonal only - no diagonals)
            // Allow same point (dx=0, dy=0) for turning corners
            bool isAdjacent = (dx == 1 && dy == 0) || (dx == 0 && dy == 1) || (dx == 0 && dy == 0);
            if (!isAdjacent)
                throw new ArgumentException(
                    $"Map '{Name}': Gap in path between ({current.X},{current.Y}) and ({next.X},{next.Y})");
        }

        // 4. Maze zones must be in bounds
        foreach (var zone in MazeZones)
        {
            if (zone.Bounds.Left < 0 || zone.Bounds.Right > Columns ||
                zone.Bounds.Top < 0 || zone.Bounds.Bottom > Rows)
                throw new ArgumentException(
                    $"Map '{Name}': Maze zone '{zone.Name}' is out of bounds");
        }
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
        "spiral" => CreateSpiralPath(),
        _ => throw new ArgumentException($"Unknown map ID: {mapId}")
    };

    /// <summary>
    /// Lists all available map IDs.
    /// </summary>
    public static List<string> GetAvailableMaps() => new()
    {
        "classic_s",
        "straight",
        "spiral"
    };

    /// <summary>
    /// The original hardcoded S-path for backward compatibility.
    /// No maze zones - classic tower defense map.
    /// </summary>
    private static MapData CreateClassicSPath()
    {
        var path = new List<Point>();

        // Segment 1: Left to right on row 2
        for (int x = 0; x < 18; x++)
            path.Add(new Point(x, 2));

        // Segment 2: Down from row 2 to row 6 at column 17
        for (int y = 3; y <= 6; y++)
            path.Add(new Point(17, y));

        // Segment 3: Right to left on row 6
        for (int x = 17; x >= 2; x--)
            path.Add(new Point(x, 6));

        // Segment 4: Down from row 6 to row 10 at column 2
        for (int y = 7; y <= 10; y++)
            path.Add(new Point(2, y));

        // Segment 5: Left to right on row 10
        for (int x = 2; x < 18; x++)
            path.Add(new Point(x, 10));

        // Segment 6: Down from row 10 to row 13 at column 17
        for (int y = 11; y <= 13; y++)
            path.Add(new Point(17, y));

        // Segment 7: Right to left on row 13 to exit
        for (int x = 17; x >= 0; x--)
            path.Add(new Point(x, 13));

        // No maze zones in classic map (can build anywhere except path)
        var mapData = new MapData(
            Name: "Classic S-Path",
            Id: "classic_s",
            PathPoints: path,
            MazeZones: new List<MazeZone>()
        );

        mapData.Validate();
        return mapData;
    }

    /// <summary>
    /// Simple straight horizontal path for testing.
    /// No maze zones - useful for debugging tower mechanics.
    /// </summary>
    private static MapData CreateStraightPath()
    {
        var path = new List<Point>();

        // Simple horizontal line across the middle
        for (int x = 0; x < 20; x++)
            path.Add(new Point(x, 7));

        var mapData = new MapData(
            Name: "Straight Path",
            Id: "straight",
            PathPoints: path,
            MazeZones: new List<MazeZone>()
        );

        mapData.Validate();
        return mapData;
    }

    /// <summary>
    /// Mazing test map: straight path through a large open maze zone.
    /// The path goes left-to-right through the center. Players build towers
    /// within the zone to force enemies into longer detours.
    /// </summary>
    private static MapData CreateSpiralPath()
    {
        var path = new List<Point>();

        // Simple horizontal path across the middle (row 7)
        for (int x = 0; x < 19; x++)
            path.Add(new Point(x, 7));

        // One large maze zone covering most of the map
        var mazeZones = new List<MazeZone>
        {
            new MazeZone(
                Bounds: new Rectangle(2, 1, 15, 13),
                Name: "Main Maze"
            )
        };

        var mapData = new MapData(
            Name: "Maze Test",
            Id: "spiral",
            PathPoints: path,
            MazeZones: mazeZones
        );

        mapData.Validate();
        return mapData;
    }
}
