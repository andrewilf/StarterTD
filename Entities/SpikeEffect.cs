using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Timers;
using StarterTD.Engine;

namespace StarterTD.Entities;

/// <summary>
/// Short-lived visual for the walling champion's spike attack.
/// Draws a narrow vertical spike that rises from the target tile and fades out.
/// </summary>
public class SpikeEffect
{
    public bool IsActive => !_timer.State.HasFlag(TimerState.Completed);

    private readonly Vector2 _position;
    private readonly CountdownTimer _timer;
    private const float DurationSeconds = 0.45f;
    private const float MaxHeight = 48f;
    private const float SpikeWidth = 10f;

    // Flash: a bright wide burst at the base that quickly shrinks
    private const float FlashSize = 22f;

    public SpikeEffect(Vector2 position)
    {
        _position = position;
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

        // CurrentTime counts up (elapsed). Progress 0→1 over duration.
        float progress = (float)_timer.CurrentTime.TotalSeconds / DurationSeconds;

        // Spike: shoots up fast, lingers at full height, then fades
        // Use an ease-out curve so the spike reaches near-full height quickly
        float spikeOpacity = 1f - progress;
        float heightFraction = MathF.Min(1f, progress * 4f); // reaches full height in first 25% of duration
        float height = MaxHeight * heightFraction;

        var spikeRect = new Rectangle(
            (int)(_position.X - SpikeWidth / 2f),
            (int)(_position.Y - height),
            (int)SpikeWidth,
            (int)MathF.Max(1f, height)
        );

        // Bright white-green core with a darker green outline feel via layered rects
        TextureManager.DrawRect(spriteBatch, spikeRect, Color.LimeGreen * spikeOpacity);

        // Bright white flash at tile center (only in first half)
        float flashProgress = progress / 0.5f; // 0→1 over first half of duration
        if (flashProgress < 1f)
        {
            float flashOpacity = 1f - flashProgress;
            float flashRadius = FlashSize * (1f - flashProgress * 0.5f);
            var flashRect = new Rectangle(
                (int)(_position.X - flashRadius / 2f),
                (int)(_position.Y - flashRadius / 2f),
                (int)flashRadius,
                (int)flashRadius
            );
            TextureManager.DrawRect(spriteBatch, flashRect, Color.White * flashOpacity * 0.85f);
        }
    }
}
