using System;
using System.Runtime.InteropServices;
using Gum.Forms;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using StarterTD.Engine;
using StarterTD.Interfaces;
using StarterTD.Scenes;

namespace StarterTD;

/// <summary>
/// Entry point for the MonoGame application.
/// Sets up the window, initializes the TextureManager,
/// and delegates to the SceneManager for game logic.
/// </summary>
public class Game1 : Game
{
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_MaximizeWindow(IntPtr window);

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SceneManager _sceneManager = null!;
    private SpriteFont? _debugFont;

    private double _displayedFps;
    private int _fpsSampleFrames;
    private TimeSpan _fpsSampleElapsed = TimeSpan.Zero;
    private static readonly TimeSpan FpsSampleWindow = TimeSpan.FromMilliseconds(500);
    private bool _isApplyingClientResize;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GameSettings.Initialize(GraphicsDevice);

        // Start in windowed mode and ask the OS to maximize the window so
        // native controls (close/minimize/move) remain available.
        Window.IsBorderless = false;
        Window.AllowUserResizing = true;
        _graphics.IsFullScreen = false;
        _graphics.HardwareModeSwitch = false;
        SetBackBufferSize(GameSettings.ScreenWidth, GameSettings.ScreenHeight);

        TryMaximizeWindow();

        Rectangle client = Window.ClientBounds;
        if (client.Width > 0 && client.Height > 0)
        {
            SetBackBufferSize(client.Width, client.Height);
            GameSettings.SetScreenSize(client.Width, client.Height);
        }

        Window.Title = "StarterTD — Tower Defense";
        Window.ClientSizeChanged += HandleClientSizeChanged;

        base.Initialize();
    }

    /// <summary>
    /// Replace the current scene (clears the stack). Used for full transitions.
    /// </summary>
    public void SetScene(IScene scene)
    {
        _sceneManager.SetScene(scene);
    }

    /// <summary>
    /// Replace the current scene using a timed transition.
    /// </summary>
    public void TransitionToScene(
        IScene scene,
        SceneTransitionPreset preset = SceneTransitionPreset.MenuForwardSlideFade
    )
    {
        _sceneManager.TransitionToScene(scene, preset);
    }

    /// <summary>
    /// Push a scene on top (e.g., pause menu overlay). Preserves underlying scene.
    /// </summary>
    public void PushScene(IScene scene)
    {
        _sceneManager.PushScene(scene);
    }

    /// <summary>
    /// Pop the top scene (e.g., close pause menu). Resumes underlying scene.
    /// </summary>
    public void PopScene()
    {
        _sceneManager.PopScene();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Initialize the texture manager (creates the 1x1 white pixel and loads terrain tileset)
        TextureManager.Initialize(GraphicsDevice, Content);

        // Try to load debug font (same font used for UI)
        try
        {
            _debugFont = Content.Load<SpriteFont>("DefaultFont");
        }
        catch
        {
            // Font not available - FPS will use fallback rendering
        }

        InitializeGum();

        // Set up the scene manager and load the start menu scene.
        _sceneManager = new SceneManager(GraphicsDevice);
        _sceneManager.SetScene(new StartMenuScene(this));
    }

    protected override void Update(GameTime gameTime)
    {
        if (GumService.Default.IsInitialized)
            GumService.Default.Update(gameTime);

        _sceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void UnloadContent()
    {
        Window.ClientSizeChanged -= HandleClientSizeChanged;

        if (GumService.Default.IsInitialized)
            GumService.Default.Uninitialize();

        base.UnloadContent();
    }

    private void TryMaximizeWindow()
    {
        try
        {
            SDL_MaximizeWindow(Window.Handle);
        }
        catch (DllNotFoundException)
        {
            // SDL symbol unavailable on this platform/runtime; continue with current size.
        }
        catch (EntryPointNotFoundException)
        {
            // SDL symbol unavailable on this platform/runtime; continue with current size.
        }
    }

    /// <summary>
    /// Keep backbuffer sizing in one place so startup/window-mode transitions stay consistent.
    /// </summary>
    private void SetBackBufferSize(int width, int height)
    {
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();
    }

    private void InitializeGum()
    {
        GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
        SyncGumCanvasToViewport();
    }

    private void HandleClientSizeChanged(object? sender, EventArgs e)
    {
        if (_isApplyingClientResize)
            return;

        Rectangle client = Window.ClientBounds;
        if (client.Width <= 0 || client.Height <= 0)
            return;

        _isApplyingClientResize = true;
        try
        {
            SetBackBufferSize(client.Width, client.Height);
            GameSettings.SetScreenSize(client.Width, client.Height);
            SyncGumCanvasToViewport();
        }
        finally
        {
            _isApplyingClientResize = false;
        }
    }

    private void SyncGumCanvasToViewport()
    {
        if (!GumService.Default.IsInitialized)
            return;

        int width = Math.Max(1, GraphicsDevice.Viewport.Width);
        int height = Math.Max(1, GraphicsDevice.Viewport.Height);

        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        GumService.Default.CanvasWidth = width;
        GumService.Default.CanvasHeight = height;
        GumService.Default.Root.UpdateLayout();
        GumService.Default.Root.UpdateToFontValues();
    }

    protected override void Draw(GameTime gameTime)
    {
        UpdateFPS(gameTime);
        GraphicsDevice.Clear(Color.Black);

        _sceneManager.Draw(_spriteBatch);

        // Draw Gum once per frame from a single owner to avoid scene-level draw coupling.
        if (GumService.Default.IsInitialized)
            GumService.Default.Draw();

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            null
        );

        // Draw FPS counter in top-left corner
        DrawFPSCounter(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Update the FPS counter from rendered frames, not update ticks.
    /// This keeps the value honest when the fixed-step loop runs catch-up updates.
    /// </summary>
    private void UpdateFPS(GameTime gameTime)
    {
        _fpsSampleElapsed += gameTime.ElapsedGameTime;
        _fpsSampleFrames++;

        if (_fpsSampleElapsed >= FpsSampleWindow)
        {
            _displayedFps = _fpsSampleFrames / _fpsSampleElapsed.TotalSeconds;
            _fpsSampleElapsed = TimeSpan.Zero;
            _fpsSampleFrames = 0;
        }
    }

    /// <summary>
    /// Draw the FPS counter in the top-left corner.
    /// Uses font if available, otherwise uses a colored rectangle indicator.
    /// </summary>
    private void DrawFPSCounter(SpriteBatch spriteBatch)
    {
        string fpsText = $"FPS: {_displayedFps:0}";

        if (_debugFont != null)
        {
            // Draw with shadow for readability
            Vector2 position = new Vector2(10, 10);
            spriteBatch.DrawString(_debugFont, fpsText, position + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(_debugFont, fpsText, position, Color.Lime);
        }
        else
        {
            // Fallback: simple colored rectangle (green = good FPS, yellow = ok, red = bad)
            int fps = (int)_displayedFps;
            Color indicatorColor;
            if (fps >= 50)
                indicatorColor = Color.Lime;
            else if (fps >= 30)
                indicatorColor = Color.Yellow;
            else
                indicatorColor = Color.Red;

            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(10, 10, 60, 20),
                indicatorColor * 0.7f
            );
        }
    }
}
