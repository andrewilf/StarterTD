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
    private const float SpriteSize = 30f;

    public static void Draw(SpriteBatch spriteBatch, Tower tower)
    {
        Vector2 drawPosition = tower.DrawPosition;

        // Champions (DrawScale.Y > 1.0) use bottom-center origin so they grow upward.
        // Generic towers use centered origin (default behavior).
        Vector2 spriteOrigin;
        if (tower.DrawScale.Y > 1.0f)
        {
            // Position so bottom sits at same level as generic tower's bottom.
            // Generic bottom = WorldPosition.Y + SpriteSize/2
            spriteOrigin = new Vector2(0.5f, 1.0f);
            drawPosition.Y += SpriteSize / 2f;
        }
        else
        {
            spriteOrigin = new Vector2(0.5f, 0.5f);
        }

        // Ability aura: two concentric circles give a soft glow effect
        if (tower.IsAbilityBuffActive)
        {
            TextureManager.DrawFilledCircle(
                spriteBatch,
                tower.DrawPosition,
                SpriteSize * 1.4f,
                Color.Gold * 0.25f
            );
            TextureManager.DrawFilledCircle(
                spriteBatch,
                tower.DrawPosition,
                SpriteSize * 0.9f,
                Color.Gold * 0.45f
            );
        }

        // Semi-transparent when moving so tower looks ghostly (non-blocking)
        Color bodyColor =
            tower.CurrentState == TowerState.Moving ? tower.TowerColor * 0.5f : tower.TowerColor;

        TextureManager.DrawSprite(
            spriteBatch,
            drawPosition,
            new Vector2(SpriteSize * tower.DrawScale.X, SpriteSize * tower.DrawScale.Y),
            bodyColor,
            rotation: 0f,
            origin: spriteOrigin,
            drawOutline: true
        );

        // Health bar above tower (only when damaged)
        if (tower.CurrentHealth < tower.MaxHealth)
        {
            float healthBarWidth = SpriteSize;
            float healthBarHeight = 4f;
            float healthPercent = (float)tower.CurrentHealth / tower.MaxHealth;

            int barX = (int)(drawPosition.X - healthBarWidth / 2f);
            int barY = (int)(drawPosition.Y - (SpriteSize * tower.DrawScale.Y) / 2f - 8f);

            TextureManager.DrawRect(
                spriteBatch,
                new Rectangle(barX, barY, (int)healthBarWidth, (int)healthBarHeight),
                Color.Red
            );
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

        DrawCapacityBar(spriteBatch, tower, drawPosition);

        if (tower.CurrentState == TowerState.Cooldown)
            DrawMoveCooldownBar(spriteBatch, tower, drawPosition);

        foreach (var proj in tower.Projectiles)
            proj.Draw(spriteBatch);
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

    private static void DrawCapacityBar(SpriteBatch spriteBatch, Tower tower, Vector2 drawPosition)
    {
        float barWidth = SpriteSize;
        float barHeight = 3f;
        float remainingPercent =
            (float)(tower.BlockCapacity - tower.CurrentEngagedCount) / tower.BlockCapacity;

        int barX = (int)(drawPosition.X - barWidth / 2f);
        // Scale Y offset by DrawScale.Y so bar stays at top of scaled tower
        int barY = (int)(drawPosition.Y - (SpriteSize * tower.DrawScale.Y) / 2f - 8f + 5f);

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

    private static void DrawMoveCooldownBar(
        SpriteBatch spriteBatch,
        Tower tower,
        Vector2 drawPosition
    )
    {
        float barWidth = SpriteSize;
        float barHeight = 3f;
        // Fills from 0% (just arrived) to 100% (ready to move again)
        float progress = tower.MoveCooldownProgress;

        int barX = (int)(drawPosition.X - barWidth / 2f);
        // Position below capacity bar: health bar at -8f, capacity at -3f, this at +2f
        int barY = (int)(drawPosition.Y - (SpriteSize * tower.DrawScale.Y) / 2f - 8f + 10f);

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
}
