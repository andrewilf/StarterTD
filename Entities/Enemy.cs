using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Entities;

/// <summary>
/// Base enemy class that follows the path from start to end.
/// New enemy types can inherit from this or implement IEnemy directly.
/// </summary>
public class Enemy : IEnemy
{
    public string Name { get; }
    public float Health { get; private set; }
    public float MaxHealth { get; }
    public float Speed { get; }
    public int Bounty { get; }
    public Vector2 Position { get; private set; }
    public bool IsDead => Health <= 0;
    public bool ReachedEnd { get; private set; }
    public int AttackDamage { get; }

    private List<Point> _path;
    private int _currentPathIndex;
    private readonly Color _color;
    private const float SpriteSize = 20f;

    public Enemy(
        string name,
        float health,
        float speed,
        int bounty,
        List<Point> path,
        Color color,
        int attackDamage
    )
    {
        Name = name;
        Health = health;
        MaxHealth = health;
        Speed = speed;
        Bounty = bounty;
        _path = path;
        _currentPathIndex = 0;
        _color = color;
        AttackDamage = attackDamage;
        Position = Map.GridToWorld(_path[0]);
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health < 0)
            Health = 0;
    }

    /// <summary>
    /// Update the path this enemy follows (called when towers change in maze zones).
    /// Snaps the enemy to the closest waypoint on the new path to prevent diagonal cuts
    /// across obstacles, then continues from there.
    ///
    /// Think of it like: "You're between tiles on the old path. The new path goes
    /// a different way. Snap to your closest point on the new path and resume."
    /// </summary>
    public void UpdatePath(Map map)
    {
        if (IsDead || ReachedEnd)
            return;

        // Convert current world position to grid cell
        Point currentGridPos = Map.WorldToGrid(Position);

        // Compute a fresh path from the enemy's current grid position to the exit
        var freshPath = map.ComputePathFromPosition(currentGridPos);

        if (freshPath != null && freshPath.Count > 0)
        {
            _path = freshPath;
            _currentPathIndex = 1; // Start at index 1 since 0 is the current cell
        }
    }

    public void Update(GameTime gameTime)
    {
        if (IsDead || ReachedEnd)
            return;

        if (_currentPathIndex >= _path.Count)
        {
            ReachedEnd = true;
            return;
        }

        Vector2 target = Map.GridToWorld(_path[_currentPathIndex]);
        Vector2 direction = target - Position;
        float distance = direction.Length();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float moveAmount = Speed * dt;

        if (distance <= moveAmount)
        {
            Position = target;
            _currentPathIndex++;
        }
        else
        {
            direction.Normalize();
            Position += direction * moveAmount;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (IsDead)
            return;

        TextureManager.DrawSprite(
            spriteBatch,
            Position,
            new Vector2(SpriteSize, SpriteSize),
            _color
        );

        float healthBarWidth = SpriteSize;
        float healthBarHeight = 4f;
        float healthPercent = Health / MaxHealth;
        Vector2 barPos = new Vector2(
            Position.X - healthBarWidth / 2f,
            Position.Y - SpriteSize / 2f - 8f
        );

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle((int)barPos.X, (int)barPos.Y, (int)healthBarWidth, (int)healthBarHeight),
            Color.Red
        );

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(
                (int)barPos.X,
                (int)barPos.Y,
                (int)(healthBarWidth * healthPercent),
                (int)healthBarHeight
            ),
            Color.LimeGreen
        );
    }
}
