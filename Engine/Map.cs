using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Engine;

/// <summary>
/// Represents what a tile on the grid can be.
/// Whether a tile is in a maze zone is determined by the Tile.MazeZone reference, not the type.
/// </summary>
public enum TileType
{
    /// <summary>Empty ground where towers can be placed.</summary>
    Buildable,
    /// <summary>Part of the enemy path. Buildable only if inside a maze zone.</summary>
    Path,
    /// <summary>A tower has been placed on this tile.</summary>
    Occupied
}

/// <summary>
/// A single tile in the grid.
/// This is a class (reference type) so we can mutate it in the grid array.
/// </summary>
public class Tile
{
    public TileType Type { get; set; }
    public Point GridPosition { get; }

    /// <summary>
    /// If this tile is part of a maze zone, reference it.
    /// Null for non-maze-zone tiles. This determines visual styling and build rules,
    /// NOT the TileType enum.
    /// </summary>
    public MazeZone? MazeZone { get; set; }

    public Tile(Point gridPosition, TileType type, MazeZone? mazeZone = null)
    {
        GridPosition = gridPosition;
        Type = type;
        MazeZone = mazeZone;
    }
}

/// <summary>
/// Cached info about how the fixed path passes through a maze zone.
/// Used to know which segment of PathPoints to replace with A* results.
///
/// Python analogy: Like a namedtuple storing (zone, entry_index, exit_index)
/// so you can do path[0:entry] + a_star_result + path[exit:] to build the full route.
/// </summary>
public class MazeZonePathInfo
{
    /// <summary>The maze zone this info describes.</summary>
    public MazeZone Zone { get; }

    /// <summary>Index into PathPoints where the path first enters this zone.</summary>
    public int EntryPathIndex { get; }

    /// <summary>Index into PathPoints where the path last exits this zone.</summary>
    public int ExitPathIndex { get; }

    /// <summary>The grid point where enemies enter the zone (= PathPoints[EntryPathIndex]).</summary>
    public Point EntryPoint { get; }

    /// <summary>The grid point where enemies exit the zone (= PathPoints[ExitPathIndex]).</summary>
    public Point ExitPoint { get; }

    public MazeZonePathInfo(MazeZone zone, int entryPathIndex, int exitPathIndex, List<Point> pathPoints)
    {
        Zone = zone;
        EntryPathIndex = entryPathIndex;
        ExitPathIndex = exitPathIndex;
        EntryPoint = pathPoints[entryPathIndex];
        ExitPoint = pathPoints[exitPathIndex];
    }
}

/// <summary>
/// The game map: a 2D grid of tiles with data-driven paths and maze zones.
/// Think of this like a 2D array board in a board-game simulation.
/// </summary>
public class Map
{
    public Tile[,] Tiles { get; }
    public int Columns { get; }
    public int Rows { get; }

    /// <summary>
    /// The original fixed path from MapData. Never changes.
    /// </summary>
    public List<Point> PathPoints { get; }

    /// <summary>
    /// The active path enemies follow. Starts as PathPoints, but gets recomposed
    /// when towers are placed in maze zones (fixed segments + A* segments stitched together).
    /// </summary>
    public List<Point> ActivePath { get; private set; }

    /// <summary>
    /// Cached info about how the path intersects each maze zone.
    /// Sorted by EntryPathIndex so zones are processed in path order.
    /// </summary>
    public List<MazeZonePathInfo> MazeZonePathInfos { get; private set; } = new();

    /// <summary>
    /// The map data used to create this map (null for legacy default).
    /// </summary>
    public MapData? MapData { get; }

    /// <summary>
    /// BACKWARD COMPATIBLE: Default constructor uses classic S-path.
    /// </summary>
    public Map() : this(MapDataRepository.GetMap("classic_s"))
    {
    }

    /// <summary>
    /// Primary constructor accepting MapData.
    /// </summary>
    public Map(MapData mapData)
    {
        MapData = mapData;
        mapData.Validate(); // Fail fast if invalid

        Columns = mapData.Columns;
        Rows = mapData.Rows;
        Tiles = new Tile[Columns, Rows];
        PathPoints = mapData.PathPoints;
        ActivePath = new List<Point>(PathPoints); // Start as a copy of PathPoints

        InitializeTiles();
        AnalyzeMazeZones();
    }

    /// <summary>
    /// Static factory for map ID.
    /// </summary>
    public static Map FromId(string mapId)
    {
        return new Map(MapDataRepository.GetMap(mapId));
    }

