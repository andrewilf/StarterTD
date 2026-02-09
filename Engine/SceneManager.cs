using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Interfaces;

namespace StarterTD.Engine;

/// <summary>
/// Manages scene transitions. Only one scene is active at a time.
/// Think of this like a React Router — it decides which "page" is rendered.
/// </summary>
public class SceneManager
{
    private IScene? _currentScene;

    /// <summary>The currently active scene.</summary>
    public IScene? CurrentScene => _currentScene;

    /// <summary>
    /// Switch to a new scene. Calls LoadContent on the new scene.
    /// </summary>
    public void SetScene(IScene scene)
    {
        _currentScene = scene;
        _currentScene.LoadContent();
    }

    /// <summary>Update the current scene.</summary>
    public void Update(GameTime gameTime)
    {
        _currentScene?.Update(gameTime);
    }

    /// <summary>Draw the current scene.</summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        _currentScene?.Draw(spriteBatch);
    }
}
