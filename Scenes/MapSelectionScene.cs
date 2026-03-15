using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGameGum;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.Managers;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Map selection screen. Cards are arranged 3 per row; card height shrinks
/// to fit all rows within the available vertical space.
/// </summary>
public class MapSelectionScene : IScene
{
    private readonly Game1 _game;
    private InputManager _inputManager = null!;
    private MapSelectionGumView? _gumView;
    private SpriteFont? _font;

    private List<MapData> _availableMaps = [];
    private List<RectangleF> _cardBounds = [];
    private int _layoutWidth;
    private int _layoutHeight;

    // Track which card is currently hovered (-1 = none)
    private int _hoveredCardIndex = -1;
    private bool _isNavigatingAway;

    private const int CardsPerRow = 3;
    private const int CardWidth = 280;
    private const int HorizontalGap = 50;
    private const int VerticalGap = 30;
    private const int TitleAreaHeight = 120; // space reserved at top for title
    private const int ScreenPadding = 30; // bottom margin
    private const int BackButtonWidth = 150;
    private const int BackButtonHeight = 52;
    private const int BackButtonMargin = 24;

    public MapSelectionScene(Game1 game)
    {
        _game = game;
    }

    public void LoadContent()
    {
        _inputManager = new InputManager();

        _availableMaps = MapDataRepository.GetAvailableMaps().ConvertAll(MapDataRepository.GetMap);
        _isNavigatingAway = false;
        _gumView = new MapSelectionGumView(
            _availableMaps,
            onBackClicked: ReturnToStartMenu,
            onMapClicked: StartMapByIndex
        );
        var (viewportWidth, viewportHeight) = GetViewportSize();
        RebuildLayout(viewportWidth, viewportHeight);
        _gumView.AttachToRoot();

        try
        {
            _font = _game.Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available
        }
    }

    public void UnloadContent()
    {
        _gumView?.Dispose();
        _gumView = null;
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();
        HandleViewportResize();

        Vector2 mousePos = _inputManager.MousePosition.ToVector2();

        _hoveredCardIndex = -1;
        for (int i = 0; i < _cardBounds.Count; i++)
        {
            if (_cardBounds[i].Contains(mousePos))
            {
                _hoveredCardIndex = i;
                break;
            }
        }

        if (_inputManager.IsKeyPressed(Keys.Escape))
        {
            ReturnToStartMenu();
        }
    }

    private void HandleViewportResize()
    {
        var (viewportWidth, viewportHeight) = GetViewportSize();

        if (viewportWidth == _layoutWidth && viewportHeight == _layoutHeight)
            return;

        RebuildLayout(viewportWidth, viewportHeight);
    }

    private void ReturnToStartMenu()
    {
        if (_isNavigatingAway)
            return;

        _isNavigatingAway = true;
        _game.SetScene(new StartMenuScene(_game));
    }

    private void StartMapByIndex(int index)
    {
        if (_isNavigatingAway)
            return;

        if (index < 0 || index >= _availableMaps.Count)
            return;

        _isNavigatingAway = true;
        string selectedMapId = _availableMaps[index].Id;
        var gameplayScene = new GameplayScene(_game, selectedMapId);
        _game.SetScene(gameplayScene);
    }

