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

    /// <summary>
    /// Canonical anchor position in half-tile units.
    /// 1x1 towers anchor to tile centers; 2x2 towers anchor to tile-corner intersections.
    /// </summary>
    public GridAnchor GridAnchor { get; private set; }

    /// <summary>
    /// Compatibility accessor: top-left tile of the tower footprint.
    /// </summary>
    public Point GridPosition => Map.AnchorToTopLeft(GridAnchor, FootprintSize);

    public Vector2 WorldPosition => Map.AnchorToWorld(GridAnchor);

    /// <summary>Visual position used for rendering. Interpolates smoothly during movement.</summary>
    public Vector2 DrawPosition => _drawPosition;
    public float Range { get; protected set; }
    public float Damage { get; protected set; }
    public float FireRate { get; protected set; }
    public float EffectiveFireInterval =>
        MathF.Max(FireRate / _externalAttackSpeedMultiplier, MinFireIntervalSeconds);
    public float BaseCooldown { get; }
    public float CooldownPenalty { get; }
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

    public Point FootprintSize { get; private set; }

    public Vector2 PlaceholderDrawSize { get; private set; }

    /// <summary>Scale factor for visual rendering.</summary>
    public Vector2 DrawScale { get; private set; }

    /// <summary>Final draw size in pixels after applying DrawScale.</summary>
    public Vector2 DrawSize =>
        new(PlaceholderDrawSize.X * DrawScale.X, PlaceholderDrawSize.Y * DrawScale.Y);

    /// <summary>All currently occupied grid tiles by this tower.</summary>
    public IReadOnlyList<Point> OccupiedTiles => _occupiedTiles;

    /// <summary>0→1 progress of post-move cooldown. 1 means ready to move again. Read by TowerDrawingHelper.</summary>
    public float MoveCooldownProgress => 1f - (_cooldownTimer / _cooldownDuration);

    public TowerType TowerType { get; }

    private CountdownTimer? _fireCooldown;
    private int _currentEngagedCount;
    private const float ProjectileSpeed = 400f;
    protected const float MinFireIntervalSeconds = 0.01f;

    private Vector2 _drawPosition;
    private Queue<Point> _movePath = new();
    private float _cooldownTimer;
    private float _cooldownDuration;

    private Point[] _occupiedTiles = [];

    protected float _abilityTimer;

    /// <summary>
    /// Duration of this tower's ability in seconds. Protected so subclasses can read it
    /// (e.g. WallingTower uses it as the slow duration applied to enemies on spike attack).
    /// </summary>
    protected float _abilityDuration;

    private float _originalDamage;
    private float _originalFireRate;
    private bool _hasStoredAbilityStats;
    private float _externalAttackSpeedMultiplier = 1f;
    private float _healingUltSparklePhase;

    private TargetingStrategy _targeting; // set once in ApplyStats; not readonly because ApplyStats is called after field init

    /// <summary>True while the super ability buff is active on this tower.</summary>
    public bool IsAbilityBuffActive { get; protected set; }
    public bool HasHealingUltAttackSpeedBuff { get; private set; }
    public float HealingUltSparklePhase => _healingUltSparklePhase;

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

        var stats = TowerData.GetStats(type);
        ApplyStats(stats);
        SetTopLeftGridPosition(gridPosition);
        _drawPosition = WorldPosition;

        BaseCooldown = stats.BaseCooldown;
        CooldownPenalty = stats.CooldownPenalty;
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
            TowerType.ChampionHealing => new HealingChampionTower(gridPos),
            _ => new Tower(type, gridPos),
        };

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth < 0)
            CurrentHealth = 0;
    }

    /// <summary>
    /// Restore tower HP by up to <paramref name="amount"/>, clamped to MaxHealth.
    /// Returns the actual HP restored.
    /// </summary>
    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead || CurrentHealth >= MaxHealth)
            return 0;

        int healed = Math.Min(amount, MaxHealth - CurrentHealth);
        CurrentHealth += healed;
        return healed;
    }

    public void SetHealingUltAttackSpeedBuff(bool isActive, float attackSpeedMultiplier)
    {
        float nextMultiplier = isActive ? MathF.Max(1f, attackSpeedMultiplier) : 1f;
        if (
            HasHealingUltAttackSpeedBuff == isActive
            && MathF.Abs(_externalAttackSpeedMultiplier - nextMultiplier) < 0.0001f
        )
            return;

        if (isActive)
        {
            _externalAttackSpeedMultiplier = nextMultiplier;
            HasHealingUltAttackSpeedBuff = true;
            return;
        }

        _externalAttackSpeedMultiplier = 1f;
        HasHealingUltAttackSpeedBuff = false;
    }

    protected void ResetFireCooldown()
    {
        _fireCooldown = null;
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

    /// <summary>
    /// True when the given tile is part of this tower's occupied footprint.
    /// </summary>
    public bool OccupiesTile(Point gridPos)
    {
        for (int i = 0; i < _occupiedTiles.Length; i++)
        {
            if (_occupiedTiles[i] == gridPos)
                return true;
        }

        return false;
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
        FootprintSize = stats.FootprintTiles;
        PlaceholderDrawSize = stats.PlaceholderDrawSize;
        DrawScale = stats.DrawScale;
        MoveSpeed = stats.MoveSpeed;
        _cooldownDuration = stats.CooldownDuration;
        _abilityDuration = stats.AbilityDuration;
        CanWalk = stats.CanWalk;
        _targeting = stats.Targeting;

        EnsureOccupiedTileBuffer();
    }

    private void EnsureOccupiedTileBuffer()
    {
        int needed = FootprintSize.X * FootprintSize.Y;
        if (_occupiedTiles.Length == needed)
            return;

        _occupiedTiles = new Point[needed];
    }

    private void SetTopLeftGridPosition(Point topLeft)
    {
        GridAnchor = Map.TopLeftToAnchor(topLeft, FootprintSize);

        int index = 0;
        for (int y = 0; y < FootprintSize.Y; y++)
        {
            for (int x = 0; x < FootprintSize.X; x++)
            {
                _occupiedTiles[index++] = new Point(topLeft.X + x, topLeft.Y + y);
            }
        }
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

        if (HasHealingUltAttackSpeedBuff)
        {
            _healingUltSparklePhase += dt;
            if (_healingUltSparklePhase >= 1000f)
                _healingUltSparklePhase -= 1000f;
        }
        else
        {
            _healingUltSparklePhase = 0f;
        }

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
            _fireCooldown = new CountdownTimer(EffectiveFireInterval);
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
    /// Updates anchor/grid position on cell arrival. Transitions to Cooldown when path exhausted.
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

        Point nextTopLeft = _movePath.Peek();
        Vector2 nextWorld = Map.AnchorToWorld(Map.TopLeftToAnchor(nextTopLeft, FootprintSize));

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 direction = nextWorld - _drawPosition;
        float distance = direction.Length();
        float moveAmount = MoveSpeed * dt;

        if (distance <= moveAmount)
        {
            _drawPosition = nextWorld;
            SetTopLeftGridPosition(nextTopLeft);
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
