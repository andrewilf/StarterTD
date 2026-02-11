using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Entities;

namespace StarterTD.Engine;

/// <summary>
/// Represents what a tile on the grid can be.
/// </summary>
public enum TileType
{
    /// <summary>High ground: towers can be placed, but enemies cannot walk through.</summary>
    HighGround,

    /// <summary>Walkable terrain. Enemies can traverse, towers can be placed.</summary>
    Path,

    /// <summary>Impassable and unbuildable rock terrain.</summary>
    Rock,
}

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

    /// <summary>
    /// Movement cost for pathfinding. Towers add a cost penalty based on type.
    /// HighGround and Rock are impassable (int.MaxValue). Path is walkable (cost 1).
    /// If a tower occupies this tile, use its tower type's movement cost.
    /// Like a Python @property — recomputes from current state, no stored field.
    /// </summary>
    public int MovementCost =>
        OccupyingTower != null
            ? TowerData.GetStats(OccupyingTower.TowerType).MovementCost
            : Type switch
            {
                TileType.HighGround => int.MaxValue,
                TileType.Path => 1,
                TileType.Rock => int.MaxValue,
                _ => int.MaxValue,
            };

    public Tile(Point gridPosition, TileType type)
    {
        GridPosition = gridPosition;
        Type = type;
    }
}

/// <summary>
/// The game map: a 2D grid of tiles with Dijkstra-based pathfinding.
/// Terrain is defined by WalkableAreas rectangles; enemies find their own route.
/// </summary>
public class Map
{
    public Tile[,] Tiles { get; }
    public int Columns { get; }
    public int Rows { get; }

    /// <summary>Where enemies enter the map.</summary>
    public Point SpawnPoint { get; }

    /// <summary>Where enemies leave the map.</summary>
    public Point ExitPoint { get; }

    /// <summary>
    /// The active path enemies follow. Extracted from the heat map via gradient descent.
    /// Recomputed whenever towers change.
    /// </summary>
    public List<Point> ActivePath { get; private set; }

    /// <summary>
    /// Cost-to-exit for every tile. Recomputed when towers are placed.
    /// heatMap[x, y] = minimum total movement cost from (x,y) to the exit.
    /// </summary>
    public int[,]? HeatMap { get; private set; }

    /// <summary>
    /// The map data used to create this map.
    /// </summary>
    public MapData MapData { get; }

    /// <summary>
    /// BACKWARD COMPATIBLE: Default constructor uses classic S-path.
    /// </summary>
    public Map()
        : this(MapDataRepository.GetMap("classic_s")) { }

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
        SpawnPoint = mapData.SpawnPoint;
        ExitPoint = mapData.ExitPoint;
        ActivePath = new List<Point>();

        InitializeTiles();
        RecomputeHeatMap();
    }

    /// <summary>
    /// Static factory for map ID.
    /// </summary>
    public static Map FromId(string mapId)
    {
        return new Map(MapDataRepository.GetMap(mapId));
    }

    /// <summary>
    /// Initialize tiles: all start as HighGround, then WalkableAreas are marked as Path.
    /// </summary>
    private void InitializeTiles()
    {
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
    /// Recompute the heat map from the exit and extract a new ActivePath.
    /// Towers are expensive (cost 500) but passable, so this always succeeds.
    /// </summary>
    public bool RecomputeActivePath()
    {
        return RecomputeHeatMap();
    }

    /// <summary>
    /// Compute a path from a custom start position to the exit (using the current heat map).
    /// Like ExtractPath but from an arbitrary grid position instead of just the spawn point.
    /// Used when enemies need to pathfind from their current location after the map changes.
    /// </summary>
    public List<Point>? ComputePathFromPosition(Point startPos)
    {
        if (HeatMap == null)
            return null;

        return Pathfinder.ExtractPath(startPos, HeatMap, Columns, Rows);
    }

    /// <summary>
    /// Dijkstra flood fill from exit, then extract optimal path from spawn.
    /// </summary>
    private bool RecomputeHeatMap()
    {
        HeatMap = Pathfinder.ComputeHeatMap(
            ExitPoint,
            Columns,
            Rows,
            p => Tiles[p.X, p.Y].MovementCost
        );

        var extracted = Pathfinder.ExtractPath(SpawnPoint, HeatMap, Columns, Rows);

        ActivePath = extracted ?? new List<Point>();
        return extracted != null;
    }

    /// <summary>
    /// Check if a grid position is valid and buildable.
    /// HighGround and Path tiles are buildable if not occupied by a tower. Rock tiles are never buildable.
    /// </summary>
    public bool CanBuild(Point gridPos)
    {
        if (gridPos.X < 0 || gridPos.X >= Columns || gridPos.Y < 0 || gridPos.Y >= Rows)
            return false;

        var tile = Tiles[gridPos.X, gridPos.Y];
        bool isBuildableTerrain = tile.Type == TileType.HighGround || tile.Type == TileType.Path;
        bool notOccupied = tile.OccupyingTower == null;

        return isBuildableTerrain && notOccupied;
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

                Color tileColor = tile.Type switch
                {
                    TileType.HighGround => Color.ForestGreen,
                    TileType.Path => Color.Tan,
                    TileType.Rock => Color.DarkGray,
                    _ => Color.Black,
                };

                TextureManager.DrawRect(spriteBatch, rect, tileColor);
                TextureManager.DrawRectOutline(spriteBatch, rect, Color.Black * 0.24f, 1);
            }
        }

        DrawActivePathOverlay(spriteBatch);
    }

    /// <summary>
    /// Debug visualization: draws the ActivePath as a connected line.
    /// </summary>
    private void DrawActivePathOverlay(SpriteBatch spriteBatch)
    {
        if (ActivePath.Count == 0)
            return;

        const int dotSize = 8;
        int halfDot = dotSize / 2;
        var pathColor = Color.DeepSkyBlue * 0.63f;

        for (int i = 0; i < ActivePath.Count; i++)
        {
            var point = ActivePath[i];
            if (point.X < 0 || point.X >= Columns || point.Y < 0 || point.Y >= Rows)
                continue;

            int centerX = point.X * GameSettings.TileSize + GameSettings.TileSize / 2;
            int centerY = point.Y * GameSettings.TileSize + GameSettings.TileSize / 2;

            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(centerX - halfDot, centerY - halfDot, dotSize, dotSize),
                pathColor
            );

            if (i < ActivePath.Count - 1)
            {
                var next = ActivePath[i + 1];
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
