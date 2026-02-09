namespace StarterTD.Entities;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Represents temporary floating text that moves upward and fades out over time.
/// Used for visual feedback when money is spent or gained.
/// </summary>
public class FloatingText
{
    public Vector2 Position { get; private set; }
    public string Text { get; }
    public Color Color { get; }
    public bool IsActive => _remainingTime > 0;

    private float _remainingTime;
    private readonly float _lifetime;
    private readonly Vector2 _velocity;

    /// <summary>
    /// Creates a new floating text display.
    /// </summary>
    /// <param name="startPos">Starting world position</param>
    /// <param name="text">Text to display (e.g., "-$50")</param>
    /// <param name="color">Text color</param>
    /// <param name="lifetime">Duration in seconds (default 1.5s)</param>
    /// <param name="velocity">Movement velocity (default: float upward)</param>
    public FloatingText(Vector2 startPos, string text, Color color,
                       float lifetime = 1.5f, Vector2? velocity = null)
    {
        Position = startPos;
        Text = text;
        Color = color;
        _lifetime = lifetime;
        _remainingTime = lifetime;
        _velocity = velocity ?? new Vector2(0, -20f); // Default: float upward
    }

    /// <summary>
    /// Updates the floating text position and lifetime.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _remainingTime -= dt;
        Position += _velocity * dt;
    }

    /// <summary>
    /// Draws the floating text with fade-out effect.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont? font)
    {
        if (font == null || !IsActive) return;

        // Fade-out alpha during last 30% of lifetime
        float alpha = Math.Min(1f, _remainingTime / (_lifetime * 0.3f));
        Color fadeColor = Color * alpha;

        // Shadow for readability
        spriteBatch.DrawString(font, Text, Position + new Vector2(1, 1),
                              Color.Black * alpha);
        // Main text
        spriteBatch.DrawString(font, Text, Position, fadeColor);
    }
}
