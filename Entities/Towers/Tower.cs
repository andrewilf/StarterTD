using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Timers;
using StarterTD.Engine;
using StarterTD.Interfaces;

namespace StarterTD.Entities;

/// <summary>
/// Concrete tower implementation. Targets the nearest enemy in range
/// and fires projectiles at it.
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
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    /// <summary>Movement speed in pixels per second when in Moving state.</summary>
    public float MoveSpeed { get; private set; }

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

    public TowerType TowerType { get; }

    private CountdownTimer? _fireCooldown;
    private int _currentEngagedCount;
    private const float SpriteSize = 30f;
    private const float ProjectileSpeed = 400f;

    private Vector2 _drawPosition;
    private Queue<Point> _movePath = new();
    private float _cooldownTimer;
    private float _cooldownDuration;

    /// <summary>List of active projectiles fired by this tower.</summary>
    public List<Projectile> Projectiles { get; } = new();

    /// <summary>
    /// Callback fired when an AoE projectile from this tower impacts (for visual effects).
    /// Passes the impact position and AoE radius. Bubbles up to TowerManager → GameplayScene.
    /// </summary>
    public Action<Vector2, float>? OnAOEImpact;

    public Tower(TowerType type, Point gridPosition)
    {
        TowerType = type;
        GridPosition = gridPosition;
        _drawPosition = Map.GridToWorld(gridPosition);

        var stats = TowerData.GetStats(type);
        ApplyStats(stats);
        Cost = stats.Cost;
    }

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

    private void DrawMoveCooldownBar(SpriteBatch spriteBatch, Vector2 drawPosition)
    {
        float barWidth = SpriteSize;
        float barHeight = 3f;
        // Fills from 0% (just arrived) to 100% (ready to move again)
        float progress = 1f - (_cooldownTimer / _cooldownDuration);

        int barX = (int)(drawPosition.X - barWidth / 2f);
        // Position below capacity bar: health bar is at -8f offset, capacity at -3f, this at +2f
        int barY = (int)(drawPosition.Y - (SpriteSize * DrawScale.Y) / 2f - 8f + 10f);

        // Dark gray background
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, (int)barWidth, (int)barHeight),
            Color.DarkGray
        );

        // Yellow foreground (progress toward move-ready)
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, (int)(barWidth * progress), (int)barHeight),
            Color.Gold
        );
    }

    private void DrawCapacityBar(SpriteBatch spriteBatch, Vector2 drawPosition)
    {
        float capacityBarWidth = SpriteSize;
        float capacityBarHeight = 3f; // Slightly thinner than health bar
        float remainingPercent = (float)(BlockCapacity - CurrentEngagedCount) / BlockCapacity;

        int capBarX = (int)(drawPosition.X - capacityBarWidth / 2f);
        // Scale Y offset by DrawScale.Y so bars stay at top of scaled tower
        int capBarY = (int)(drawPosition.Y - (SpriteSize * DrawScale.Y) / 2f - 8f + 5f); // 5px below health bar

        // Dark gray background (full bar)
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(capBarX, capBarY, (int)capacityBarWidth, (int)capacityBarHeight),
            Color.DarkGray
        );

        // Blue foreground (remaining capacity)
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(
                capBarX,
                capBarY,
                (int)(capacityBarWidth * remainingPercent),
                (int)capacityBarHeight
            ),
            Color.CornflowerBlue
        );
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
    }

    public void Update(GameTime gameTime, List<IEnemy> enemies)
    {
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
    /// Normal targeting and firing logic. Only runs when CurrentState == Active.
    /// </summary>
    private void UpdateActive(GameTime gameTime, List<IEnemy> enemies)
    {
        _fireCooldown?.Update(gameTime);

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

        bool canFire = _fireCooldown == null || _fireCooldown.State.HasFlag(TimerState.Completed);

        if (target != null && canFire)
        {
            _fireCooldown = new CountdownTimer(FireRate);
            _fireCooldown.Start();

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

    public void Draw(SpriteBatch spriteBatch, SpriteFont? font = null)
    {
        // Determine origin and draw position based on vertical scaling.
        // Champions (DrawScale.Y > 1.0) use bottom-center origin so they grow upward.
        // Generic towers use centered origin (default behavior).
        Vector2 spriteOrigin;
        Vector2 drawPosition = _drawPosition;

        if (DrawScale.Y > 1.0f)
        {
            // Champion tower: bottom-center origin.
            // Position so bottom sits at same level as generic tower's bottom.
            // Generic bottom = WorldPosition.Y + SpriteSize/2
            spriteOrigin = new Vector2(0.5f, 1.0f);
            drawPosition.Y += SpriteSize / 2f;
        }
        else
        {
            // Generic tower: centered origin
            spriteOrigin = new Vector2(0.5f, 0.5f);
        }

        // Semi-transparent when moving so tower looks ghostly (non-blocking)
        Color bodyColor = CurrentState == TowerState.Moving ? TowerColor * 0.5f : TowerColor;

        // Draw tower body (centered via DrawSprite), scaled by DrawScale
        TextureManager.DrawSprite(
            spriteBatch,
            drawPosition,
            new Vector2(SpriteSize * DrawScale.X, SpriteSize * DrawScale.Y),
            bodyColor,
            rotation: 0f,
            origin: spriteOrigin
        );

        // Draw health bar above tower (always visible for now)
        if ((CurrentHealth < MaxHealth))
        {
            float healthBarWidth = SpriteSize;
            float healthBarHeight = 4f;
            float healthPercent = (float)CurrentHealth / MaxHealth;

            // DrawRect uses top-left origin
            // Use drawPosition (adjusted for champion scaling) to position bar at tower top
            int barX = (int)(drawPosition.X - healthBarWidth / 2f);
            int barY = (int)(drawPosition.Y - (SpriteSize * DrawScale.Y) / 2f - 8f);

            // Red background (full bar)
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(barX, barY, (int)healthBarWidth, (int)healthBarHeight),
                Color.Red
            );

            // Green foreground (current health)
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(
                    barX,
                    barY,
                    (int)(healthBarWidth * healthPercent),
                    (int)healthBarHeight
                ),
                Color.LimeGreen
            );
        }

        // Draw capacity bar below health bar (always visible)
        DrawCapacityBar(spriteBatch, drawPosition);

        // Draw movement cooldown bar (fills 0→100%, disappears when ready to move again)
        if (CurrentState == TowerState.Cooldown)
            DrawMoveCooldownBar(spriteBatch, drawPosition);

        // Draw projectiles
        foreach (var proj in Projectiles)
        {
            proj.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Draw the range circle indicator (called when tower is hovered or during placement preview).
    /// Uses a pre-generated filled circle texture from TextureManager cache.
    /// </summary>
    public void DrawRangeIndicator(SpriteBatch spriteBatch)
    {
        TextureManager.DrawFilledCircle(spriteBatch, WorldPosition, Range, Color.White * 0.15f);
    }

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
