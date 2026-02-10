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
    /// Finds the enemy's current grid cell in the new path and continues from there.
    ///
    /// Think of it like: "You're on tile (5,4). The new path also goes through (5,4)
    /// at index 12. So set your next waypoint to index 12 and keep going."
    /// </summary>
    public void UpdatePath(List<Point> newPath)
    {
        if (IsDead || ReachedEnd)
            return;

        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < newPath.Count; i++)
        {
            Vector2 pointWorld = Map.GridToWorld(newPath[i]);
            float dist = Vector2.Distance(Position, pointWorld);

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestIndex = i;
            }
        }

        if (bestIndex == -1)
            bestIndex = 0;

        _path = newPath;

        if (bestDistance < GameSettings.TileSize / 2f && bestIndex < newPath.Count - 1)
            _currentPathIndex = bestIndex + 1;
        else
            _currentPathIndex = bestIndex;
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
