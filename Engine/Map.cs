using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Entities;

namespace StarterTD.Engine;

/// <summary>
/// A single tile in the grid.
/// This is a class (reference type) so we can mutate it in the grid array.
/// </summary>
public class Tile
{
    public TileType Type { get; set; }

    public Point GridPosition { get; }

    /// <summary>The tower occupying this tile, or null if unoccupied.</summary>
    public Tower? OccupyingTower { get; set; }

    /// <summary>Tower committed to moving here but not yet arrived. Blocks placement and movement targeting.</summary>
    public Tower? ReservedByTower { get; set; }

    /// <summary>
    /// Movement cost for pathfinding. Towers add a cost penalty based on type.
    /// If a tower occupies this tile, use the higher of the tile's base cost and the tower's cost.
    /// Like a Python @property — recomputes from current state, no stored field.
    /// </summary>
    public int MovementCost
    {
        get
        {
            int baseCost = TileData.GetStats(Type).MovementCost;

            if (OccupyingTower != null)
            {
                int towerCost = TowerData.GetStats(OccupyingTower.TowerType).MovementCost;
                return Math.Max(baseCost, towerCost);
            }

            return baseCost;
        }
    }

    public Tile(Point gridPosition, TileType type)
    {
        GridPosition = gridPosition;
        Type = type;
    }
}

/// <summary>
/// The game map: a 2D grid of tiles with Dijkstra-based pathfinding.
/// Terrain is loaded from a Tiled .tmx TileGrid; enemies find their own route at runtime.
///
/// Multi-lane support: each exit has its own heatmap. Each spawn is paired with the exit
/// that shares the same name suffix (e.g. "spawn_a" → "exit_a"). If no suffix match exists,
/// the spawn falls back to the first exit.
/// </summary>
public class Map
{
    public Tile[,] Tiles { get; }
    public int Columns { get; }
    public int Rows { get; }

    /// <summary>The map data used to create this map.</summary>
    public MapData MapData { get; }

    /// <summary>
    /// Pre-computed paths per spawn point. Key = spawn name (e.g. "spawn", "spawn_a").
    /// Recomputed whenever towers change.
    /// </summary>
    public Dictionary<string, List<Point>> ActivePaths { get; private set; }

    /// <summary>
    /// Convenience accessor for single-spawn-point maps and legacy callers.
    /// Returns the first path, or an empty list if none computed yet.
    /// </summary>
    public List<Point> ActivePath => ActivePaths.Values.FirstOrDefault() ?? new List<Point>();

    // One heatmap per exit. Key = exit name. Filled by RecomputeAllPaths().
    private readonly Dictionary<string, int[,]> _heatMaps = new();

    /// <summary>
    /// Primary constructor accepting MapData.
    /// </summary>
    public Map(MapData mapData)
    {
        MapData = mapData;
        mapData.Validate();

        Columns = mapData.Columns;
        Rows = mapData.Rows;
        Tiles = new Tile[Columns, Rows];
        ActivePaths = new Dictionary<string, List<Point>>();

        InitializeTiles();
        RecomputeAllPaths();
    }

    /// <summary>
    /// Static factory for map ID.
    /// </summary>
    public static Map FromId(string mapId)
    {
        return new Map(MapDataRepository.GetMap(mapId));
    }

