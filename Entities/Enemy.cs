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

    private readonly List<Point> _path;
    private int _currentPathIndex;
    private readonly Color _color;
    private const float SpriteSize = 20f;

    public Enemy(string name, float health, float speed, int bounty, List<Point> path, Color color)
    {
        Name = name;
        Health = health;
        MaxHealth = health;
        Speed = speed;
        Bounty = bounty;
        _path = path;
        _currentPathIndex = 0;
        _color = color;

        // Start at the first path point
        Position = Map.GridToWorld(_path[0]);
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    public void Update(GameTime gameTime)
    {
        if (IsDead || ReachedEnd) return;

        // Move toward the next path point
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
            // Arrived at waypoint, move to next
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
        if (IsDead) return;

        // Draw enemy body (centered origin via DrawSprite)
        TextureManager.DrawSprite(spriteBatch, Position, new Vector2(SpriteSize, SpriteSize), _color);

        // Draw health bar above enemy
        float healthBarWidth = SpriteSize;
        float healthBarHeight = 4f;
        float healthPercent = Health / MaxHealth;
        Vector2 barPos = new Vector2(
            Position.X - healthBarWidth / 2f,
            Position.Y - SpriteSize / 2f - 8f);

        // Background (red)
        TextureManager.DrawRect(spriteBatch,
            new Rectangle((int)barPos.X, (int)barPos.Y, (int)healthBarWidth, (int)healthBarHeight),
            Color.Red);

        // Foreground (green)
        TextureManager.DrawRect(spriteBatch,
            new Rectangle((int)barPos.X, (int)barPos.Y, (int)(healthBarWidth * healthPercent), (int)healthBarHeight),
            Color.LimeGreen);
    }
}
