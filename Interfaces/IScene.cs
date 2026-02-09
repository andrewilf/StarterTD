using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Interfaces;

/// <summary>
/// Interface for game scenes (e.g., MainMenu, Gameplay, GameOver).
/// </summary>
public interface IScene
{
    /// <summary>Called once when the scene is first loaded.</summary>
    void LoadContent();

    /// <summary>Called every frame to update scene logic.</summary>
    void Update(GameTime gameTime);

    /// <summary>Called every frame to draw the scene.</summary>
    void Draw(SpriteBatch spriteBatch);
}
