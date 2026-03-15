using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.Managers;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Pause menu overlay scene. Displays pause UI and handles resume/menu navigation.
/// Sits on top of GameplayScene using the scene stack.
/// </summary>
public class PauseScene : IScene
{
    private readonly Game1 _game;
    private InputManager _inputManager = null!;
    private PauseMenuGumView? _gumView;
    private SpriteFont? _font;
    private int _layoutWidth;
    private int _layoutHeight;
    private bool _isNavigatingAway;

    private Rectangle _resumeButton;
    private Rectangle _mapSelectionButton;

    private const int ButtonWidth = 200;
    private const int ButtonHeight = 60;
    private const int Gap = 20;

    public PauseScene(Game1 game)
    {
        _game = game;
    }

    public void LoadContent()
    {
        _inputManager = new InputManager();
        _isNavigatingAway = false;

        try
        {
            _font = _game.Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available — UI will use fallback rendering
        }

        _gumView = new PauseMenuGumView(
            onResumeClicked: HandleResumeClicked,
            onMapSelectionClicked: HandleMapSelectionClicked
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

    public void Update(GameTime gameTime)
    {
        _inputManager.Update();
        HandleViewportResize();

        if (_inputManager.IsKeyPressed(Keys.Escape) || _inputManager.IsKeyPressed(Keys.P))
        {
            HandleResumeClicked();
            return;
        }

        // Fallback input path if Gum is unavailable.
        if (_inputManager.IsLeftClick() && !GumService.Default.IsInitialized)
        {
            Point mousePos = _inputManager.MousePosition;

            if (_resumeButton.Contains(mousePos))
            {
                HandleResumeClicked();
                return;
            }

            if (_mapSelectionButton.Contains(mousePos))
                HandleMapSelectionClicked();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (GumService.Default.IsInitialized)
            return;

        // Fallback when Gum is unavailable.
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.ScreenHeight),
            Color.Black * 0.4f
        );
        DrawButton(spriteBatch, _resumeButton, "Resume (P/ESC)");
        DrawButton(spriteBatch, _mapSelectionButton, "Map Selection");
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

        int centerX = viewportWidth / 2 - ButtonWidth / 2;
        int startY = viewportHeight / 2 - 80;
        _resumeButton = new Rectangle(centerX, startY, ButtonWidth, ButtonHeight);
        _mapSelectionButton = new Rectangle(
            centerX,
            startY + ButtonHeight + Gap,
            ButtonWidth,
            ButtonHeight
        );

        _gumView?.ResizeToViewport(viewportWidth, viewportHeight);
        _gumView?.UpdateLayout(_resumeButton, _mapSelectionButton);
    }

    private void HandleResumeClicked()
    {
        _game.PopScene();
    }

    private void HandleMapSelectionClicked()
    {
        if (_isNavigatingAway)
            return;

        _isNavigatingAway = true;
        _game.SetScene(new MapSelectionScene(_game));
    }

    private void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string label)
    {
        TextureManager.DrawRect(spriteBatch, rect, Color.DarkSlateGray);
        TextureManager.DrawRectOutline(spriteBatch, rect, Color.White, 2);

        if (_font != null)
        {
            Vector2 textSize = _font.MeasureString(label);
            spriteBatch.DrawString(
                _font,
                label,
                new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2,
                    rect.Y + (rect.Height - textSize.Y) / 2
                ),
                Color.White
            );
        }
    }
}
