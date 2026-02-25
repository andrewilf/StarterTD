using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Timers;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Entities;

/// <summary>
/// Base tower class. Handles targeting, firing, movement, and ability buffs.
/// Subclasses add type-specific features: WallSegmentTower (growth/decay),
/// WallingTower (frenzy), CannonChampionTower (laser).
/// </summary>
public class Tower : ITower
{
    public string Name { get; private set; } = string.Empty;
    public TowerState CurrentState { get; private set; } = TowerState.Active;
    public Point GridPosition { get; private set; }
    public Vector2 WorldPosition => Map.GridToWorld(GridPosition);

    /// <summary>Visual position used for rendering. Interpolates smoothly during movement.</summary>
    public Vector2 DrawPosition => _drawPosition;
    public float Range { get; private set; }
    public float Damage { get; private set; }
    public float FireRate { get; private set; }
    public int Cost { get; }
    public bool IsAOE { get; private set; }
    public float AOERadius { get; private set; }
    public Color TowerColor { get; private set; }
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public bool IsDead => CurrentHealth <= 0;

    /// <summary>Movement speed in pixels per second when in Moving state.</summary>
    public float MoveSpeed { get; private set; }

    /// <summary>Whether this tower type is allowed to move on the map.</summary>
    public bool CanWalk { get; private set; }

    /// <summary>
    /// Fires when tower finishes its movement path and enters Cooldown.
    /// TowerManager uses this to re-add the tower to the tile grid.
    /// </summary>
    public Action? OnMovementComplete;

    /// <summary>Maximum number of enemies that can simultaneously attack this tower.</summary>
    public int BlockCapacity { get; private set; }

    /// <summary>Number of enemies currently attacking this tower.</summary>
    public int CurrentEngagedCount => _currentEngagedCount;

    /// <summary>Scale factor for visual rendering (X, Y). Generics default to (1.0, 1.0), Champions to (1.0, 1.5).</summary>
    public Vector2 DrawScale { get; private set; }

    /// <summary>0→1 progress of post-move cooldown. 1 means ready to move again. Read by TowerDrawingHelper.</summary>
    public float MoveCooldownProgress => 1f - (_cooldownTimer / _cooldownDuration);

    public TowerType TowerType { get; }

    private CountdownTimer? _fireCooldown;
    private int _currentEngagedCount;
    private const float ProjectileSpeed = 400f;

    private Vector2 _drawPosition;
    private Queue<Point> _movePath = new();
    private float _cooldownTimer;
    private float _cooldownDuration;

    protected float _abilityTimer;

    /// <summary>
    /// Duration of this tower's ability in seconds. Protected so subclasses can read it
    /// (e.g. WallingTower uses it as the slow duration applied to enemies on spike attack).
    /// </summary>
    protected float _abilityDuration;

    private float _originalDamage;
    private float _originalFireRate;
    private bool _hasStoredAbilityStats;

    private TargetingStrategy _targeting; // set once in ApplyStats; not readonly because ApplyStats is called after field init

    /// <summary>True while the super ability buff is active on this tower.</summary>
    public bool IsAbilityBuffActive { get; protected set; }

    /// <summary>The last enemy this tower successfully targeted. Used by laser activation to aim the initial beam.</summary>
    public IEnemy? LastTarget { get; private set; }

    /// <summary>List of active projectiles fired by this tower.</summary>
    public List<Projectile> Projectiles { get; } = new();

    /// <summary>
    /// Callback fired when an AoE projectile from this tower impacts (for visual effects).
    /// Passes the impact position and AoE radius. Bubbles up to TowerManager → GameplayScene.
    /// </summary>
    public Action<Vector2, float>? OnAOEImpact;

    /// <summary>
    /// If set, replaces circular range targeting with wall-network targeting.
    /// Returns the best enemy to attack from within the wall attack zone.
    /// Set each frame by TowerManager for walling towers.
    /// </summary>
    public Func<List<IEnemy>, IEnemy?>? WallNetworkTargetFinder;

