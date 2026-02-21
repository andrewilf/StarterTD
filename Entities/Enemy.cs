using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Timers;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Entities;

/// <summary>
/// Represents the current behavior state of an enemy.
/// </summary>
public enum EnemyState
{
    /// <summary>Enemy is moving along the path toward the exit.</summary>
    Moving,

    /// <summary>Enemy has encountered a tower and is attacking it.</summary>
    Attacking,
}

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
    private readonly string _spawnName;
    private readonly Color _color;
    private const float SpriteSize = 20f;
    private EnemyState _state;
    private Tower? _targetTower;
    private CountdownTimer? _attackTimer;
    private const float AttackInterval = 1.0f;

    private float _slowTimer;
    private const float SlowFactor = 0.4f; // Move at 40% of base speed while slowed

    public bool IsSlowed => _slowTimer > 0f;

    public Enemy(
        string name,
        float health,
        float speed,
        int bounty,
        List<Point> path,
        string spawnName,
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
        _spawnName = spawnName;
        _currentPathIndex = 0;
        _color = color;
        AttackDamage = attackDamage;
        _state = EnemyState.Moving;
        _attackTimer = null;
        Position = Map.GridToWorld(_path[0]);
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health < 0)
            Health = 0;
    }

    public void ApplySlow(float duration)
    {
        _slowTimer = duration;
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

        // Compute a fresh path from the enemy's current grid position to the exit.
        // Pass _spawnName so ResolveExitName pairs it back to the correct exit (e.g. "spawn_a" → "exit_a").
        var freshPath = map.ComputePathFromPosition(currentGridPos, _spawnName);

        if (freshPath != null && freshPath.Count > 0)
        {
            _path = freshPath;
            _currentPathIndex = 1; // Start at index 1 since 0 is the current cell

            // When path updates, release engagement and reset to Moving state
            // The tower we were attacking might be gone, or a new path opens
            if (_state == EnemyState.Attacking && _targetTower != null)
            {
                _targetTower.ReleaseEngagement();
            }
            _state = EnemyState.Moving;
            _targetTower = null;
            _attackTimer = null;
        }
    }

    /// <summary>
    /// Called when enemy is about to be removed. Releases tower engagement if in Attacking state.
    /// </summary>
    public void OnDestroy()
    {
        if (_state == EnemyState.Attacking && _targetTower != null)
        {
            _targetTower.ReleaseEngagement();
        }
    }

    public void Update(GameTime gameTime, Map map)
    {
        if (IsDead || ReachedEnd)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_slowTimer > 0f)
            _slowTimer -= dt;

        switch (_state)
        {
            case EnemyState.Moving:
                UpdateMovingState(dt, map);
                break;
            case EnemyState.Attacking:
                UpdateAttackingState(gameTime);
                break;
        }
    }

    private void UpdateMovingState(float dt, Map map)
    {
        if (_currentPathIndex >= _path.Count)
        {
            ReachedEnd = true;
            return;
        }

        // Check if next waypoint has a tower blocking the path
        Point nextWaypoint = _path[_currentPathIndex];

        if (
            nextWaypoint.X >= 0
            && nextWaypoint.X < map.Columns
            && nextWaypoint.Y >= 0
            && nextWaypoint.Y < map.Rows
        )
        {
            Tile nextTile = map.Tiles[nextWaypoint.X, nextWaypoint.Y];

            if (
                nextTile.OccupyingTower != null
                && !nextTile.OccupyingTower.IsDead
                && nextTile.OccupyingTower.TryEngage()
            )
            {
                // Successfully engaged - switch to attacking
                _state = EnemyState.Attacking;
                _targetTower = nextTile.OccupyingTower;
                _attackTimer = new CountdownTimer(AttackInterval);
                _attackTimer.Start();
                return; // Stop moving this frame
            }
            // Tower at capacity or no tower - continue moving
        }

        // Continue moving toward current waypoint
        Vector2 target = Map.GridToWorld(_path[_currentPathIndex]);
        Vector2 direction = target - Position;
        float distance = direction.Length();
        float effectiveSpeed = IsSlowed ? Speed * SlowFactor : Speed;
        float moveAmount = effectiveSpeed * dt;

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

    private void UpdateAttackingState(GameTime gameTime)
    {
        // Validate target still exists and is alive
        if (_targetTower?.IsDead ?? true)
        {
            // Tower destroyed or null - release engagement and resume movement
            _targetTower?.ReleaseEngagement();
            _state = EnemyState.Moving;
            _targetTower = null;
            _attackTimer = null;
            return;
        }

        // Update attack timer
        _attackTimer?.Update(gameTime);

        // Deal damage when timer completes
        if (_attackTimer != null && _attackTimer.State.HasFlag(TimerState.Completed))
        {
            _targetTower.TakeDamage(AttackDamage);
            _attackTimer.Restart();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (IsDead)
            return;

        // Use red tint when attacking, original color when moving; mix in blue when slowed
        Color spriteColor = _state == EnemyState.Attacking ? Color.Red : _color;
        if (IsSlowed)
            spriteColor = Color.Lerp(spriteColor, Color.CornflowerBlue, 0.5f);

        TextureManager.DrawSprite(
            spriteBatch,
            Position,
            new Vector2(SpriteSize, SpriteSize),
            spriteColor
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
