using System.Collections.Generic;
using System.Threading;
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
    private static int _nextStableId;

    public string Name { get; }
    public float Health { get; private set; }
    public float MaxHealth { get; }
    public float Speed { get; }
    public int Bounty { get; }
    public Vector2 Position => _basePosition + _crowdingOffset;
    public Vector2 BasePosition => _basePosition;
    public Vector2 CrowdingOffset => _crowdingOffset;
    public bool IsAttacking => _state == EnemyState.Attacking;
    public Tower? TargetTower => _targetTower;
    public Tower? BlockingTower => _blockingTower;
    public Vector2? BlockedSpreadTarget => _blockedSpreadTarget;
    public bool IsDead => Health <= 0;
    public bool ReachedEnd { get; private set; }
    public int AttackDamage { get; }
    public int StableId { get; }
    public Vector2 CrowdingVelocity => _crowdingVelocity;

    private List<Point> _path;
    private int _currentPathIndex;
    private readonly string _spawnName;
    private readonly Color _color;
    private const float SpriteSize = 30f;
    private EnemyState _state;
    private Tower? _targetTower;
    private Tower? _blockingTower;
    private CountdownTimer? _attackTimer;
    private const float AttackInterval = 1.0f;
    private Vector2 _basePosition;
    private Vector2 _crowdingOffset;
    private Vector2 _crowdingVelocity;
    private Vector2? _blockedSpreadTarget;
    private const float BlockedSpreadMoveSpeed = 70f;

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
        StableId = Interlocked.Increment(ref _nextStableId);
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
        _basePosition = Map.GridToWorld(_path[0]);
        _crowdingOffset = Vector2.Zero;
        _crowdingVelocity = Vector2.Zero;
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health < 0)
            Health = 0;
    }

    public void ApplySlow(float duration)
    {
        // Only refresh if the new duration exceeds what's already remaining.
        // Prevents a shorter slow (e.g. from a generic Walling tower) from cutting short
        // a longer slow already in progress (e.g. from the champion's 5s slow).
        if (_slowTimer < duration)
            _slowTimer = duration;
    }

    public void SetCrowdingOffset(Vector2 offset)
    {
        _crowdingOffset = offset;
    }

    public void SetCrowdingVelocity(Vector2 velocity)
    {
        _crowdingVelocity = velocity;
    }

    public void SetBlockedSpreadTarget(Vector2 worldTarget)
    {
        _blockedSpreadTarget = worldTarget;
    }

    public void ClearBlockedSpreadTarget()
    {
        _blockedSpreadTarget = null;
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
        Point currentGridPos = Map.WorldToGrid(_basePosition);

        // Compute a fresh path from the enemy's current grid position to the exit.
        // Pass _spawnName so ResolveExitName pairs it back to the correct exit (e.g. "spawn_a" → "exit_a").
        var freshPath = map.ComputePathFromPosition(currentGridPos, _spawnName);

        if (freshPath != null && freshPath.Count > 0)
        {
            _path = freshPath;
            _currentPathIndex = freshPath.Count > 1 ? 1 : freshPath.Count;

            // When path updates, release engagement and reset to Moving state
            // The tower we were attacking might be gone, or a new path opens
            if (_state == EnemyState.Attacking && _targetTower != null)
            {
                _targetTower.ReleaseEngagement();
            }
            _state = EnemyState.Moving;
            _targetTower = null;
            _blockingTower = null;
            _attackTimer = null;
            _blockedSpreadTarget = null;
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
                UpdateAttackingState(gameTime, map);
                break;
        }
    }

    private void UpdateMovingState(float dt, Map map)
    {
        if (_path.Count == 0 || _currentPathIndex < 0 || _currentPathIndex >= _path.Count)
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
            Tower? blockingTower = nextTile.OccupyingTower;

            if (blockingTower != null && !blockingTower.IsDead && blockingTower.TryEngage())
            {
                // Successfully engaged - switch to attacking
                _state = EnemyState.Attacking;
                _targetTower = blockingTower;
                _blockingTower = null;
                _attackTimer = new CountdownTimer(AttackInterval);
                _attackTimer.Start();
                _blockedSpreadTarget = null;
                return; // Stop moving this frame
            }

            // Tower is alive but block capacity is currently full.
            // Do not engage. Capacity is full, so this enemy walks on.
            if (blockingTower != null && !blockingTower.IsDead)
            {
                _blockingTower = null;
                _blockedSpreadTarget = null;
            }
        }

        _blockingTower = null;
        _blockedSpreadTarget = null;

        // Continue moving toward current waypoint
        Vector2 target = Map.GridToWorld(_path[_currentPathIndex]);
        Vector2 direction = target - _basePosition;
        float distance = direction.Length();
        float effectiveSpeed = Speed;
        if (IsSlowed)
            effectiveSpeed *= SlowFactor;
        float moveAmount = effectiveSpeed * dt;

        if (distance <= moveAmount)
        {
            _basePosition = target;
            _currentPathIndex++;
        }
        else
        {
            direction.Normalize();
            _basePosition += direction * moveAmount;
        }
    }

    private void UpdateAttackingState(GameTime gameTime, Map map)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Validate target still exists and is alive
        if (_targetTower?.IsDead ?? true)
        {
            // Tower destroyed or null - release engagement and resume movement
            _targetTower?.ReleaseEngagement();
            _state = EnemyState.Moving;
            _targetTower = null;
            _blockingTower = null;
            _attackTimer = null;
            _blockedSpreadTarget = null;
            _ = TryRecomputePathFromCurrentPosition(map);
            return;
        }

        // Under crowd pressure, attackers can step around the blocker perimeter
        // so backline enemies can flow to other tower fronts.
        _ = UpdateBlockedSpreadPosition(dt);

        // Update attack timer
        _attackTimer?.Update(gameTime);

        // Deal damage when timer completes
        if (_attackTimer != null && _attackTimer.State.HasFlag(TimerState.Completed))
        {
            _targetTower.TakeDamage(AttackDamage);
            _attackTimer.Restart();
        }
    }

    private bool UpdateBlockedSpreadPosition(float dt)
    {
        if (_blockedSpreadTarget is null)
            return false;

        Tower? spreadTower = _blockingTower ?? _targetTower;
        if (dt <= 0f || spreadTower == null || spreadTower.IsDead)
            return false;

        Vector2 target = _blockedSpreadTarget.Value;
        Vector2 delta = target - _basePosition;
        float distance = delta.Length();
        if (distance <= 0.75f)
        {
            _basePosition = target;
            return true;
        }

        float moveAmount = BlockedSpreadMoveSpeed * dt;
        if (distance <= moveAmount)
        {
            _basePosition = target;
            return true;
        }

        delta /= distance;
        _basePosition += delta * moveAmount;
        return false;
    }

    private bool TryRecomputePathFromCurrentPosition(Map map)
    {
        Point currentGridPos = Map.WorldToGrid(_basePosition);
        var freshPath = map.ComputePathFromPosition(currentGridPos, _spawnName);
        if (freshPath == null || freshPath.Count == 0)
            return false;

        _path = freshPath;
        _currentPathIndex = freshPath.Count > 1 ? 1 : freshPath.Count;
        return true;
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
            spriteColor,
            drawOutline: true
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