    /// <summary>
    /// Initialize tiles from MapData. Tiled maps use a pre-built TileGrid; legacy maps use rectangle fill.
    /// </summary>
    private void InitializeTiles()
    {
        // Fast path: Tiled .tmx map already has a complete per-tile type grid
        if (MapData.TileGrid != null)
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    Tiles[x, y] = new Tile(new Point(x, y), MapData.TileGrid[x, y]);
                }
            }
            return;
        }

        // Legacy path: fill by rectangle regions
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                Tiles[x, y] = new Tile(new Point(x, y), TileType.HighGround);
            }
        }

        foreach (var area in MapData.WalkableAreas)
        {
            for (int x = area.Left; x < area.Right; x++)
            {
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                    {
                        Tiles[x, y].Type = TileType.Path;
                    }
                }
            }
        }

        // Mark rock areas as Rock (overrides previous types)
        if (MapData.RockAreas != null)
        {
            foreach (var area in MapData.RockAreas)
            {
                for (int x = area.Left; x < area.Right; x++)
                {
                    for (int y = area.Top; y < area.Bottom; y++)
                    {
                        if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                        {
                            Tiles[x, y].Type = TileType.Rock;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Convert a grid position to world-space center position.
    /// </summary>
    public static Vector2 GridToWorld(Point gridPos)
    {
        return new Vector2(
            gridPos.X * GameSettings.TileSize + GameSettings.TileSize / 2f,
            gridPos.Y * GameSettings.TileSize + GameSettings.TileSize / 2f
        );
    }

    /// <summary>
    /// Convert a world-space position to a grid position.
    /// </summary>
    public static Point WorldToGrid(Vector2 worldPos)
    {
        return new Point(
            (int)(worldPos.X / GameSettings.TileSize),
            (int)(worldPos.Y / GameSettings.TileSize)
        );
    }

    /// <summary>
    /// Recompute all heatmaps (one per exit) and extract paths for every spawn.
    /// Called on init and whenever towers are placed or removed.
    /// </summary>
    public bool RecomputeActivePath()
    {
        return RecomputeAllPaths();
    }

    /// <summary>
    /// Compute a path from a custom start position to the exit paired with the given exit name.
    /// If exitName is null, uses the first exit. Used when enemies reroute from their current position.
    /// </summary>
    public List<Point>? ComputePathFromPosition(Point startPos, string? exitName = null)
    {
        string resolvedExit = ResolveExitName(exitName);

        if (!_heatMaps.TryGetValue(resolvedExit, out var heatMap))
            return null;

        return Pathfinder.ExtractPath(startPos, heatMap, Columns, Rows);
    }

    /// <summary>
    /// Dijkstra flood fill from each exit, then extract optimal path from each spawn.
    /// Lane pairing: "spawn_a" → "exit_a" by matching suffix after first underscore.
    /// Falls back to first exit if no suffix match exists.
    /// </summary>
    private bool RecomputeAllPaths()
    {
        _heatMaps.Clear();

        // Build one heatmap per exit
        foreach (var (exitName, exitPoint) in MapData.ExitPoints)
        {
            _heatMaps[exitName] = Pathfinder.ComputeHeatMap(
                exitPoint,
                Columns,
                Rows,
                p => Tiles[p.X, p.Y].MovementCost
            );
        }

        // Extract a path for each spawn using its paired exit's heatmap
        var newPaths = new Dictionary<string, List<Point>>();
        bool allSucceeded = true;

        foreach (var (spawnName, spawnPoint) in MapData.SpawnPoints)
        {
            string exitName = ResolveExitName(spawnName);
            var heatMap = _heatMaps[exitName];
            var path = Pathfinder.ExtractPath(spawnPoint, heatMap, Columns, Rows);

            newPaths[spawnName] = path ?? new List<Point>();
            if (path == null)
                allSucceeded = false;
        }

        ActivePaths = newPaths;
        return allSucceeded;
    }

    /// <summary>
    /// Given a spawn name or explicit exit name, return the exit name to use.
    /// Pairing rule: strip "spawn" prefix from the spawn name to get the suffix,
    /// then look for "exit" + suffix. Example: "spawn_a" → suffix "_a" → looks for "exit_a".
    /// Falls back to the first exit if no match.
    /// </summary>
    private string ResolveExitName(string? nameHint)
    {
        if (nameHint != null)
        {
            // Direct hit: the hint is already an exit name
            if (MapData.ExitPoints.ContainsKey(nameHint))
                return nameHint;

            // Derive suffix from spawn name and look for matching exit
            if (nameHint.StartsWith("spawn", StringComparison.OrdinalIgnoreCase))
            {
                string suffix = nameHint["spawn".Length..]; // e.g. "_a" from "spawn_a"
                string candidate = "exit" + suffix; // e.g. "exit_a"
                if (MapData.ExitPoints.ContainsKey(candidate))
                    return candidate;
            }
        }

        return MapData.ExitPoints.Keys.First();
    }

    /// <summary>
    /// Check if a grid position is valid and buildable.
    /// Buildability is determined by the tile type's IsBuildable flag and whether a tower already occupies it.
    /// </summary>
    public bool CanBuild(Point gridPos)
    {
        if (gridPos.X < 0 || gridPos.X >= Columns || gridPos.Y < 0 || gridPos.Y >= Rows)
            return false;

        var tile = Tiles[gridPos.X, gridPos.Y];
        return TileData.GetStats(tile.Type).IsBuildable
            && tile.OccupyingTower == null
            && tile.ReservedByTower == null;
    }

    /// <summary>
    /// Draw the grid with colored tiles.
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
                    GameSettings.TileSize
                );

                var tile = Tiles[x, y];
                TextureManager.DrawTile(spriteBatch, rect, tile.Type);
            }
        }

        DrawActivePathOverlay(spriteBatch);
    }

    /// <summary>
    /// Debug visualization: draws all active paths as connected lines.
    /// </summary>
    private void DrawActivePathOverlay(SpriteBatch spriteBatch)
    {
        foreach (var path in ActivePaths.Values)
            DrawPath(spriteBatch, path);
    }

    private void DrawPath(SpriteBatch spriteBatch, List<Point> path)
    {
        if (path.Count == 0)
            return;

        const int dotSize = 8;
        int halfDot = dotSize / 2;
        var pathColor = Color.DeepSkyBlue * 0.63f;

        for (int i = 0; i < path.Count; i++)
        {
            var point = path[i];
            if (point.X < 0 || point.X >= Columns || point.Y < 0 || point.Y >= Rows)
                continue;

            int centerX = point.X * GameSettings.TileSize + GameSettings.TileSize / 2;
            int centerY = point.Y * GameSettings.TileSize + GameSettings.TileSize / 2;

            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(centerX - halfDot, centerY - halfDot, dotSize, dotSize),
                pathColor
            );

            if (i < path.Count - 1)
            {
                var next = path[i + 1];
                int nextCenterX = next.X * GameSettings.TileSize + GameSettings.TileSize / 2;
                int nextCenterY = next.Y * GameSettings.TileSize + GameSettings.TileSize / 2;

                if (next.Y == point.Y && next.X != point.X)
                {
                    int minX = Math.Min(centerX, nextCenterX);
                    TextureManager.DrawRect(
                        spriteBatch,
                        new Rectangle(
                            minX,
                            centerY - halfDot / 2,
                            Math.Abs(nextCenterX - centerX),
                            halfDot
                        ),
                        pathColor
                    );
                }
                else if (next.X == point.X && next.Y != point.Y)
                {
                    int minY = Math.Min(centerY, nextCenterY);
                    TextureManager.DrawRect(
                        spriteBatch,
                        new Rectangle(
                            centerX - halfDot / 2,
                            minY,
                            halfDot,
                            Math.Abs(nextCenterY - centerY)
                        ),
                        pathColor
                    );
                }
            }
        }
    }
}
