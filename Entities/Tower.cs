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
    public int Level { get; private set; }
    public Point GridPosition { get; }
    public Vector2 WorldPosition { get; }
    public float Range { get; private set; }
    public float Damage { get; private set; }
    public float FireRate { get; private set; }
    public int Cost { get; }
    public int UpgradeCost { get; private set; }
    public bool IsAOE { get; private set; }
    public float AOERadius { get; private set; }
    public Color TowerColor { get; private set; }

    public TowerType TowerType { get; }

    private float _fireCooldown;
    private const float SpriteSize = 30f;
    private const float ProjectileSpeed = 400f;

    /// <summary>List of active projectiles fired by this tower.</summary>
    public List<Projectile> Projectiles { get; } = new();

    public Tower(TowerType type, Point gridPosition)
    {
        TowerType = type;
        GridPosition = gridPosition;
        WorldPosition = Map.GridToWorld(gridPosition);
        Level = 1;

        var stats = TowerData.GetStats(type, Level);
        ApplyStats(stats);
        Cost = stats.Cost;
    }

    private void ApplyStats(TowerData.TowerStats stats)
    {
        Name = stats.Name;
        Range = stats.Range;
        Damage = stats.Damage;
        FireRate = stats.FireRate;
        UpgradeCost = stats.UpgradeCost;
        IsAOE = stats.IsAOE;
        AOERadius = stats.AOERadius;
        TowerColor = stats.Color;
    }

    public void Upgrade()
    {
        if (Level >= 2)
            return;
        Level++;
        var stats = TowerData.GetStats(TowerType, Level);
        ApplyStats(stats);
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
        // Draw tower body (centered via DrawSprite)
        TextureManager.DrawSprite(
            spriteBatch,
            WorldPosition,
            new Vector2(SpriteSize, SpriteSize),
            TowerColor
        );

        // Draw level indicator (small dot on top)
        if (Level >= 2)
        {
            TextureManager.DrawSprite(
                spriteBatch,
                WorldPosition - new Vector2(0, SpriteSize / 2f - 3f),
                new Vector2(6f, 6f),
                Color.Gold
            );
        }

        // Draw upgrade cost indicator (if not max level)
        if (Level < 2 && font != null && UpgradeCost > 0)
        {
            string costText = $"${UpgradeCost}";
            Vector2 textSize = font.MeasureString(costText);
            Vector2 textPos = new Vector2(
                WorldPosition.X - textSize.X / 2f,
                WorldPosition.Y - SpriteSize / 2f - 20f
            );

            // Shadow
            spriteBatch.DrawString(font, costText, textPos + new Vector2(1, 1), Color.Black);
            // Main text (cyan for visibility)
            spriteBatch.DrawString(font, costText, textPos, Color.Cyan);
        }

        // Draw projectiles
        foreach (var proj in Projectiles)
        {
            proj.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Draw the range circle indicator (called when tower is selected).
    /// </summary>
    public void DrawRangeIndicator(SpriteBatch spriteBatch)
    {
        // Draw a semi-transparent range indicator as a large square
        // (A real circle would require a generated texture)
        TextureManager.DrawSprite(
            spriteBatch,
            WorldPosition,
            new Vector2(Range * 2, Range * 2),
            new Color(255, 255, 255, 30)
        );
    }
}
