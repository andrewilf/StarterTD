using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SceneManager _sceneManager = null!;
    private SpriteFont? _debugFont;

    // FPS counter: simple exponential moving average
    private double _averageFps;
    private const double FpsSmoothingFactor = 0.1;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Set window size
        _graphics.PreferredBackBufferWidth = GameSettings.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameSettings.ScreenHeight;
        _graphics.ApplyChanges();

        Window.Title = "StarterTD — Tower Defense";

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

        // Set up the scene manager and load the map selection scene
        _sceneManager = new SceneManager();
        _sceneManager.SetScene(new MapSelectionScene(this));
    }

    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
        {
            // ESC is used in-game to deselect towers, so we don't exit here
        }

        UpdateFPS(gameTime);
        _sceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null,
            null,
            null,
            null
        );

        _sceneManager.Draw(_spriteBatch);

        // Draw FPS counter in top-left corner
        DrawFPSCounter(_spriteBatch);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    /// <summary>
    /// Update FPS counter using exponential moving average.
    /// Smooths FPS reading without storing frame history.
    /// </summary>
    private void UpdateFPS(GameTime gameTime)
    {
        double elapsed = gameTime.ElapsedGameTime.TotalSeconds;
        if (elapsed > 0)
        {
            double currentFps = 1.0 / elapsed;
            // Exponential moving average: new_avg = current * factor + old_avg * (1 - factor)
            _averageFps =
                (currentFps * FpsSmoothingFactor) + (_averageFps * (1 - FpsSmoothingFactor));
        }
    }

    /// <summary>
    /// Draw the FPS counter in the top-left corner.
    /// Uses font if available, otherwise uses a colored rectangle indicator.
    /// </summary>
    private void DrawFPSCounter(SpriteBatch spriteBatch)
    {
        string fpsText = $"FPS: {_averageFps:0}";

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
            int fps = (int)_averageFps;
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
