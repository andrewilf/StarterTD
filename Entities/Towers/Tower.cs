using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public Point GridPosition { get; }
    public Vector2 WorldPosition { get; }
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

    /// <summary>Maximum number of enemies that can simultaneously attack this tower.</summary>
    public int BlockCapacity { get; private set; }

    /// <summary>Number of enemies currently attacking this tower.</summary>
    public int CurrentEngagedCount => _currentEngagedCount;

    /// <summary>Scale factor for visual rendering (X, Y). Generics default to (1.0, 1.0), Champions to (1.0, 1.5).</summary>
    public Vector2 DrawScale { get; private set; }

    public TowerType TowerType { get; }

    private float _fireCooldown;
    private int _currentEngagedCount = 0;
    private const float SpriteSize = 30f;
    private const float ProjectileSpeed = 400f;

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
        WorldPosition = Map.GridToWorld(gridPosition);

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
    }

    public void Update(GameTime gameTime, List<IEnemy> enemies)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_fireCooldown > 0)
            _fireCooldown -= dt;

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

        if (target != null && _fireCooldown <= 0)
        {
            _fireCooldown = FireRate;

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

        // Update projectiles
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            Projectiles[i].Update(gameTime, enemies);
            if (!Projectiles[i].IsActive)
                Projectiles.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont? font = null)
    {
        // Determine origin and draw position based on vertical scaling.
        // Champions (DrawScale.Y > 1.0) use bottom-center origin so they grow upward.
        // Generic towers use centered origin (default behavior).
        Vector2 spriteOrigin;
        Vector2 drawPosition = WorldPosition;

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

        // Draw tower body (centered via DrawSprite), scaled by DrawScale
        TextureManager.DrawSprite(
            spriteBatch,
            drawPosition,
            new Vector2(SpriteSize * DrawScale.X, SpriteSize * DrawScale.Y),
            TowerColor,
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
