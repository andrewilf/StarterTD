using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.Managers;

namespace StarterTD.Scenes;

/// <summary>
/// Top-level start menu shown on launch.
/// Keeps the initial navigation separate from map selection.
/// </summary>
public class StartMenuScene : IScene
{
    private readonly Game1 _game;
    private InputManager _inputManager = null!;
    private SpriteFont? _font;

    private Rectangle _startButton;
    private Rectangle _settingsButton;
    private Rectangle _exitButton;
    private int _layoutWidth;
    private int _layoutHeight;

    private bool _isStartHovered;
    private bool _isSettingsHovered;
    private bool _isExitHovered;

    private const int ButtonWidth = 260;
    private const int ButtonHeight = 64;
    private const int ButtonGap = 22;
    private const int TitleTopMargin = 110;

    public StartMenuScene(Game1 game)
    {
        _game = game;
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
            // Font not available - menu falls back to shape-only rendering.
        }

        var (viewportWidth, viewportHeight) = GetViewportSize();
        RebuildLayout(viewportWidth, viewportHeight);
    }

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();
        HandleViewportResize();

        Point mousePos = _inputManager.MousePosition;
        _isStartHovered = _startButton.Contains(mousePos);
        _isSettingsHovered = _settingsButton.Contains(mousePos);
        _isExitHovered = _exitButton.Contains(mousePos);

        if (!_inputManager.IsLeftClick())
            return;

        if (_isStartHovered)
        {
            _game.TransitionToScene(
                new MapSelectionScene(_game),
                SceneTransitionPreset.MenuForwardSlideFade
            );
            return;
        }

        if (_isSettingsHovered)
            return;

        if (_isExitHovered)
            _game.Exit();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.ScreenHeight),
            new Color(18, 24, 34)
        );

        if (_font != null)
        {
            string title = "StarterTD";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                (GameSettings.ScreenWidth - titleSize.X) / 2f,
                TitleTopMargin
            );

            spriteBatch.DrawString(_font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(_font, title, titlePos, Color.White);
        }

        DrawButton(
            spriteBatch,
            _startButton,
            "Start",
            _isStartHovered,
            Color.DarkSlateGray,
            Color.CadetBlue,
            Color.LightGray,
            Color.White
        );
        DrawButton(
            spriteBatch,
            _settingsButton,
            "Settings",
            _isSettingsHovered,
            Color.DarkSlateGray,
            Color.SlateGray,
            Color.LightGray,
            Color.White
        );
        DrawButton(
            spriteBatch,
            _exitButton,
            "Exit",
            _isExitHovered,
            Color.Maroon,
            Color.IndianRed,
            Color.Salmon,
            Color.White
        );
    }

    private void HandleViewportResize()
    {
        var (viewportWidth, viewportHeight) = GetViewportSize();
        if (viewportWidth == _layoutWidth && viewportHeight == _layoutHeight)
            return;

        RebuildLayout(viewportWidth, viewportHeight);
    }

    private (int width, int height) GetViewportSize() =>
        (
            Math.Max(1, _game.GraphicsDevice.Viewport.Width),
            Math.Max(1, _game.GraphicsDevice.Viewport.Height)
        );

    private void RebuildLayout(int viewportWidth, int viewportHeight)
    {
        _layoutWidth = viewportWidth;
        _layoutHeight = viewportHeight;
        GameSettings.SetScreenSize(viewportWidth, viewportHeight);

        int totalHeight = (ButtonHeight * 3) + (ButtonGap * 2);
        int startY = (viewportHeight - totalHeight) / 2;
        int startX = (viewportWidth - ButtonWidth) / 2;

        _startButton = CreateButtonRect(startX, startY, 0);
        _settingsButton = CreateButtonRect(startX, startY, 1);
        _exitButton = CreateButtonRect(startX, startY, 2);
    }

    private static Rectangle CreateButtonRect(int x, int startY, int index)
    {
        int y = startY + (index * (ButtonHeight + ButtonGap));
        return new Rectangle(x, y, ButtonWidth, ButtonHeight);
    }

    private void DrawButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        string label,
        bool isHovered,
        Color fill,
        Color hoverFill,
        Color border,
        Color hoverBorder
    )
    {
        TextureManager.DrawRect(spriteBatch, rect, isHovered ? hoverFill : fill);
        TextureManager.DrawRectOutline(spriteBatch, rect, isHovered ? hoverBorder : border, 3);

        if (_font == null)
            return;

        Vector2 textSize = _font.MeasureString(label);
        Vector2 textPos = new Vector2(
            rect.X + (rect.Width - textSize.X) / 2f,
            rect.Y + (rect.Height - textSize.Y) / 2f
        );

        spriteBatch.DrawString(_font, label, textPos + new Vector2(1, 1), Color.Black);
        spriteBatch.DrawString(_font, label, textPos, Color.White);
    }
}
