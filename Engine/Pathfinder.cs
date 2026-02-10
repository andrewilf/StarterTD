using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Dijkstra-based pathfinding on a 2D grid. Static utility — no state, just pure functions.
///
/// Python/TS analogy: Like standalone functions `compute_heat_map(target, cost_fn)`
/// and `extract_path(start, heat_map)`. The Func parameter is like passing
/// `(point) => number` in TypeScript — lets the caller define movement costs
/// without the pathfinder knowing about tiles or towers.
/// </summary>
public static class Pathfinder
{
    /// <summary>
    /// Dijkstra flood fill from a target point outward across the entire grid.
    /// Returns an int[columns, rows] where each cell holds the minimum movement
    /// cost to reach the target. Unreachable cells remain int.MaxValue.
    ///
    /// Python analogy: Like running BFS with heapq so lower-cost tiles expand first.
    /// </summary>
    /// <param name="target">The destination (enemies' exit point). Gets cost 0.</param>
    /// <param name="columns">Grid width.</param>
    /// <param name="rows">Grid height.</param>
    /// <param name="movementCost">Returns the cost to enter a tile. int.MaxValue = impassable.</param>
    public static int[,] ComputeHeatMap(
        Point target,
        int columns,
        int rows,
        Func<Point, int> movementCost)
    {
        var cost = new int[columns, rows];
        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                cost[x, y] = int.MaxValue;

        cost[target.X, target.Y] = 0;

        Point[] directions = {
            new Point(0, -1),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(1, 0)
        };

        var queue = new PriorityQueue<Point, int>();
        queue.Enqueue(target, 0);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentCost = cost[current.X, current.Y];

            foreach (var dir in directions)
            {
                int nx = current.X + dir.X;
                int ny = current.Y + dir.Y;

                if (nx < 0 || nx >= columns || ny < 0 || ny >= rows)
                    continue;

                var neighbor = new Point(nx, ny);
                int tileCost = movementCost(neighbor);

                if (tileCost == int.MaxValue)
                    continue;

                // Overflow guard: currentCost + tileCost could wrap around
                long newCostLong = (long)currentCost + tileCost;
                if (newCostLong >= int.MaxValue)
                    continue;

                int newCost = (int)newCostLong;
                if (newCost < cost[nx, ny])
                {
                    cost[nx, ny] = newCost;
                    queue.Enqueue(neighbor, newCost);
                }
            }
        }

        return cost;
    }

    /// <summary>
    /// Extracts a path by following steepest descent (lowest-cost neighbor) on the heat map.
    /// Like gradient descent on a 2D surface — always step toward the smallest value until 0.
    /// </summary>
    /// <param name="start">Starting grid position (enemies' spawn point).</param>
    /// <param name="heatMap">Cost-to-target for every tile, from ComputeHeatMap.</param>
    /// <param name="columns">Grid width.</param>
    /// <param name="rows">Grid height.</param>
    /// <returns>Ordered path from start to target, or null if start is unreachable.</returns>
    public static List<Point>? ExtractPath(
        Point start,
        int[,] heatMap,
        int columns,
        int rows)
    {
        if (heatMap[start.X, start.Y] == int.MaxValue)
            return null;

        Point[] directions = {
            new Point(0, -1),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(1, 0)
        };

        var path = new List<Point> { start };
        var current = start;
        int maxSteps = columns * rows; // Safety cap to prevent infinite loops

        while (heatMap[current.X, current.Y] > 0 && maxSteps-- > 0)
        {
            Point best = current;
            int bestCost = heatMap[current.X, current.Y];

            foreach (var dir in directions)
            {
                int nx = current.X + dir.X;
                int ny = current.Y + dir.Y;

                if (nx < 0 || nx >= columns || ny < 0 || ny >= rows)
                    continue;

                if (heatMap[nx, ny] < bestCost)
                {
                    bestCost = heatMap[nx, ny];
                    best = new Point(nx, ny);
                }
            }

            // No progress — shouldn't happen if heat map is valid
            if (best == current)
                break;

            path.Add(best);
            current = best;
        }

        return path;
    }
}
