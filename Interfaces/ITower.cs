using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Interfaces;

/// <summary>
/// Interface for all tower types. Implement this to create new tower variants.
/// </summary>
public interface ITower
{
    /// <summary>Display name of the tower (e.g., "Gun Tower").</summary>
    string Name { get; }

    /// <summary>Grid position (column, row) where the tower is placed.</summary>
    Point GridPosition { get; }

    /// <summary>World-space center position of the tower in pixels.</summary>
    Vector2 WorldPosition { get; }

    /// <summary>Attack range in pixels.</summary>
    float Range { get; }

    /// <summary>Damage dealt per shot.</summary>
    float Damage { get; }

    /// <summary>Seconds between shots.</summary>
    float FireRate { get; }

    /// <summary>Cost to place this tower.</summary>
    int Cost { get; }

    /// <summary>Whether this tower deals area-of-effect damage.</summary>
    bool IsAOE { get; }

    /// <summary>Radius of AOE damage (0 if not AOE).</summary>
    float AOERadius { get; }

    /// <summary>Color used to render the placeholder sprite.</summary>
    Color TowerColor { get; }

    /// <summary>Maximum health points for this tower.</summary>
    int MaxHealth { get; }

    /// <summary>Current health points.</summary>
    int CurrentHealth { get; }

    /// <summary>Whether this tower has been destroyed (health &lt;= 0).</summary>
    bool IsDead { get; }

    /// <summary>Apply damage to this tower.</summary>
    void TakeDamage(int amount);

    /// <summary>Update tower logic (targeting, firing).</summary>
    void Update(GameTime gameTime, List<Interfaces.IEnemy> enemies);

    /// <summary>Draw the tower.</summary>
    void Draw(SpriteBatch spriteBatch, SpriteFont? font = null);
}