    /// <summary>
    /// Fires with the target's world position when a wall spike attack lands.
    /// Bubbles up to TowerManager → GameplayScene to spawn SpikeEffect.
    /// </summary>
    public Action<Vector2>? OnWallAttack;

    /// <summary>
    /// Total HP capacity used for health bar rendering.
    /// Overridden by WallSegmentTower to return the growth target cap.
    /// </summary>
    public virtual int HealthBarCapacity => MaxHealth;

    /// <summary>
    /// When true, suppresses normal projectile firing in UpdateActive.
    /// Overridden by CannonChampionTower to return IsLaserActive.
    /// </summary>
    protected virtual bool IsFiringSuppressed => false;

    public Tower(TowerType type, Point gridPosition)
    {
        TowerType = type;
        GridPosition = gridPosition;
        _drawPosition = Map.GridToWorld(gridPosition);

        var stats = TowerData.GetStats(type);
        ApplyStats(stats);
        Cost = stats.Cost;
    }

    /// <summary>
    /// Factory method: creates the correct Tower subclass for the given type.
    /// Use this instead of new Tower(...) to ensure subclass-specific behaviour is active.
    /// </summary>
    public static Tower Create(TowerType type, Point gridPos) =>
        type switch
        {
            TowerType.WallSegment => new WallSegmentTower(gridPos),
            TowerType.Walling => new WallingTower(type, gridPos),
            TowerType.ChampionWalling => new WallingTower(type, gridPos),
            TowerType.ChampionCannon => new CannonChampionTower(gridPos),
            _ => new Tower(type, gridPos),
        };

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0)
            CurrentHealth = 0;
    }

    /// <summary>
    /// Attempt to engage this tower. Returns true if the enemy can engage (capacity available).
    /// </summary>
    public bool TryEngage()
    {
        if (_currentEngagedCount < BlockCapacity)
        {
            _currentEngagedCount++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Release an engagement slot when an enemy stops attacking.
    /// </summary>
    public void ReleaseEngagement()
    {
        if (_currentEngagedCount > 0)
            _currentEngagedCount--;
    }

    private void ApplyStats(TowerStats stats)
    {
        Name = stats.Name;
        Range = stats.Range;
        Damage = stats.Damage;
        FireRate = stats.FireRate;
        IsAOE = stats.IsAOE;
        AOERadius = stats.AOERadius;
        TowerColor = stats.Color;
        MaxHealth = stats.MaxHealth;
        CurrentHealth = stats.MaxHealth;
        BlockCapacity = stats.BlockCapacity;
        DrawScale = stats.DrawScale;
        MoveSpeed = stats.MoveSpeed;
        _cooldownDuration = stats.CooldownDuration;
        _abilityDuration = stats.AbilityDuration;
        CanWalk = stats.CanWalk;
        _targeting = stats.Targeting;
    }

    /// <summary>
    /// Apply a temporary buff to this tower's damage and fire rate.
    /// damageMult scales damage (2f = double). fireRateSpeedMult speeds up attack (1.4f = 40% faster).
    /// Safe to call while already active — resets the timer but does not stack multipliers.
    /// </summary>
    public void ActivateAbilityBuff(float damageMult, float fireRateSpeedMult)
    {
        if (!IsAbilityBuffActive || !_hasStoredAbilityStats)
        {
            _originalDamage = Damage;
            _originalFireRate = FireRate;
            _hasStoredAbilityStats = true;
        }
        else
        {
            // Already buffed: restore originals before re-applying to avoid stacking
            Damage = _originalDamage;
            FireRate = _originalFireRate;
        }

        Damage *= damageMult;
        // Lower FireRate value = faster attack (seconds between shots)
        FireRate /= fireRateSpeedMult;
        IsAbilityBuffActive = true;
        _abilityTimer = _abilityDuration;
    }

    public void CancelAbility()
    {
        if (IsAbilityBuffActive)
            DeactivateAbilityBuff();
    }

    private void DeactivateAbilityBuff()
    {
        if (_hasStoredAbilityStats)
        {
            Damage = _originalDamage;
            FireRate = _originalFireRate;
            _hasStoredAbilityStats = false;
        }

        IsAbilityBuffActive = false;
        _abilityTimer = 0f;

        OnAbilityDeactivated();
    }

    /// <summary>
    /// Called at the end of DeactivateAbilityBuff. Subclasses override to clear their own state
    /// (e.g. CannonChampionTower clears IsLaserActive here).
    /// </summary>
    protected virtual void OnAbilityDeactivated() { }

    public void Update(GameTime gameTime, List<IEnemy> enemies)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        OnUpdateStart(dt);

        if (IsAbilityBuffActive)
        {
            _abilityTimer -= dt;
            if (_abilityTimer <= 0f)
                DeactivateAbilityBuff();
        }

        if (CurrentState == TowerState.Moving)
        {
            UpdateMovement(gameTime);
        }
        else
        {
            // Tower can fire in both Active and Cooldown states
            UpdateActive(gameTime, enemies);

            if (CurrentState == TowerState.Cooldown)
                UpdateCooldown(gameTime);
        }

        // Projectiles update regardless of state so in-flight shots still resolve
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            Projectiles[i].Update(gameTime, enemies);
            if (!Projectiles[i].IsActive)
                Projectiles.RemoveAt(i);
        }
    }

    /// <summary>
    /// Called at the start of each Update tick before ability/movement/firing logic.
    /// Overridden by WallSegmentTower to run wall growth each frame.
    /// </summary>
    protected virtual void OnUpdateStart(float dt) { }

    /// <summary>
    /// Normal targeting and firing logic. Only runs when CurrentState == Active.
    /// Skipped entirely for towers with no range and no wall targeting delegate,
    /// to avoid passing float.MaxValue FireRate into CountdownTimer (TimeSpan overflow).
    /// Wall-network targeting (WallingTower) uses WallNetworkTargetFinder instead of circular range,
    /// and deals instant spike damage rather than spawning a projectile.
    /// </summary>
    private void UpdateActive(GameTime gameTime, List<IEnemy> enemies)
    {
        bool hasWallTargeting = WallNetworkTargetFinder != null;
        if (Range <= 0f && !hasWallTargeting)
            return;

        _fireCooldown?.Update(gameTime);

        IEnemy? target = hasWallTargeting
            ? WallNetworkTargetFinder!(enemies)
            : SelectTarget(enemies);

        bool canFire = _fireCooldown == null || _fireCooldown.State.HasFlag(TimerState.Completed);

        if (target != null)
            LastTarget = target;

        if (target != null && canFire && !IsFiringSuppressed)
        {
            _fireCooldown = new CountdownTimer(FireRate);
            _fireCooldown.Start();

            if (hasWallTargeting)
            {
                // Instant spike damage — no projectile; visual spawned via OnWallAttack callback
                target.TakeDamage((int)Damage);
                target.ApplySlow(_abilityDuration);
                OnWallAttack?.Invoke(target.Position);
            }
            else
            {
                var projectile = new Projectile(
                    WorldPosition,
                    target,
                    Damage,
                    ProjectileSpeed,
                    IsAOE,
                    AOERadius,
                    Color.Yellow
                );

                // Wire up projectile's AoE impact callback to bubble up to TowerManager → GameplayScene
                projectile.OnAOEImpact = (pos, radius) => OnAOEImpact?.Invoke(pos, radius);

                Projectiles.Add(projectile);
            }
        }
    }

    private IEnemy? SelectTarget(List<IEnemy> enemies) =>
        _targeting switch
        {
            TargetingStrategy.LowestHP => FindLowestHP(enemies),
            TargetingStrategy.MostGrouped => FindMostGrouped(enemies),
            _ => FindClosest(enemies),
        };

    private IEnemy? FindClosest(List<IEnemy> enemies)
    {
        IEnemy? target = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead || enemy.ReachedEnd)
                continue;

            float dist = Vector2.Distance(WorldPosition, enemy.Position);
            if (dist <= Range && dist < closestDist)
            {
                closestDist = dist;
                target = enemy;
            }
        }

        return target;
    }

    // Among all alive enemies in range, pick the one with the lowest current HP.
    // Finishing weak enemies reduces total threat count faster than pure DPS optimisation.
    private IEnemy? FindLowestHP(List<IEnemy> enemies)
    {
        IEnemy? target = null;
        float lowestHP = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy.IsDead || enemy.ReachedEnd)
                continue;

            float dist = Vector2.Distance(WorldPosition, enemy.Position);
            if (dist <= Range && enemy.Health < lowestHP)
            {
                lowestHP = enemy.Health;
                target = enemy;
            }
        }

        return target;
    }

    // For each alive enemy in range, count how many other alive enemies sit within AOERadius
    // of that enemy's position. Pick the candidate with the highest neighbour count.
    // Tie-break: lowest HP (mirrors LowestHP priority for consistency).
    // O(n * m) per call where n = in-range enemies, m = total enemies; acceptable at <100 enemies.
    private IEnemy? FindMostGrouped(List<IEnemy> enemies)
    {
        IEnemy? target = null;
        int bestCount = -1;
        float bestHP = float.MaxValue;

        foreach (var candidate in enemies)
        {
            if (candidate.IsDead || candidate.ReachedEnd)
                continue;

            float dist = Vector2.Distance(WorldPosition, candidate.Position);
            if (dist > Range)
                continue;

            int neighbourCount = 0;
            foreach (var other in enemies)
            {
                if (other.IsDead || other.ReachedEnd)
                    continue;

                if (Vector2.Distance(candidate.Position, other.Position) <= AOERadius)
                    neighbourCount++;
            }

            if (
                neighbourCount > bestCount
                || (neighbourCount == bestCount && candidate.Health < bestHP)
            )
            {
                bestCount = neighbourCount;
                bestHP = candidate.Health;
                target = candidate;
            }
        }

        return target;
    }

    /// <summary>
    /// Begin moving along a path. TowerPathfinder includes the start cell as the first
    /// element, so we dequeue it immediately since the tower is already there.
    /// </summary>
    public void StartMoving(Queue<Point> path)
    {
        _movePath = path;

        // Discard start cell (tower is already standing on it)
        if (_movePath.Count > 0)
            _movePath.Dequeue();

        if (_movePath.Count == 0)
            return;

        CurrentState = TowerState.Moving;
        _drawPosition = WorldPosition;
    }

    /// <summary>
    /// Smoothly interpolates the tower's visual position along the path queue.
    /// Updates GridPosition on arrival at each cell. Transitions to Cooldown when path exhausted.
    /// </summary>
    private void UpdateMovement(GameTime gameTime)
    {
        if (_movePath.Count == 0)
        {
            _drawPosition = WorldPosition;
            CurrentState = TowerState.Cooldown;
            _cooldownTimer = _cooldownDuration;
            OnMovementComplete?.Invoke();
            return;
        }

        Point nextGrid = _movePath.Peek();
        Vector2 nextWorld = Map.GridToWorld(nextGrid);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 direction = nextWorld - _drawPosition;
        float distance = direction.Length();
        float moveAmount = MoveSpeed * dt;

        if (distance <= moveAmount)
        {
            _drawPosition = nextWorld;
            GridPosition = nextGrid;
            _movePath.Dequeue();
        }
        else
        {
            direction.Normalize();
            _drawPosition += direction * moveAmount;
        }
    }

    /// <summary>
    /// Post-move delay before the tower can move again. Tower can still fire during cooldown.
    /// </summary>
    private void UpdateCooldown(GameTime gameTime)
    {
        _cooldownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = 0f;
            CurrentState = TowerState.Active;
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont? font = null) =>
        TowerDrawingHelper.Draw(spriteBatch, this);

    public void DrawRangeIndicator(SpriteBatch spriteBatch) =>
        TowerDrawingHelper.DrawRangeIndicator(spriteBatch, this);

    /// <summary>
    /// Hook for future debuff system. Called when a champion's alive state changes.
    /// Generic towers can override this to respond to champion death/revival.
    /// </summary>
    public virtual void UpdateChampionStatus(bool isChampionAlive)
    {
        // Empty implementation for now
        // Future: Apply stat debuffs when isChampionAlive = false
    }
}