    private (int width, int height) GetViewportSize() =>
        (
            Math.Max(1, _game.GraphicsDevice.Viewport.Width),
            Math.Max(1, _game.GraphicsDevice.Viewport.Height)
        );

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_font != null)
        {
            string title = "Select Your Map";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2((GameSettings.ScreenWidth - titleSize.X) / 2, 40);

            spriteBatch.DrawString(_font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(_font, title, titlePos, Color.White);
        }

        for (int i = 0; i < _availableMaps.Count; i++)
        {
            DrawMapCard(spriteBatch, _cardBounds[i], _availableMaps[i], _hoveredCardIndex == i);
        }

        if (!GumService.Default.IsInitialized)
            return;

        // SceneManager has already started a SpriteBatch. Gum renders with its own pipeline,
        // so we briefly close this batch, draw Gum, then restore SceneManager's expected state.
        spriteBatch.End();
        GumService.Default.Draw();
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            null
        );
    }

    /// <summary>
    /// Draws a single map selection card. The preview area scales with card height,
    /// leaving fixed margins for the title (top) and metadata (bottom).
    /// </summary>
    private void DrawMapCard(
        SpriteBatch spriteBatch,
        RectangleF bounds,
        MapData mapData,
        bool isHovered
    )
    {
        Color bgColor = isHovered ? Color.DarkSlateGray : Color.DarkSlateGray * 0.89f;
        TextureManager.DrawRect(spriteBatch, bounds.ToRectangle(), bgColor);

        Color borderColor = isHovered ? Color.Yellow : Color.Gray;
        TextureManager.DrawRectOutline(spriteBatch, bounds.ToRectangle(), borderColor, 3);

        const int titleMargin = 40; // height reserved for map name at top of card
        const int metaMargin = 35; // height reserved for grid-size text at bottom of card

        if (_font != null)
        {
            Vector2 titleSize = _font.MeasureString(mapData.Name);
            Vector2 titlePos = new Vector2(
                bounds.X + (bounds.Width - titleSize.X) / 2f,
                bounds.Y + 10
            );

            spriteBatch.DrawString(_font, mapData.Name, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(_font, mapData.Name, titlePos, Color.White);
        }

        // Preview fills the space between title and metadata margins.
        Rectangle previewBounds = new Rectangle(
            (int)(bounds.X + 10),
            (int)(bounds.Y + titleMargin),
            (int)(bounds.Width - 20),
            (int)(bounds.Height - titleMargin - metaMargin)
        );
        DrawMapPreview(spriteBatch, mapData, previewBounds);

        if (_font != null)
        {
            string info = $"{mapData.Columns}x{mapData.Rows}";
            Vector2 infoSize = _font.MeasureString(info);
            Vector2 infoPos = new Vector2(
                bounds.X + (bounds.Width - infoSize.X) / 2f,
                bounds.Bottom - metaMargin + 8
            );
            spriteBatch.DrawString(_font, info, infoPos, Color.LightGray);
        }
    }

    /// <summary>
    /// Renders a miniature version of the map grid showing terrain types.
    /// </summary>
    private static void DrawMapPreview(SpriteBatch spriteBatch, MapData mapData, Rectangle bounds)
    {
        int miniTileSize = Math.Max(
            2,
            Math.Min(bounds.Width / mapData.Columns, bounds.Height / mapData.Rows)
        );

        int gridWidth = mapData.Columns * miniTileSize;
        int gridHeight = mapData.Rows * miniTileSize;
        int startX = bounds.X + (bounds.Width - gridWidth) / 2;
        int startY = bounds.Y + (bounds.Height - gridHeight) / 2;

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

                Color tileColor;
                if (mapData.TileGrid != null)
                {
                    tileColor = mapData.TileGrid[x, y] switch
                    {
                        TileType.Path => Color.Tan,
                        TileType.Rock => Color.DarkGray,
                        _ => Color.ForestGreen,
                    };
                }
                else
                {
                    tileColor = mapData.IsInWalkableArea(new Point(x, y))
                        ? Color.Tan
                        : Color.ForestGreen;
                }

                TextureManager.DrawRect(spriteBatch, tileRect, tileColor);
            }
        }

        Rectangle gridBounds = new Rectangle(startX, startY, gridWidth, gridHeight);
        TextureManager.DrawRectOutline(spriteBatch, gridBounds, Color.White * 0.39f, 2);
    }

    private void RebuildLayout(int viewportWidth, int viewportHeight)
    {
        _layoutWidth = viewportWidth;
        _layoutHeight = viewportHeight;
        GameSettings.SetScreenSize(viewportWidth, viewportHeight);

        int count = _availableMaps.Count;
        int rowCount = (int)Math.Ceiling(count / (double)CardsPerRow);

        // Compute card height so all rows fit vertically within the current window height.
        int availableHeight =
            viewportHeight - TitleAreaHeight - ScreenPadding - (VerticalGap * (rowCount - 1));
        int cardHeight = rowCount > 0 ? availableHeight / rowCount : 200;

        // Center the grid horizontally. If there is only one partial row, center that too.
        int colsThisLayout = Math.Min(count, CardsPerRow);
        int totalWidth = (CardWidth * colsThisLayout) + (HorizontalGap * (colsThisLayout - 1));
        int startX = (viewportWidth - totalWidth) / 2;

        _cardBounds = [];
        for (int i = 0; i < count; i++)
        {
            int col = i % CardsPerRow;
            int row = i / CardsPerRow;
            int x = startX + col * (CardWidth + HorizontalGap);
            int y = TitleAreaHeight + row * (cardHeight + VerticalGap);
            _cardBounds.Add(new RectangleF(x, y, CardWidth, cardHeight));
        }

        RectangleF backButtonBounds = new RectangleF(
            viewportWidth - BackButtonWidth - BackButtonMargin,
            BackButtonMargin,
            BackButtonWidth,
            BackButtonHeight
        );

        _gumView?.ResizeToViewport(viewportWidth, viewportHeight);
        _gumView?.UpdateLayout(_cardBounds, backButtonBounds);
    }
}
