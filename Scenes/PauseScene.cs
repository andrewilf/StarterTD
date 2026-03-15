using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    private int _layoutWidth;
    private int _layoutHeight;
    private bool _isNavigatingAway;

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
            HandleResumeClicked();
    }

    public void Draw(SpriteBatch spriteBatch) { }

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
        var resumeButton = new Rectangle(centerX, startY, ButtonWidth, ButtonHeight);
        var mapSelectionButton = new Rectangle(
            centerX,
            startY + ButtonHeight + Gap,
            ButtonWidth,
            ButtonHeight
        );

        _gumView?.ResizeToViewport(viewportWidth, viewportHeight);
        _gumView?.UpdateLayout(resumeButton, mapSelectionButton);
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
}
