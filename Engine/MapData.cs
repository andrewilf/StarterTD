using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Data-driven map configuration. Terrain layout comes from either a TileGrid (Tiled .tmx maps)
/// or legacy WalkableAreas/RockAreas rectangles. Dijkstra computes the enemy route at runtime.
///
/// Python/TS analogy: Like a config dict { spawn, exit, tile_grid, ... }
/// that a Map class reads to build a 2D grid.
/// </summary>
public record MapData(
    string Name, // Map display name
    string Id, // Unique identifier
    Point SpawnPoint, // Where enemies enter the map
    Point ExitPoint, // Where enemies leave the map
    List<Rectangle> WalkableAreas, // Rectangles of Path tiles — unused when TileGrid is set
    int Columns = 20, // Grid width
    int Rows = 15, // Grid height
    List<Rectangle>? RockAreas = null // Rectangles of Rock tiles — unused when TileGrid is set
)
{
    // Kept outside the primary constructor to avoid S2368 (multidimensional array in public ctor).
    // Set via object initializer: new MapData(...) { TileGrid = grid }
    public TileType[,]? TileGrid { get; init; } = null;

    /// <summary>
    /// Validates map data. Throws ArgumentException if invalid.
    /// </summary>
    public void Validate()
    {
        if (TileGrid != null && (TileGrid.GetLength(0) != Columns || TileGrid.GetLength(1) != Rows))
            throw new ArgumentException(
                $"Map '{Name}': TileGrid dimensions ({TileGrid.GetLength(0)}x{TileGrid.GetLength(1)}) "
                    + $"do not match Columns/Rows ({Columns}x{Rows})"
            );

        if (SpawnPoint.X < 0 || SpawnPoint.X >= Columns || SpawnPoint.Y < 0 || SpawnPoint.Y >= Rows)
            throw new ArgumentException(
                $"Map '{Name}': SpawnPoint ({SpawnPoint.X}, {SpawnPoint.Y}) is out of bounds"
            );

        if (ExitPoint.X < 0 || ExitPoint.X >= Columns || ExitPoint.Y < 0 || ExitPoint.Y >= Rows)
            throw new ArgumentException(
                $"Map '{Name}': ExitPoint ({ExitPoint.X}, {ExitPoint.Y}) is out of bounds"
            );

        if (SpawnPoint == ExitPoint)
            throw new ArgumentException(
                $"Map '{Name}': SpawnPoint and ExitPoint cannot be the same"
            );

        // Spawn and exit must be inside a walkable area
        if (!IsInWalkableArea(SpawnPoint))
            throw new ArgumentException(
                $"Map '{Name}': SpawnPoint ({SpawnPoint.X}, {SpawnPoint.Y}) is not inside any WalkableArea"
            );

        if (!IsInWalkableArea(ExitPoint))
            throw new ArgumentException(
                $"Map '{Name}': ExitPoint ({ExitPoint.X}, {ExitPoint.Y}) is not inside any WalkableArea"
            );

        // All walkable areas must be in bounds
        foreach (var area in WalkableAreas)
        {
            if (area.Left < 0 || area.Right > Columns || area.Top < 0 || area.Bottom > Rows)
                throw new ArgumentException(
                    $"Map '{Name}': WalkableArea ({area.X},{area.Y},{area.Width},{area.Height}) is out of bounds"
                );
        }

        // All rock areas must be in bounds (if present)
        if (RockAreas != null)
        {
            foreach (var area in RockAreas)
            {
                if (area.Left < 0 || area.Right > Columns || area.Top < 0 || area.Bottom > Rows)
                    throw new ArgumentException(
                        $"Map '{Name}': RockArea ({area.X},{area.Y},{area.Width},{area.Height}) is out of bounds"
                    );
            }
        }
    }

    /// <summary>
    /// Check if a point is a walkable (Path) tile.
    /// For Tiled maps, reads TileGrid directly. For legacy rectangle maps, checks WalkableAreas.
    /// </summary>
    public bool IsInWalkableArea(Point p)
    {
        if (TileGrid != null)
            return TileGrid[p.X, p.Y] == TileType.Path;

        foreach (var area in WalkableAreas)
        {
            if (p.X >= area.Left && p.X < area.Right && p.Y >= area.Top && p.Y < area.Bottom)
                return true;
        }
        return false;
    }
};

/// <summary>
/// Loads maps by ID from Tiled .tmx files in Content/Maps/.
/// Add a new map by creating a .tmx file — no C# changes required.
/// </summary>
public static class MapDataRepository
{
    /// <summary>
    /// Loads a map from its .tmx file. Throws if the file is not found or is invalid.
    /// </summary>
    public static MapData GetMap(string mapId) =>
        TmxLoader.TryLoad(mapId)
        ?? throw new ArgumentException(
            $"Map file not found for ID '{mapId}'. " + $"Expected: Content/Maps/{mapId}.tmx"
        );

    /// <summary>
    /// Scans Content/Maps/ at runtime for .tmx files and returns their IDs (filename without extension), sorted.
    /// No C# changes needed when maps are added or removed — just run sync_maps.sh.
    /// </summary>
    public static List<string> GetAvailableMaps()
    {
        string mapsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Maps");

        if (!Directory.Exists(mapsDir))
            return [];

        return Directory
            .GetFiles(mapsDir, "*.tmx")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => id != null)
            .Select(id => id!)
            .OrderBy(id => id)
            .ToList();
    }
}
