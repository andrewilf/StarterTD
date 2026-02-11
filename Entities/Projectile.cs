using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Entities;

/// <summary>
/// A projectile fired from a tower toward an enemy.
/// Moves in a straight line toward the target's position.
/// </summary>
public class Projectile
{
    public Vector2 Position { get; private set; }
    public float Speed { get; }
    public float Damage { get; }
    public bool IsAOE { get; }
    public float AOERadius { get; }
    public Color ProjectileColor { get; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Callback fired when this AoE projectile impacts (for visual effects).
    /// Passes the impact position and AoE radius. Bubbles up to Tower → TowerManager → GameplayScene.
    /// </summary>
    public Action<Vector2, float>? OnAOEImpact;

    private readonly IEnemy? _target;
    private Vector2 _targetPosition;
    private bool _targetLost = false;

    /// <summary>Size of the projectile sprite in pixels.</summary>
    private const float Size = 6f;

    /// <summary>How close the projectile must be to the target to "hit".</summary>
    private const float HitDistance = 10f;

    public Projectile(
        Vector2 startPosition,
        IEnemy target,
        float damage,
        float speed,
        bool isAOE,
        float aoeRadius,
        Color color
    )
    {
        Position = startPosition;
        _target = target;
        _targetPosition = target.Position;
        _targetLost = false;
        Damage = damage;
        Speed = speed;
        IsAOE = isAOE;
        AOERadius = aoeRadius;
        ProjectileColor = color;
    }

    /// <summary>
    /// Update projectile movement. Returns true if the projectile hit something.
    /// </summary>
    public bool Update(GameTime gameTime, List<IEnemy> allEnemies)
    {
        if (!IsActive)
            return false;

        // If target dies or escapes, lock onto last known position
        if (!_targetLost && (_target == null || _target.IsDead || _target.ReachedEnd))
        {
            _targetPosition = _target?.Position ?? _targetPosition;
            _targetLost = true;
        }

        // Move toward target or last known position
        Vector2 direction = _targetPosition - Position;
        float distance = direction.Length();

        if (distance < HitDistance)
        {
            // Only apply damage if target was alive when we fired
            if (!_targetLost && _target != null && !_target.IsDead)
            {
                if (IsAOE)
                {
                    // Damage all enemies in AOE radius
                    foreach (var enemy in allEnemies)
                    {
                        if (
                            !enemy.IsDead
                            && Vector2.Distance(Position, enemy.Position) <= AOERadius
                        )
                        {
                            enemy.TakeDamage(Damage);
                        }
                    }

                    // Fire AoE impact callback for visual effect
                    OnAOEImpact?.Invoke(Position, AOERadius);
                }
                else
                {
                    _target.TakeDamage(Damage);
                }
            }

            IsActive = false;
            return true;
        }

        // Normalize and move
        direction.Normalize();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Position += direction * Speed * dt;

        return false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;
        TextureManager.DrawSprite(spriteBatch, Position, new Vector2(Size, Size), ProjectileColor);
    }
}
