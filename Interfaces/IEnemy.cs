using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Interfaces;

/// <summary>
/// Interface for all enemy types. Implement this to create new enemy variants.
/// </summary>
public interface IEnemy
{
    /// <summary>Display name of the enemy type.</summary>
    string Name { get; }

    /// <summary>Current health points.</summary>
    float Health { get; }

    /// <summary>Maximum health points.</summary>
    float MaxHealth { get; }

    /// <summary>Movement speed in pixels per second.</summary>
    float Speed { get; }

    /// <summary>Money awarded to the player when this enemy is killed.</summary>
    int Bounty { get; }

    /// <summary>Current world-space position in pixels.</summary>
    Vector2 Position { get; }

    /// <summary>Whether this enemy has been killed (health <= 0).</summary>
    bool IsDead { get; }

    /// <summary>Whether this enemy has reached the end of the path.</summary>
    bool ReachedEnd { get; }

    /// <summary>Damage this enemy deals to towers (data only, no attack AI yet).</summary>
    int AttackDamage { get; }

    /// <summary>Apply damage to this enemy.</summary>
    void TakeDamage(float amount);

    /// <summary>Update enemy movement along the path.</summary>
    void Update(GameTime gameTime);

    /// <summary>Draw the enemy.</summary>
    void Draw(SpriteBatch spriteBatch);
}