    /// <summary>
    /// Initialize tiles in 2 phases:
    /// 1. All tiles start as Buildable (with MazeZone reference set if inside a zone)
    /// 2. Path tiles are marked as Path (keep MazeZone reference)
    /// </summary>
    private void InitializeTiles()
    {
        // Phase 1: All tiles start as Buildable
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                Tiles[x, y] = new Tile(new Point(x, y), TileType.Buildable);
            }
        }

        // Tag maze zone membership (sets reference only, TileType stays Buildable)
        if (MapData != null)
        {
            foreach (var zone in MapData.MazeZones)
            {
                for (int x = zone.Bounds.Left; x < zone.Bounds.Right; x++)
                {
                    for (int y = zone.Bounds.Top; y < zone.Bounds.Bottom; y++)
                    {
                        if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                        {
                            Tiles[x, y].MazeZone = zone;
                        }
                    }
                }
            }
        }

        // Phase 2: Mark path tiles as Path (MazeZone reference is preserved)
        foreach (var point in PathPoints)
        {
            Tiles[point.X, point.Y].Type = TileType.Path;
        }
    }

    /// <summary>
    /// Convert a grid position to world-space center position.
    /// </summary>
    public static Vector2 GridToWorld(Point gridPos)
    {
        return new Vector2(
            gridPos.X * GameSettings.TileSize + GameSettings.TileSize / 2f,
            gridPos.Y * GameSettings.TileSize + GameSettings.TileSize / 2f);
    }

    /// <summary>
    /// Convert a world-space position to a grid position.
    /// </summary>
    public static Point WorldToGrid(Vector2 worldPos)
    {
        return new Point(
            (int)(worldPos.X / GameSettings.TileSize),
            (int)(worldPos.Y / GameSettings.TileSize));
    }

    /// <summary>
    /// Analyze which maze zones the path passes through and cache entry/exit info.
    /// Called once during construction.
    /// </summary>
    private void AnalyzeMazeZones()
    {
        if (MapData == null) return;

        foreach (var zone in MapData.MazeZones)
        {
            int entryIndex = -1;
            int exitIndex = -1;

            for (int i = 0; i < PathPoints.Count; i++)
            {
                if (zone.ContainsPoint(PathPoints[i]))
                {
                    if (entryIndex == -1) entryIndex = i;
                    exitIndex = i;
                }
            }

            if (entryIndex != -1 && exitIndex != -1 && entryIndex != exitIndex)
            {
                MazeZonePathInfos.Add(new MazeZonePathInfo(zone, entryIndex, exitIndex, PathPoints));
            }
        }

        MazeZonePathInfos.Sort((a, b) => a.EntryPathIndex.CompareTo(b.EntryPathIndex));
    }

    /// <summary>
    /// Recompute ActivePath by stitching fixed segments with A* segments through maze zones.
    /// Returns true if a valid path exists, false if a zone is fully blocked.
    /// </summary>
    public bool RecomputeActivePath()
    {
        if (MazeZonePathInfos.Count == 0)
        {
            ActivePath = new List<Point>(PathPoints);
            return true;
        }

        var newPath = new List<Point>();
        int currentIndex = 0;

        foreach (var zoneInfo in MazeZonePathInfos)
        {
            for (int i = currentIndex; i < zoneInfo.EntryPathIndex; i++)
                newPath.Add(PathPoints[i]);

            bool hasTowersInZone = HasTowersInZone(zoneInfo.Zone);

            if (hasTowersInZone)
            {
                var aStarPath = Pathfinder.FindPath(
                    zoneInfo.EntryPoint,
                    zoneInfo.ExitPoint,
                    Columns,
                    Rows,
                    p => IsWalkableForMaze(p, zoneInfo.Zone));

                if (aStarPath == null)
                    return false;

                newPath.AddRange(aStarPath);
            }
            else
            {
                for (int i = zoneInfo.EntryPathIndex; i <= zoneInfo.ExitPathIndex; i++)
                    newPath.Add(PathPoints[i]);
            }

            currentIndex = zoneInfo.ExitPathIndex + 1;
        }

        for (int i = currentIndex; i < PathPoints.Count; i++)
            newPath.Add(PathPoints[i]);

        ActivePath = newPath;
        return true;
    }

    private bool HasTowersInZone(MazeZone zone)
    {
        for (int x = zone.Bounds.Left; x < zone.Bounds.Right; x++)
        {
            for (int y = zone.Bounds.Top; y < zone.Bounds.Bottom; y++)
            {
                if (x >= 0 && x < Columns && y >= 0 && y < Rows &&
                    Tiles[x, y].Type == TileType.Occupied)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if a grid point is walkable for A* pathfinding within a maze zone.
    /// Walkable = (inside zone AND not Occupied) OR (Path tile for entry/exit).
    /// </summary>
    private bool IsWalkableForMaze(Point p, MazeZone zone)
    {
        if (p.X < 0 || p.X >= Columns || p.Y < 0 || p.Y >= Rows)
            return false;

        var type = Tiles[p.X, p.Y].Type;

        // Within the zone: Buildable and Path tiles are walkable, Occupied is not
        if (zone.ContainsPoint(p))
            return type == TileType.Buildable || type == TileType.Path;

        // Path tiles just outside the zone boundary are walkable (for entry/exit)
        return type == TileType.Path;
    }

    /// <summary>
    /// Check if a grid position is valid and buildable.
    /// Buildable tiles always. Path tiles only inside maze zones.
    /// </summary>
    public bool CanBuild(Point gridPos)
    {
        if (gridPos.X < 0 || gridPos.X >= Columns || gridPos.Y < 0 || gridPos.Y >= Rows)
            return false;

        var tile = Tiles[gridPos.X, gridPos.Y];

        if (tile.Type == TileType.Buildable)
            return true;

        // Path tiles inside a maze zone are buildable (this IS mazing)
        if (tile.Type == TileType.Path && tile.MazeZone != null)
            return true;

        return false;
    }

    /// <summary>
    /// Draw the grid with colored tiles.
    /// Maze zone membership is shown via the MazeZone reference, not TileType.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                var rect = new Rectangle(
                    x * GameSettings.TileSize,
                    y * GameSettings.TileSize,
                    GameSettings.TileSize,
                    GameSettings.TileSize);

                var tile = Tiles[x, y];

                // Maze zone tiles get bright green regardless of Path vs Buildable
                Color tileColor;
                if (tile.MazeZone != null && tile.Type != TileType.Occupied)
                    tileColor = new Color(50, 180, 50);
                else
                    tileColor = tile.Type switch
                    {
                        TileType.Buildable => new Color(34, 139, 34),
                        TileType.Path => new Color(194, 178, 128),
                        TileType.Occupied => new Color(100, 100, 100),
                        _ => Color.Black
                    };

                TextureManager.DrawRect(spriteBatch, rect, tileColor);
                TextureManager.DrawRectOutline(spriteBatch, rect, new Color(0, 0, 0, 60), 1);

                // Yellow border for maze zone tiles
                if (tile.MazeZone != null && tile.Type != TileType.Occupied)
                {
                    TextureManager.DrawRectOutline(spriteBatch, rect,
                        new Color(255, 255, 100, 100), 2);
                }
            }
        }

        DrawMazePathOverlay(spriteBatch);
    }

    /// <summary>
    /// Debug visualization: draws the full ActivePath as a connected line.
    /// </summary>
    private void DrawMazePathOverlay(SpriteBatch spriteBatch)
    {
        if (ActivePath.Count == 0) return;

        const int dotSize = 8;
        int halfDot = dotSize / 2;
        var pathColor = new Color(100, 180, 255, 160);

        for (int i = 0; i < ActivePath.Count; i++)
        {
            var point = ActivePath[i];
            if (point.X < 0 || point.X >= Columns || point.Y < 0 || point.Y >= Rows)
                continue;

            int centerX = point.X * GameSettings.TileSize + GameSettings.TileSize / 2;
            int centerY = point.Y * GameSettings.TileSize + GameSettings.TileSize / 2;

            TextureManager.DrawRect(spriteBatch,
                new Rectangle(centerX - halfDot, centerY - halfDot, dotSize, dotSize),
                pathColor);

            if (i < ActivePath.Count - 1)
            {
                var next = ActivePath[i + 1];
                int nextCenterX = next.X * GameSettings.TileSize + GameSettings.TileSize / 2;
                int nextCenterY = next.Y * GameSettings.TileSize + GameSettings.TileSize / 2;

                if (next.Y == point.Y && next.X != point.X)
                {
                    int minX = Math.Min(centerX, nextCenterX);
                    TextureManager.DrawRect(spriteBatch,
                        new Rectangle(minX, centerY - halfDot / 2, Math.Abs(nextCenterX - centerX), halfDot),
                        pathColor);
                }
                else if (next.X == point.X && next.Y != point.Y)
                {
                    int minY = Math.Min(centerY, nextCenterY);
                    TextureManager.DrawRect(spriteBatch,
                        new Rectangle(centerX - halfDot / 2, minY, halfDot, Math.Abs(nextCenterY - centerY)),
                        pathColor);
                }
            }
        }
    }

    /// <summary>
    /// Check if placing a tower here would block all paths through its maze zone.
    /// </summary>
    public bool WouldBlockPath(Point gridPos)
    {
        if (gridPos.X < 0 || gridPos.X >= Columns || gridPos.Y < 0 || gridPos.Y >= Rows)
            return false;

        var tile = Tiles[gridPos.X, gridPos.Y];
        if (tile.MazeZone == null || tile.Type == TileType.Occupied)
            return false;

        var originalType = tile.Type;
        tile.Type = TileType.Occupied;
        bool pathExists = RecomputeActivePath();
        tile.Type = originalType;
        RecomputeActivePath();

        return !pathExists;
    }
}
