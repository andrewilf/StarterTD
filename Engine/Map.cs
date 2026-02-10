using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Engine;

/// <summary>
/// Represents what a tile on the grid can be.
/// </summary>
public enum TileType
{
    /// <summary>Empty buildable ground.</summary>
    Buildable,
    /// <summary>Part of the enemy path — cannot build here.</summary>
    Path,
    /// <summary>A tower has been placed on this tile.</summary>
    Occupied,
    /// <summary>
    /// Within a maze zone - can build here to create mazes.
    /// Path goes through this zone, but it's buildable.
    /// Future: Building here may trigger dynamic pathfinding.
    /// </summary>
    MazeZoneBuildable
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
    /// Null for non-maze-zone tiles.
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
/// The game map: a 2D grid of tiles with data-driven paths and maze zones.
/// Think of this like a 2D array board in a board-game simulation.
/// </summary>
public class Map
{
    public Tile[,] Tiles { get; }
    public int Columns { get; }
    public int Rows { get; }

    /// <summary>
    /// Ordered list of grid positions that enemies follow from start to end.
    /// </summary>
    public List<Point> PathPoints { get; }

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
    /// NEW: Primary constructor accepting MapData.
    /// </summary>
    public Map(MapData mapData)
    {
        MapData = mapData;
        mapData.Validate(); // Fail fast if invalid

        Columns = mapData.Columns;
        Rows = mapData.Rows;
        Tiles = new Tile[Columns, Rows];
        PathPoints = mapData.PathPoints;

        InitializeTiles();
    }

    /// <summary>
    /// ALTERNATIVE: Static factory for map ID.
    /// </summary>
    public static Map FromId(string mapId)
    {
        return new Map(MapDataRepository.GetMap(mapId));
    }

    /// <summary>
    /// Initialize tiles with 3-phase logic: buildable → maze zones → path.
    /// </summary>
    private void InitializeTiles()
    {
        // Phase 1: Initialize all tiles as buildable
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                Tiles[x, y] = new Tile(new Point(x, y), TileType.Buildable);
            }
        }

        // Phase 2: Mark maze zone tiles (BEFORE path, so path overrides)
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
                            Tiles[x, y].Type = TileType.MazeZoneBuildable;
                            Tiles[x, y].MazeZone = zone;
                        }
                    }
                }
            }
        }

        // Phase 3: Mark path tiles (OVERRIDES maze zones if path goes through)
        foreach (var point in PathPoints)
        {
            Tiles[point.X, point.Y].Type = TileType.Path;
            // Keep MazeZone reference even if it's a path tile (for future pathfinding)
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
    /// Check if a grid position is valid and buildable.
    /// ENHANCED: Now allows maze zone tiles.
    /// </summary>
    public bool CanBuild(Point gridPos)
    {
        if (gridPos.X < 0 || gridPos.X >= Columns || gridPos.Y < 0 || gridPos.Y >= Rows)
            return false;

        var tileType = Tiles[gridPos.X, gridPos.Y].Type;
        return tileType == TileType.Buildable || tileType == TileType.MazeZoneBuildable;
    }

    /// <summary>
    /// Draw the grid with colored tiles.
    /// ENHANCED: Maze zone visualization with brighter green and yellow border.
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

                Color tileColor = Tiles[x, y].Type switch
                {
                    TileType.Buildable => new Color(34, 139, 34),         // Forest green
                    TileType.MazeZoneBuildable => new Color(50, 180, 50), // Brighter green
                    TileType.Path => new Color(194, 178, 128),             // Sandy path
                    TileType.Occupied => new Color(100, 100, 100),         // Gray (tower placed)
                    _ => Color.Black
                };

                TextureManager.DrawRect(spriteBatch, rect, tileColor);

                // Draw grid lines
                TextureManager.DrawRectOutline(spriteBatch, rect, new Color(0, 0, 0, 60), 1);

                // Draw maze zone border highlight (optional visual feedback)
                if (Tiles[x, y].Type == TileType.MazeZoneBuildable)
                {
                    TextureManager.DrawRectOutline(spriteBatch, rect,
                        new Color(255, 255, 100, 100), 2);
                }
            }
        }
    }
}
