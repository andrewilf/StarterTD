using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// A* pathfinding on a 2D grid. Static utility — no state, just a pure function.
///
/// Python/TS analogy: Like a standalone `def find_path(start, end, is_walkable)`.
/// The Func&lt;Point, bool&gt; parameter is like passing `(point) => boolean` in TypeScript —
/// it lets the caller define what "walkable" means without the pathfinder knowing
/// about tiles, towers, or zones.
/// </summary>
public static class Pathfinder
{
    /// <summary>
    /// Finds the shortest path from start to goal on a grid using A*.
    /// Returns an ordered list of grid points (inclusive of start and goal),
    /// or null if no path exists.
    /// </summary>
    /// <param name="start">Starting grid position.</param>
    /// <param name="goal">Target grid position.</param>
    /// <param name="columns">Grid width.</param>
    /// <param name="rows">Grid height.</param>
    /// <param name="walkable">Function that returns true if a grid cell can be traversed.</param>
    public static List<Point>? FindPath(
        Point start,
        Point goal,
        int columns,
        int rows,
        Func<Point, bool> walkable)
    {
        // Edge case: start or goal is not walkable
        if (!walkable(start) || !walkable(goal))
            return null;

        // Edge case: already there
        if (start == goal)
            return new List<Point> { start };

        // The 4 cardinal directions (no diagonals — matches the grid's orthogonal movement)
        Point[] directions = {
            new Point(0, -1),  // up
            new Point(0, 1),   // down
            new Point(-1, 0),  // left
            new Point(1, 0)    // right
        };

        // gScore[point] = cheapest known cost from start to point
        var gScore = new Dictionary<Point, float>();
        gScore[start] = 0;

        // cameFrom[point] = the point we arrived from on the cheapest path
        var cameFrom = new Dictionary<Point, Point>();

        // Open set as a priority queue, ordered by fScore (gScore + heuristic)
        // PriorityQueue is built into .NET 6+ — dequeues lowest priority first
        var openSet = new PriorityQueue<Point, float>();
        openSet.Enqueue(start, Heuristic(start, goal));

        // Track what's in the open set (PriorityQueue doesn't have Contains)
        var inOpenSet = new HashSet<Point> { start };

        // Closed set — points we've already fully evaluated
        var closedSet = new HashSet<Point>();

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            inOpenSet.Remove(current);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            closedSet.Add(current);

            foreach (var dir in directions)
            {
                var neighbor = new Point(current.X + dir.X, current.Y + dir.Y);

                // Skip if out of bounds
                if (neighbor.X < 0 || neighbor.X >= columns ||
                    neighbor.Y < 0 || neighbor.Y >= rows)
                    continue;

                // Skip if already evaluated or not walkable
                if (closedSet.Contains(neighbor) || !walkable(neighbor))
                    continue;

                // All edges cost 1 (uniform grid)
                float tentativeG = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    float fScore = tentativeG + Heuristic(neighbor, goal);

                    if (!inOpenSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore);
                        inOpenSet.Add(neighbor);
                    }
                }
            }
        }

        // No path found — goal is unreachable
        return null;
    }

    /// <summary>
    /// Manhattan distance heuristic. Admissible for 4-directional grids
    /// (never overestimates), so A* is guaranteed to find the shortest path.
    /// </summary>
    private static float Heuristic(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Walks backward through cameFrom to build the path from start to goal.
    /// </summary>
    private static List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
    {
        var path = new List<Point> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
