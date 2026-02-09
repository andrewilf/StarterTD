using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.Managers;

namespace StarterTD.Scenes;

public class MapSelectionScene : IScene
{
    private readonly Game1 _game;
    private readonly SceneManager _sceneManager;
    private InputManager _inputManager = null!;
    private SpriteFont? _font;

    private static readonly MapType[] MapTypes = { MapType.SShape, MapType.Spiral, MapType.ZigZag };
    private static readonly string[] MapNames = { "S-Shape", "Spiral", "Zig-Zag" };

    // Layout constants
    private const int PanelWidth = 280;
    private const int PanelHeight = 260;
    private const int PanelSpacing = 40;
    private const int PreviewTileSize = 12;
    private const int PreviewColumns = 20;
    private const int PreviewRows = 15;

    private int _hoveredIndex = -1;
    private Rectangle[] _panelRects = null!;
    private Map[] _previewMaps = null!;

    public MapSelectionScene(Game1 game, SceneManager sceneManager)
    {
        _game = game;
        _sceneManager = sceneManager;
    }

    public void LoadContent()
    {
        _inputManager = new InputManager();

        try
        {
            _font = _game.Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available — will use fallback rendering
        }

        // Calculate panel positions centered on screen
        int totalWidth = MapTypes.Length * PanelWidth + (MapTypes.Length - 1) * PanelSpacing;
        int startX = (GameSettings.ScreenWidth - totalWidth) / 2;
        int startY = (GameSettings.ScreenHeight - PanelHeight) / 2 + 30;

        _panelRects = new Rectangle[MapTypes.Length];
        _previewMaps = new Map[MapTypes.Length];
        for (int i = 0; i < MapTypes.Length; i++)
        {
            _panelRects[i] = new Rectangle(
                startX + i * (PanelWidth + PanelSpacing),
                startY,
                PanelWidth,
                PanelHeight);
            _previewMaps[i] = new Map(MapTypes[i]);
        }
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();

        Point mousePos = _inputManager.MousePosition;

        // Determine hovered panel
        _hoveredIndex = -1;
        for (int i = 0; i < _panelRects.Length; i++)
        {
            if (_panelRects[i].Contains(mousePos))
            {
                _hoveredIndex = i;
                break;
            }
        }

        // Handle click
        if (_inputManager.IsLeftClick() && _hoveredIndex >= 0)
        {
            var scene = new GameplayScene(_game, _sceneManager, MapTypes[_hoveredIndex]);
            _sceneManager.SetScene(scene);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Background
        TextureManager.DrawRect(spriteBatch,
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.ScreenHeight),
            new Color(20, 25, 20));

        // Title
        if (_font != null)
        {
            string title = "SELECT MAP";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                (GameSettings.ScreenWidth - titleSize.X) / 2,
                60);
            spriteBatch.DrawString(_font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(_font, title, titlePos, Color.Gold);

            string subtitle = "Click a map to start";
            Vector2 subSize = _font.MeasureString(subtitle);
            Vector2 subPos = new Vector2(
                (GameSettings.ScreenWidth - subSize.X) / 2,
                60 + titleSize.Y + 10);
            spriteBatch.DrawString(_font, subtitle, subPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(_font, subtitle, subPos, Color.LightGray);
        }

        // Draw each map panel
        for (int i = 0; i < MapTypes.Length; i++)
        {
            DrawMapPanel(spriteBatch, i);
        }
    }

    private void DrawMapPanel(SpriteBatch spriteBatch, int index)
    {
        var rect = _panelRects[index];
        bool hovered = index == _hoveredIndex;

        // Panel background
        Color bgColor = hovered ? new Color(60, 70, 60) : new Color(40, 50, 40);
        TextureManager.DrawRect(spriteBatch, rect, bgColor);

        // Panel border
        Color borderColor = hovered ? Color.Gold : new Color(100, 120, 100);
        TextureManager.DrawRectOutline(spriteBatch, rect, borderColor, hovered ? 3 : 2);

        // Draw mini map preview
        int previewWidth = PreviewColumns * PreviewTileSize;
        int previewHeight = PreviewRows * PreviewTileSize;
        int previewX = rect.X + (rect.Width - previewWidth) / 2;
        int previewY = rect.Y + 15;

        var previewMap = _previewMaps[index];

        for (int x = 0; x < PreviewColumns; x++)
        {
            for (int y = 0; y < PreviewRows; y++)
            {
                var tileRect = new Rectangle(
                    previewX + x * PreviewTileSize,
                    previewY + y * PreviewTileSize,
                    PreviewTileSize,
                    PreviewTileSize);

                Color tileColor = previewMap.Tiles[x, y].Type switch
                {
                    TileType.Path => new Color(194, 178, 128),
                    _ => new Color(34, 139, 34)
                };

                TextureManager.DrawRect(spriteBatch, tileRect, tileColor);
                TextureManager.DrawRectOutline(spriteBatch, tileRect, new Color(0, 0, 0, 40), 1);
            }
        }

        // Draw map name below preview
        if (_font != null)
        {
            string name = MapNames[index];
            Vector2 nameSize = _font.MeasureString(name);
            Vector2 namePos = new Vector2(
                rect.X + (rect.Width - nameSize.X) / 2,
                previewY + previewHeight + 10);

            Color textColor = hovered ? Color.Gold : Color.White;
            spriteBatch.DrawString(_font, name, namePos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(_font, name, namePos, textColor);
        }
        else
        {
            // Fallback: draw a colored bar as label indicator
            Color labelColor = hovered ? Color.Gold : Color.White;
            TextureManager.DrawRect(spriteBatch,
                new Rectangle(rect.X + 40, previewY + previewHeight + 10, rect.Width - 80, 4),
                labelColor);
        }
    }
}
