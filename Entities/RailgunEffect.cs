using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Timers;
using StarterTD.Engine;

namespace StarterTD.Entities;

/// <summary>
/// Short-lived beam flash for ChampionHealing's instant railgun shots.
/// </summary>
public class RailgunEffect
{
    private const float DurationSeconds = 0.14f;

    private readonly CountdownTimer _timer;
    private readonly Vector2 _start;
    private readonly Vector2 _end;

    public bool IsActive => !_timer.State.HasFlag(TimerState.Completed);

    public RailgunEffect(Vector2 start, Vector2 end)
    {
        _start = start;
        _end = end;
        _timer = new CountdownTimer(DurationSeconds);
        _timer.Start();
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;

        _timer.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;

        float progress = (float)_timer.CurrentTime.TotalSeconds / DurationSeconds;
        float opacity = 1f - progress;

        Vector2 direction = _end - _start;
        float length = direction.Length();
        if (length > 0.001f)
        {
            float rotation = MathF.Atan2(direction.Y, direction.X);
            Vector2 midpoint = (_start + _end) * 0.5f;

            TextureManager.DrawSprite(
                spriteBatch,
                midpoint,
                new Vector2(length, 16f),
                Color.Cyan * (0.22f * opacity),
                rotation
            );
            TextureManager.DrawSprite(
                spriteBatch,
                midpoint,
                new Vector2(length, 7f),
                Color.DeepSkyBlue * (0.5f * opacity),
                rotation
            );
            TextureManager.DrawSprite(
                spriteBatch,
                midpoint,
                new Vector2(length, 3f),
                Color.White * (0.9f * opacity),
                rotation
            );
        }

        float impactRadius = 9f + 13f * progress;
        TextureManager.DrawFilledCircle(
            spriteBatch,
            _end,
            impactRadius,
            Color.Cyan * (0.35f * opacity)
        );
        TextureManager.DrawFilledCircle(spriteBatch, _end, 4f, Color.White * (0.95f * opacity));
    }
}
