using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;

namespace StarterTD.Entities;

/// <summary>
/// A temporary visual effect that displays an expanding circle when an AoE projectile impacts.
/// The circle expands from 0 to MaxRadius while fading out over ~0.3 seconds.
/// Similar to FloatingText, this is a self-contained entity that manages its own lifecycle.
/// </summary>
public class AoEEffect
{
    public Vector2 Position { get; }
    public float MaxRadius { get; }
    public bool IsActive { get; private set; }

    private float _elapsedTime;
    private const float DurationSeconds = 0.3f;

    public AoEEffect(Vector2 position, float maxRadius)
    {
        Position = position;
        MaxRadius = maxRadius;
        IsActive = true;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive)
            return;

        _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_elapsedTime >= DurationSeconds)
            IsActive = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive)
            return;

        float progress = _elapsedTime / DurationSeconds;
        float currentRadius = MaxRadius * progress;
        float opacity = 1f - progress;

        TextureManager.DrawFilledCircle(
            spriteBatch,
            Position,
            currentRadius,
            Color.Orange * opacity
        );
    }
}
