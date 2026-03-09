using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;

namespace StarterTD.Entities;

/// <summary>
/// Pure rendering helpers for Tower. Reads tower state but never mutates it.
/// Extracted here so Tower.cs focuses on state, targeting, and combat logic.
/// </summary>
internal static class TowerDrawingHelper
{
    public static void Draw(SpriteBatch spriteBatch, Tower tower)
    {
        Texture2D? towerSprite = GetTowerSprite(tower);
        GetVisualBounds(tower, towerSprite, out Vector2 drawPosition, out Vector2 drawSize);
        Vector2 spritePosition = drawPosition;
        Vector2 spriteOrigin = new Vector2(0.5f, 0.5f);
        bool anchorToFootprintBottom = ShouldAnchorSpriteToFootprintBottom(towerSprite);
        if (anchorToFootprintBottom)
        {
            // Keep tower anchor at the occupied footprint bottom so tall art overhangs upward.
            spritePosition = GetFootprintBottomCenter(tower);
            spriteOrigin = new Vector2(0.5f, 1f);
        }

        DrawFootprintGuide(spriteBatch, tower);

        // Ability aura: two concentric circles give a soft glow effect.
        if (tower.IsAbilityBuffActive)
        {
            float auraOuterRadius = MathF.Max(drawSize.X, drawSize.Y) * 0.7f;
            float auraInnerRadius = MathF.Max(drawSize.X, drawSize.Y) * 0.45f;

            TextureManager.DrawFilledCircle(
                spriteBatch,
                drawPosition,
                auraOuterRadius,
                Color.Gold * 0.25f
            );
            TextureManager.DrawFilledCircle(
                spriteBatch,
                drawPosition,
                auraInnerRadius,
                Color.Gold * 0.45f
            );
        }

        DrawHealingUltSparkleAura(spriteBatch, tower, drawPosition, drawSize);
        DrawHealingRailgunAura(spriteBatch, tower, drawPosition, drawSize);

        bool isMoving = tower.CurrentState == TowerState.Moving;
        // When a sprite exists, keep original art colors and only apply movement alpha.
        if (towerSprite != null)
        {
            Color spriteTint = isMoving ? Color.White * 0.5f : Color.White;

            TextureManager.DrawSprite(
                spriteBatch,
                towerSprite,
                spritePosition,
                drawSize,
                spriteTint,
                rotation: 0f,
                origin: spriteOrigin
            );
        }
        else
        {
            // Fallback placeholder for tower types without sprite assets.
            Color bodyColor = isMoving ? tower.TowerColor * 0.5f : tower.TowerColor;
            TextureManager.DrawSprite(
                spriteBatch,
                spritePosition,
                drawSize,
                bodyColor,
                rotation: 0f,
                origin: spriteOrigin
            );
        }

        bool isWallSegment = tower.TowerType.IsWallSegment();
        bool showWallGrowthBar = isWallSegment && tower.MaxHealth < tower.HealthBarCapacity;
        bool showHealthBar = tower.CurrentHealth < tower.MaxHealth || showWallGrowthBar;

        // Health bar above tower.
        // Wall segments use a split bar (green/red/orange) against their growth cap.
        if (showHealthBar)
        {
            int barWidth = (int)MathF.Round(MathF.Max(24f, drawSize.X));
            const int barHeight = 4;

            int barX = (int)(drawPosition.X - barWidth / 2f);
            int barY = (int)(drawPosition.Y - drawSize.Y / 2f - 8f);

            if (isWallSegment)
            {
                DrawWallSegmentHealthBar(spriteBatch, tower, barX, barY, barWidth, barHeight);
            }
            else
            {
                float healthPercent = (float)tower.CurrentHealth / tower.MaxHealth;

                TextureManager.DrawRect(
                    spriteBatch,
                    new Rectangle(barX, barY, barWidth, barHeight),
                    Color.Red
                );
                TextureManager.DrawRect(
                    spriteBatch,
                    new Rectangle(barX, barY, (int)(barWidth * healthPercent), barHeight),
                    Color.LimeGreen
                );
            }
        }

        DrawCapacityBar(spriteBatch, tower, drawPosition, drawSize);

        if (tower.CurrentState == TowerState.Cooldown)
            DrawMoveCooldownBar(spriteBatch, tower, drawPosition, drawSize);

        foreach (var proj in tower.Projectiles)
            proj.Draw(spriteBatch);

        DrawHealingRailgunAmmoIcons(spriteBatch, tower, drawPosition, drawSize);
    }

    public static void GetVisualBounds(Tower tower, out Vector2 drawPosition, out Vector2 drawSize)
    {
        Texture2D? towerSprite = GetTowerSprite(tower);
        GetVisualBounds(tower, towerSprite, out drawPosition, out drawSize);
    }

