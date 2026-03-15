using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Top-level start menu shown on launch.
/// Keeps the initial navigation separate from map selection.
/// </summary>
public class StartMenuScene : IScene
{
    private readonly Game1 _game;
    private StartMenuGumView? _gumView;
    private SpriteFont? _font;
    private int _layoutWidth;
    private int _layoutHeight;
    private bool _isNavigatingAway;
    private const int TitleTopMargin = 100;

    public StartMenuScene(Game1 game)
    {
        _game = game;
    }

    public void LoadContent()
    {
        try
        {
            _font = _game.Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available - menu falls back to shape-only rendering.
        }

        _isNavigatingAway = false;
        _gumView = new StartMenuGumView(
            onStartClicked: HandleStartClicked,
            onSettingsClicked: HandleSettingsClicked,
            onExitClicked: HandleExitClicked
        );

        var (viewportWidth, viewportHeight) = GetViewportSize();
        RebuildLayout(viewportWidth, viewportHeight);
        _gumView.AttachToRoot();
    }

    public void UnloadContent()
    {
        _gumView?.Dispose();
        _gumView = null;
    }

    public void Update(GameTime gameTime) => HandleViewportResize();

    public void Draw(SpriteBatch spriteBatch)
    {
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.ScreenHeight),
            new Color(16, 22, 32)
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
        _gumView?.ResizeToViewport(viewportWidth, viewportHeight);
    }

    private void HandleStartClicked()
    {
        if (_isNavigatingAway)
            return;

        _isNavigatingAway = true;
        _game.SetScene(new MapSelectionScene(_game));
    }

    private void HandleSettingsClicked()
    {
        // Settings screen is intentionally deferred for a later iteration.
    }

    private void HandleExitClicked()
    {
        if (_isNavigatingAway)
            return;

        _isNavigatingAway = true;
        _game.Exit();
    }
}
