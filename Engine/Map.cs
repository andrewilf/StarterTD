using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Engine;

/// <summary>
/// Identifies the different map layouts available for selection.
/// </summary>
public enum MapType
{
    SShape,
    Spiral,
    ZigZag
}

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

    public Tile(Point gridPosition, TileType type)
    {
        GridPosition = gridPosition;
        Type = type;
    }
}

/// <summary>
/// The game map: a 2D grid of tiles with a hardcoded enemy path.
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

    public MapType MapType { get; }

    public Map(MapType mapType = MapType.SShape)
    {
        MapType = mapType;
        Columns = GameSettings.GridColumns;
        Rows = GameSettings.GridRows;
        Tiles = new Tile[Columns, Rows];

        // Initialize all tiles as buildable
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                Tiles[x, y] = new Tile(new Point(x, y), TileType.Buildable);
            }
        }

        // Generate path based on map type
        PathPoints = mapType switch
        {
            MapType.Spiral => GenerateSpiralPath(),
            MapType.ZigZag => GenerateZigZagPath(),
            _ => GenerateSShapePath()
        };

        // Mark path tiles
        foreach (var point in PathPoints)
        {
            Tiles[point.X, point.Y].Type = TileType.Path;
        }
    }

    private List<Point> GenerateSShapePath()
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

        return path;
    }

    private List<Point> GenerateSpiralPath()
    {
        var path = new List<Point>();

        // Outer ring: top left-to-right
        for (int x = 0; x < 19; x++)
            path.Add(new Point(x, 0));
        // Right side going down
        for (int y = 1; y <= 13; y++)
            path.Add(new Point(18, y));
        // Bottom right-to-left
        for (int x = 18; x >= 1; x--)
            path.Add(new Point(x, 13));
        // Left side going up
        for (int y = 13; y >= 4; y--)
            path.Add(new Point(1, y));

        // Inner ring: top left-to-right
        for (int x = 1; x < 17; x++)
            path.Add(new Point(x, 4));
        // Right side going down
        for (int y = 5; y <= 10; y++)
            path.Add(new Point(16, y));
        // Bottom right-to-left
        for (int x = 16; x >= 4; x--)
            path.Add(new Point(x, 10));
        // Left side going up to center
        for (int y = 10; y >= 7; y--)
            path.Add(new Point(4, y));
        // Center: left-to-right
        for (int x = 4; x <= 10; x++)
            path.Add(new Point(x, 7));

        return path;
    }

    private List<Point> GenerateZigZagPath()
    {
        var path = new List<Point>();

        // Vertical zigzag: enter from top-left, exit bottom-right
        // Column 1: top to bottom
        for (int y = 0; y <= 13; y++)
            path.Add(new Point(1, y));
        // Cross to column 5
        for (int x = 2; x <= 5; x++)
            path.Add(new Point(x, 13));
        // Column 5: bottom to top
        for (int y = 13; y >= 1; y--)
            path.Add(new Point(5, y));
        // Cross to column 9
        for (int x = 6; x <= 9; x++)
            path.Add(new Point(x, 1));
        // Column 9: top to bottom
        for (int y = 1; y <= 13; y++)
            path.Add(new Point(9, y));
        // Cross to column 13
        for (int x = 10; x <= 13; x++)
            path.Add(new Point(x, 13));
        // Column 13: bottom to top
        for (int y = 13; y >= 1; y--)
            path.Add(new Point(13, y));
        // Cross to column 17
        for (int x = 14; x <= 17; x++)
            path.Add(new Point(x, 1));
        // Column 17: top to bottom exit
        for (int y = 1; y <= 14; y++)
            path.Add(new Point(17, y));

        return path;
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
    /// </summary>
    public bool CanBuild(Point gridPos)
    {
        if (gridPos.X < 0 || gridPos.X >= Columns || gridPos.Y < 0 || gridPos.Y >= Rows)
            return false;
        return Tiles[gridPos.X, gridPos.Y].Type == TileType.Buildable;
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
                    GameSettings.TileSize);

                Color tileColor = Tiles[x, y].Type switch
                {
                    TileType.Buildable => new Color(34, 139, 34),   // Forest green
                    TileType.Path => new Color(194, 178, 128),       // Sandy path
                    TileType.Occupied => new Color(100, 100, 100),   // Gray (tower placed)
                    _ => Color.Black
                };

                TextureManager.DrawRect(spriteBatch, rect, tileColor);

                // Draw grid lines
                TextureManager.DrawRectOutline(spriteBatch, rect, new Color(0, 0, 0, 60), 1);
            }
        }
    }
}
