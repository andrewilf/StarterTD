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
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SceneManager _sceneManager = null!;

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
    /// Public method to allow scenes to transition to other scenes.
    /// </summary>
    public void SetScene(IScene scene)
    {
        _sceneManager.SetScene(scene);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Initialize the texture manager (creates the 1x1 white pixel)
        TextureManager.Initialize(GraphicsDevice);

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
            // Note: ESC is also used in-game to deselect towers.
            // Only exit if no scene is active or you add a menu scene.
            // For MVP, we let ESC deselect in GameplayScene and
            // don't exit the game via ESC (comment out Exit() below).
            // Exit();
        }

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

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