    public static void DrawRangeIndicator(SpriteBatch spriteBatch, Tower tower)
    {
        TextureManager.DrawFilledCircle(
            spriteBatch,
            tower.WorldPosition,
            tower.Range,
            Color.White * 0.15f
        );
    }

    private static void DrawCapacityBar(
        SpriteBatch spriteBatch,
        Tower tower,
        Vector2 drawPosition,
        Vector2 drawSize
    )
    {
        if (tower.BlockCapacity <= 0)
            return;

        float barWidth = MathF.Max(24f, drawSize.X);
        const float barHeight = 3f;
        float remainingPercent =
            (float)(tower.BlockCapacity - tower.CurrentEngagedCount) / tower.BlockCapacity;

        int barX = (int)(drawPosition.X - barWidth / 2f);
        int barY = (int)(drawPosition.Y - drawSize.Y / 2f - 3f);

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, (int)barWidth, (int)barHeight),
            Color.DarkGray
        );
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, (int)(barWidth * remainingPercent), (int)barHeight),
            Color.CornflowerBlue
        );
    }

    private static void DrawWallSegmentHealthBar(
        SpriteBatch spriteBatch,
        Tower tower,
        int barX,
        int barY,
        int barWidth,
        int barHeight
    )
    {
        int cap = tower.HealthBarCapacity;
        if (cap <= 0)
            return;

        int current = Math.Clamp(tower.CurrentHealth, 0, cap);
        int max = Math.Clamp(tower.MaxHealth, 0, cap);
        int missingCurrent = Math.Max(0, max - current);

        int greenWidth = (int)MathF.Round(barWidth * (float)current / cap);
        int redWidth = (int)MathF.Round(barWidth * (float)missingCurrent / cap);
        int orangeWidth = Math.Max(0, barWidth - greenWidth - redWidth);

        // Background for readability on bright tile colors.
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, barWidth, barHeight),
            Color.DarkSlateGray
        );

        if (orangeWidth > 0)
        {
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(barX + greenWidth + redWidth, barY, orangeWidth, barHeight),
                Color.DarkOrange
            );
        }

        if (redWidth > 0)
        {
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(barX + greenWidth, barY, redWidth, barHeight),
                Color.Red
            );
        }

        if (greenWidth > 0)
        {
            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(barX, barY, greenWidth, barHeight),
                Color.LimeGreen
            );
        }
    }

    private static void DrawMoveCooldownBar(
        SpriteBatch spriteBatch,
        Tower tower,
        Vector2 drawPosition,
        Vector2 drawSize
    )
    {
        float barWidth = MathF.Max(24f, drawSize.X);
        const float barHeight = 3f;
        // Fills from 0% (just arrived) to 100% (ready to move again)
        float progress = tower.MoveCooldownProgress;

        int barX = (int)(drawPosition.X - barWidth / 2f);
        // Position below capacity bar
        int barY = (int)(drawPosition.Y - drawSize.Y / 2f + 2f);

        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, (int)barWidth, (int)barHeight),
            Color.DarkGray
        );
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(barX, barY, (int)(barWidth * progress), (int)barHeight),
            Color.Gold
        );
    }

    private static void DrawHealingUltSparkleAura(
        SpriteBatch spriteBatch,
        Tower tower,
        Vector2 drawPosition,
        Vector2 drawSize
    )
    {
        if (!tower.HasHealingUltAttackSpeedBuff)
            return;

        float baseSize = MathF.Max(drawSize.X, drawSize.Y);
        float phase = tower.HealingUltSparklePhase;
        float pulse = 0.85f + 0.15f * MathF.Sin(phase * 6f);

        TextureManager.DrawFilledCircle(
            spriteBatch,
            drawPosition,
            baseSize * 0.8f,
            Color.Gold * (0.14f * pulse)
        );
        TextureManager.DrawFilledCircle(
            spriteBatch,
            drawPosition,
            baseSize * 0.62f,
            Color.White * (0.1f * pulse)
        );

        const int sparkleCount = 6;
        for (int i = 0; i < sparkleCount; i++)
        {
            float angle = phase * (1.8f + i * 0.07f) + i * (MathF.Tau / sparkleCount);
            float orbit = baseSize * (0.58f + 0.08f * MathF.Sin(phase * 3f + i));
            float twinkle = 0.5f + 0.5f * MathF.Sin(phase * (9f + i) + i * 1.3f);
            float sparkleSize = 3f + twinkle * 2.5f;
            var sparkleColor = (i & 1) == 0 ? Color.White : Color.Gold;
            var sparklePos = drawPosition + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * orbit;

            TextureManager.DrawSprite(
                spriteBatch,
                sparklePos,
                new Vector2(sparkleSize, sparkleSize),
                sparkleColor * (0.35f + twinkle * 0.45f)
            );
        }
    }

    private static void DrawHealingRailgunAura(
        SpriteBatch spriteBatch,
        Tower tower,
        Vector2 drawPosition,
        Vector2 drawSize
    )
    {
        if (
            tower is not HealingChampionTower healingChampion
            || !healingChampion.IsRailgunEmpowered
        )
            return;

        float baseSize = MathF.Max(drawSize.X, drawSize.Y);
        TextureManager.DrawFilledCircle(
            spriteBatch,
            drawPosition,
            baseSize * 0.78f,
            Color.Cyan * 0.22f
        );
        TextureManager.DrawFilledCircle(
            spriteBatch,
            drawPosition,
            baseSize * 0.55f,
            Color.White * 0.12f
        );
    }

    private static void DrawHealingRailgunAmmoIcons(
        SpriteBatch spriteBatch,
        Tower tower,
        Vector2 drawPosition,
        Vector2 drawSize
    )
    {
        if (
            tower is not HealingChampionTower healingChampion
            || !healingChampion.IsRailgunEmpowered
        )
            return;

        const int maxShots = 5;
        const float iconWidth = 9f;
        const float iconHeight = 4f;
        const float iconSpacing = 3f;
        const float iconOffsetLeft = 14f;

        float stackHeight = maxShots * iconHeight + (maxShots - 1) * iconSpacing;
        float centerX = drawPosition.X - drawSize.X / 2f - iconOffsetLeft;
        float startY = drawPosition.Y - stackHeight / 2f + iconHeight / 2f;
        int shotsRemaining = healingChampion.RailgunShotsRemaining;

        for (int i = 0; i < maxShots; i++)
        {
            bool isLoaded = i < shotsRemaining;
            Color shellColor = isLoaded ? new Color(255, 232, 120) : new Color(60, 60, 60);
            float y = startY + i * (iconHeight + iconSpacing);

            TextureManager.DrawSprite(
                spriteBatch,
                new Vector2(centerX, y),
                new Vector2(iconWidth, iconHeight),
                shellColor
            );
            TextureManager.DrawSprite(
                spriteBatch,
                new Vector2(centerX + iconWidth / 2f, y),
                new Vector2(2f, iconHeight + 2f),
                shellColor
            );
        }
    }

    private static Texture2D? GetTowerSprite(Tower tower) =>
        tower.TowerType switch
        {
            TowerType.Gun => TextureManager.GenericGunTowerSprite,
            TowerType.Cannon => TextureManager.GenericCannonTowerSprite,
            TowerType.Walling => TextureManager.GenericWallingTowerSprite,
            TowerType.ChampionGun => TextureManager.ChampionGunTowerSprite,
            TowerType.ChampionCannon => TextureManager.ChampionCannonTowerSprite,
            TowerType.ChampionWalling => TextureManager.ChampionWallingTowerSprite,
            TowerType.ChampionHealing => TextureManager.ChampionHealingTowerSprite,
            _ => null,
        };

    private static bool ShouldAnchorSpriteToFootprintBottom(Texture2D? towerSprite) =>
        towerSprite != null;

    private static Vector2 GetFootprintBottomCenter(Tower tower)
    {
        float footprintHeight = tower.FootprintSize.Y * GameSettings.TileSize;
        return tower.DrawPosition + new Vector2(0f, footprintHeight / 2f);
    }

    private static void DrawFootprintGuide(SpriteBatch spriteBatch, Tower tower)
    {
        const float guideAlpha = 0.16f;
        Color guideColor = Color.Gray * guideAlpha;
        int tileSize = GameSettings.TileSize;
        int footprintWidth = tower.FootprintSize.X * tileSize;
        int footprintHeight = tower.FootprintSize.Y * tileSize;
        Vector2 topLeft =
            tower.DrawPosition - new Vector2(footprintWidth / 2f, footprintHeight / 2f);

        for (int y = 0; y < tower.FootprintSize.Y; y++)
        {
            for (int x = 0; x < tower.FootprintSize.X; x++)
            {
                int tileX = (int)MathF.Round(topLeft.X + x * tileSize);
                int tileY = (int)MathF.Round(topLeft.Y + y * tileSize);
                TextureManager.DrawRect(
                    spriteBatch,
                    new Rectangle(tileX, tileY, tileSize, tileSize),
                    guideColor
                );
            }
        }
    }

    private static void GetVisualBounds(
        Tower tower,
        Texture2D? towerSprite,
        out Vector2 drawPosition,
        out Vector2 drawSize
    )
    {
        drawPosition = tower.DrawPosition;
        drawSize = tower.DrawSize;

        if (towerSprite == null)
            return;

        drawSize = new Vector2(
            towerSprite.Width * tower.DrawScale.X,
            towerSprite.Height * tower.DrawScale.Y
        );
        if (ShouldAnchorSpriteToFootprintBottom(towerSprite))
            drawPosition = GetFootprintBottomCenter(tower) - new Vector2(0f, drawSize.Y / 2f);
    }
}
