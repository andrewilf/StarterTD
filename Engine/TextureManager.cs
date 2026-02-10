using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Engine;

/// <summary>
/// Static texture manager that provides a 1x1 white pixel texture
/// and helper drawing methods. No MGCB pipeline needed.
/// </summary>
public static class TextureManager
{
    /// <summary>A 1x1 white pixel texture used for all placeholder rendering.</summary>
    public static Texture2D Pixel { get; private set; } = null!;

    /// <summary>
    /// Initialize the texture manager. Call once during LoadContent.
    /// Creates a 1x1 white pixel texture in memory.
    /// </summary>
    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        Pixel = new Texture2D(graphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Draw a filled rectangle (placeholder sprite) at the given position.
    /// Origin is set to center so the sprite can be rotated later when
    /// real sprite assets are swapped in.
    /// </summary>
    /// <param name="spriteBatch">Active SpriteBatch (Begin must have been called).</param>
    /// <param name="position">Center position of the sprite in world space.</param>
    /// <param name="size">Width and height of the sprite.</param>
    /// <param name="color">Tint color for the sprite.</param>
    /// <param name="rotation">Rotation in radians (default 0).</param>
    public static void DrawSprite(
        SpriteBatch spriteBatch,
        Vector2 position,
        Vector2 size,
        Color color,
        float rotation = 0f
    )
    {
        // Origin is at the center of the 1x1 pixel (0.5, 0.5).
        // Scale is the desired size since the source texture is 1x1.
        Vector2 origin = new Vector2(0.5f, 0.5f);
        spriteBatch.Draw(
            Pixel,
            position,
            sourceRectangle: null,
            color,
            rotation,
            origin,
            scale: size,
            SpriteEffects.None,
            layerDepth: 0f
        );
    }

    /// <summary>
    /// Draw a hollow rectangle outline.
    /// </summary>
    public static void DrawRectOutline(
        SpriteBatch spriteBatch,
        Rectangle rect,
        Color color,
        int thickness = 1
    )
    {
        // Top
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(
            Pixel,
            new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness),
            color
        );
        // Left
        spriteBatch.Draw(Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(
            Pixel,
            new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height),
            color
        );
    }

    /// <summary>
    /// Draw a filled rectangle (non-centered, top-left origin).
    /// </summary>
    public static void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        spriteBatch.Draw(Pixel, rect, color);
    }

    /// <summary>
    /// Draw a simple circle approximation using a filled square.
    /// For a real circle, you would generate a circle texture.
    /// This is a placeholder — visually it's a colored square.
    /// </summary>
    public static void DrawCirclePlaceholder(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius,
        Color color
    )
    {
        DrawSprite(spriteBatch, center, new Vector2(radius * 2, radius * 2), color);
    }
}
