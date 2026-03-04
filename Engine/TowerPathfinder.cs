using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using StarterTD.Entities;

namespace StarterTD.Engine;

/// <summary>
/// Dijkstra pathfinding for tower movement. Reuses the core Pathfinder algorithm
/// with tower-specific tile weights:
///   - Path tiles: cost 1 (preferred)
///   - HighGround tiles: cost 2 (walkable but less preferred)
///   - Tiles with stationary towers: cost 10 (passable but heavily penalized)
///   - Rock tiles: impassable
/// Enemies are completely ignored (dynamic entities don't block towers).
/// </summary>
public static class TowerPathfinder
{
    private const int PathCost = 1;
    private const int HighGroundCost = 2;
    private const int OccupiedTowerCost = 10;

    /// <summary>
    /// Compute a walking path for a tower from start to end on the given map.
    /// Returns a Queue of top-left footprint points representing the route, or null if no path exists.
    /// The queue includes the start point as the first element.
    /// </summary>
    public static Queue<Point>? FindPath(
        Point start,
        Point end,
        Point footprintSize,
        Map map,
        Tower? movingTower = null
    )
    {
        if (!map.IsFootprintInBounds(start, footprintSize))
            return null;

        if (!map.IsFootprintInBounds(end, footprintSize))
            return null;

        var heatMap = Pathfinder.ComputeHeatMap(
            end,
            map.Columns,
            map.Rows,
            p => GetTowerMovementCost(p, map, footprintSize, movingTower)
        );

        var pathList = Pathfinder.ExtractPath(start, heatMap, map.Columns, map.Rows);
        if (pathList == null)
            return null;

        return new Queue<Point>(pathList);
    }

    /// <summary>
    /// Debug-only self-test. Builds a small map, runs FindPath, and asserts correctness.
    /// Call from Game1.Initialize() or a debug button to verify the algorithm works.
    /// Writes results to the Debug output window (View → Output in IDE).
    /// </summary>
    [Conditional("DEBUG")]
    public static void DebugValidate()
    {
        // Build a 5x5 map: all Path except row 2 is Rock (wall) with a gap at (2,2)
        //
        //  . . . . .    (row 0 — Path)
        //  . . . . .    (row 1 — Path)
        //  # # . # #    (row 2 — Rock except col 2)
        //  . . . . .    (row 3 — Path)
        //  . . . . .    (row 4 — Path)
        //
        // Path from (0,0) to (4,4) must route through the gap at (2,2).

        var walkable = new List<Rectangle> { new Rectangle(0, 0, 5, 5) };
        var rocks = new List<Rectangle>
        {
            new Rectangle(0, 2, 2, 1), // (0,2) and (1,2)
            new Rectangle(3, 2, 2, 1), // (3,2) and (4,2)
        };

        var mapData = new MapData(
            Name: "Debug",
            Id: "debug",
            SpawnPoints: new Dictionary<string, Point> { ["spawn"] = new Point(0, 0) },
            ExitPoints: new Dictionary<string, Point> { ["exit"] = new Point(4, 4) },
            WalkableAreas: walkable,
            Columns: 5,
            Rows: 5,
            RockAreas: rocks
        );

        var map = new Map(mapData);

        // Test 1: basic path exists
        var path = FindPath(new Point(0, 0), new Point(4, 4), new Point(1, 1), map);
        Debug.Assert(path != null, "TowerPathfinder: path should exist from (0,0) to (4,4)");
        Debug.Assert(path.Count > 0, "TowerPathfinder: path should not be empty");

        // Test 2: path must pass through the gap at (2,2)
        Debug.Assert(
            path.Contains(new Point(2, 2)),
            "TowerPathfinder: path should route through gap at (2,2)"
        );

        // Test 3: path starts at start and ends at destination
        var asList = new List<Point>(path);
        Debug.Assert(asList[0] == new Point(0, 0), "TowerPathfinder: path should start at (0,0)");
        Debug.Assert(asList[^1] == new Point(4, 4), "TowerPathfinder: path should end at (4,4)");

        // Test 4: unreachable destination returns null
        var rocksOnly = new List<Rectangle> { new Rectangle(0, 2, 5, 1) };
        var blockedData = new MapData(
            Name: "Blocked",
            Id: "blocked",
            SpawnPoints: new Dictionary<string, Point> { ["spawn"] = new Point(0, 0) },
            ExitPoints: new Dictionary<string, Point> { ["exit"] = new Point(4, 4) },
            WalkableAreas: walkable,
            Columns: 5,
            Rows: 5,
            RockAreas: rocksOnly
        );
        var blockedMap = new Map(blockedData);
        var blockedPath = FindPath(new Point(0, 0), new Point(4, 4), new Point(1, 1), blockedMap);
        Debug.Assert(blockedPath == null, "TowerPathfinder: fully blocked path should return null");

        Debug.WriteLine("TowerPathfinder.DebugValidate: All tests passed.");
    }

    /// <summary>
    /// Tower-specific movement cost for a footprint's top-left tile.
    /// Rock or out-of-bounds in any footprint tile = impassable.
    /// Occupied tiles are penalized but still passable, except reservations by other towers.
    /// </summary>
    private static int GetTowerMovementCost(
        Point topLeft,
        Map map,
        Point footprintSize,
        Tower? movingTower
    )
    {
        if (!map.IsFootprintInBounds(topLeft, footprintSize))
            return int.MaxValue;

        int worstBaseCost = 0;
        bool hasBlockingTower = false;

        for (int y = 0; y < footprintSize.Y; y++)
        {
            for (int x = 0; x < footprintSize.X; x++)
            {
                var tile = map.Tiles[topLeft.X + x, topLeft.Y + y];
                int baseCost = tile.Type switch
                {
                    TileType.Path => PathCost,
                    TileType.HighGround => HighGroundCost,
                    TileType.Rock => int.MaxValue,
                    _ => int.MaxValue,
                };

                if (baseCost == int.MaxValue)
                    return int.MaxValue;

                if (tile.ReservedByTower != null && tile.ReservedByTower != movingTower)
                    return int.MaxValue;

                if (tile.OccupyingTower != null && tile.OccupyingTower != movingTower)
                    hasBlockingTower = true;

                if (baseCost > worstBaseCost)
                    worstBaseCost = baseCost;
            }
        }

        if (hasBlockingTower)
            return OccupiedTowerCost;

        return worstBaseCost;
    }
}
