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

    public Map()
    {
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

        // Define a hardcoded path (an S-shaped path across the map)
        PathPoints = GeneratePath();

        // Mark path tiles
        foreach (var point in PathPoints)
        {
            Tiles[point.X, point.Y].Type = TileType.Path;
        }
    }

    /// <summary>
    /// Generates an S-shaped path from left to right across the map.
    /// The path goes: left-to-right on row 2, down, right-to-left on row 6,
    /// down, left-to-right on row 10, down, right-to-left on row 13.
    /// </summary>
    private List<Point> GeneratePath()
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
