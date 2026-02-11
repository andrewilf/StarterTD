using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.Managers;

namespace StarterTD.Scenes;

/// <summary>
/// Map selection screen that allows players to choose from available maps
/// before starting gameplay. Displays preview cards for each map.
/// </summary>
public class MapSelectionScene : IScene
{
    private readonly Game1 _game;
    private InputManager _inputManager = null!;
    private SpriteFont? _font;

    // Card bounds for the three map options
    private Rectangle _card1Bounds;
    private Rectangle _card2Bounds;
    private Rectangle _card3Bounds;

    // Track which card is currently hovered (-1 = none, 0-2 = card index)
    private int _hoveredCardIndex = -1;

    // Available maps loaded from repository
    private List<MapData> _availableMaps = new();

    public MapSelectionScene(Game1 game)
    {
        _game = game;

        // Calculate card positions (3 cards, 280x500 each, 50px gaps)
        const int cardWidth = 280;
        const int cardHeight = 500;
        const int gap = 50;
        const int cardY = 150;

        // Center the 3 cards horizontally
        int totalWidth = (cardWidth * 3) + (gap * 2);
        int startX = (GameSettings.ScreenWidth - totalWidth) / 2;

        _card1Bounds = new Rectangle(startX, cardY, cardWidth, cardHeight);
        _card2Bounds = new Rectangle(startX + cardWidth + gap, cardY, cardWidth, cardHeight);
        _card3Bounds = new Rectangle(startX + (cardWidth + gap) * 2, cardY, cardWidth, cardHeight);
    }

    public void LoadContent()
    {
        _inputManager = new InputManager();

        // Load the 3 available maps from the repository
        _availableMaps = new List<MapData>
        {
            MapDataRepository.GetMap("classic_s"),
            MapDataRepository.GetMap("straight"),
            MapDataRepository.GetMap("maze_test"),
        };

        // Try to load font (same pattern as GameplayScene)
        try
        {
            _font = _game.Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available — will use fallback rendering
        }
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();

        Point mousePos = _inputManager.MousePosition;

        // Update hover state
        _hoveredCardIndex = -1;
        if (_card1Bounds.Contains(mousePos))
            _hoveredCardIndex = 0;
        else if (_card2Bounds.Contains(mousePos))
            _hoveredCardIndex = 1;
        else if (_card3Bounds.Contains(mousePos))
            _hoveredCardIndex = 2;

        // Handle click to select map
        if (_inputManager.IsLeftClick())
        {
            string? selectedMapId = _hoveredCardIndex switch
            {
                0 => "classic_s",
                1 => "straight",
                2 => "maze_test",
                _ => null,
            };

            if (selectedMapId != null)
            {
                // Transition to gameplay with selected map
                var gameplayScene = new GameplayScene(_game, selectedMapId);
                _game.SetScene(gameplayScene);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw title at top
        if (_font != null)
        {
            string title = "Select Your Map";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2((GameSettings.ScreenWidth - titleSize.X) / 2, 50);

            // Draw title with shadow
            spriteBatch.DrawString(_font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(_font, title, titlePos, Color.White);
        }

        // Draw the 3 map cards
        DrawMapCard(spriteBatch, _card1Bounds, _availableMaps[0], _hoveredCardIndex == 0);
        DrawMapCard(spriteBatch, _card2Bounds, _availableMaps[1], _hoveredCardIndex == 1);
        DrawMapCard(spriteBatch, _card3Bounds, _availableMaps[2], _hoveredCardIndex == 2);
    }

    /// <summary>
    /// Draws a single map selection card with background, border, title, preview, and metadata.
    /// </summary>
    private void DrawMapCard(
        SpriteBatch spriteBatch,
        Rectangle bounds,
        MapData mapData,
        bool isHovered
    )
    {
        // Card background (lighter when hovered)
        Color bgColor = isHovered ? Color.DarkSlateGray : Color.DarkSlateGray * 0.89f;
        TextureManager.DrawRect(spriteBatch, bounds, bgColor);

        // Border (yellow when hovered, gray otherwise)
        Color borderColor = isHovered ? Color.Yellow : Color.Gray;
        TextureManager.DrawRectOutline(spriteBatch, bounds, borderColor, 3);

        // Title text at top
        if (_font != null)
        {
            Vector2 titleSize = _font.MeasureString(mapData.Name);
            Vector2 titlePos = new Vector2(
                bounds.X + (bounds.Width - titleSize.X) / 2,
                bounds.Y + 15
            );

            // Shadow
            spriteBatch.DrawString(_font, mapData.Name, titlePos + new Vector2(2, 2), Color.Black);
            // Main text
            spriteBatch.DrawString(_font, mapData.Name, titlePos, Color.White);
        }

        // Map preview area (offset from top to leave room for title)
        Rectangle previewBounds = new Rectangle(
            bounds.X + 10,
            bounds.Y + 60,
            bounds.Width - 20,
            200
        );
        DrawMapPreview(spriteBatch, mapData, previewBounds);

        // Metadata at bottom
        if (_font != null)
        {
            string info = $"{mapData.Columns}x{mapData.Rows}";
            Vector2 infoSize = _font.MeasureString(info);
            Vector2 infoPos = new Vector2(
                bounds.X + (bounds.Width - infoSize.X) / 2,
                bounds.Bottom - 40
            );
            spriteBatch.DrawString(_font, info, infoPos, Color.LightGray);
        }
    }

    /// <summary>
    /// Renders a miniature version of the map grid showing paths, buildable areas, and maze zones.
    /// Uses the same color scheme as the full Map class.
    /// </summary>
    private static void DrawMapPreview(SpriteBatch spriteBatch, MapData mapData, Rectangle bounds)
    {
        const int miniTileSize = 13;

        // Calculate centered position
        int gridWidth = mapData.Columns * miniTileSize;
        int gridHeight = mapData.Rows * miniTileSize;
        int startX = bounds.X + (bounds.Width - gridWidth) / 2;
        int startY = bounds.Y + (bounds.Height - gridHeight) / 2;

        // Render grid
        for (int x = 0; x < mapData.Columns; x++)
        {
            for (int y = 0; y < mapData.Rows; y++)
            {
                Rectangle tileRect = new Rectangle(
                    startX + x * miniTileSize,
                    startY + y * miniTileSize,
                    miniTileSize,
                    miniTileSize
                );

                // Check if tile is inside any walkable area
                Color tileColor = mapData.IsInWalkableArea(new Point(x, y))
                    ? Color.Tan
                    : Color.ForestGreen;

                TextureManager.DrawRect(spriteBatch, tileRect, tileColor);
            }
        }

        // Draw preview border
        Rectangle gridBounds = new Rectangle(startX, startY, gridWidth, gridHeight);
        TextureManager.DrawRectOutline(spriteBatch, gridBounds, Color.White * 0.39f, 2);
    }
}
