using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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
    /// Terrain spritesheet from Maps/terrain.png.
    /// Each tile is TileSize × TileSize pixels, laid out horizontally: HighGround, Path, Rock, HighGroundVariant.
    /// Null if the texture failed to load (falls back to colored rects).
    /// </summary>
    public static Texture2D? TerrainTileset { get; private set; }

    /// <summary>Cache of generated filled circle textures keyed by radius.</summary>
    private static readonly Dictionary<int, Texture2D> FilledCircleCache = new();

    /// <summary>
    /// Initialize the texture manager. Call once during LoadContent.
    /// Creates a 1x1 white pixel texture, loads the terrain tileset, and pre-generates circle textures.
    /// </summary>
    public static void Initialize(GraphicsDevice graphicsDevice, ContentManager content)
    {
        Pixel = new Texture2D(graphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });

        try
        {
            TerrainTileset = content.Load<Texture2D>("Maps/terrain");
        }
        catch
        {
            // Terrain sprite not available — Map.Draw will fall back to colored rectangles
        }

        // Pre-generate circles for tower ranges and AoE effects
        GenerateFilledCircleTexture(graphicsDevice, 50); // Cannon AoE radius
        GenerateFilledCircleTexture(graphicsDevice, 100); // Cannon range
        GenerateFilledCircleTexture(graphicsDevice, 120); // Gun range
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
    /// <param name="origin">Origin point (0-1 range, default 0.5,0.5 for center).</param>
    public static void DrawSprite(
        SpriteBatch spriteBatch,
        Vector2 position,
        Vector2 size,
        Color color,
        float rotation = 0f,
        Vector2? origin = null
    )
    {
        // Origin defaults to center of the 1x1 pixel (0.5, 0.5).
        // Can be overridden for different scaling anchors (e.g., bottom-center for champion towers).
        Vector2 spriteOrigin = origin ?? new Vector2(0.5f, 0.5f);
        spriteBatch.Draw(
            Pixel,
            position,
            sourceRectangle: null,
            color,
            rotation,
            spriteOrigin,
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
    /// Draw a filled circle at the given center position with anti-aliased edges.
    /// Uses a pre-cached base texture scaled to the desired radius.
    /// For non-cached radii, scales the nearest larger cached texture.
    /// </summary>
    public static void DrawFilledCircle(
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius,
        Color color
    )
    {
        if (radius < 1f)
            return;

        // Find the best cached texture to scale from
        Texture2D? texture = FindBestCachedTexture((int)radius);
        if (texture == null)
        {
            // Fallback if nothing is cached (shouldn't happen after Initialize)
            DrawSprite(spriteBatch, center, new Vector2(radius * 2, radius * 2), color);
            return;
        }

        // Scale the cached texture to match the desired radius
        float scale = (radius * 2f) / texture.Width;
        Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
        spriteBatch.Draw(texture, center, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Find the best cached circle texture for the given radius.
    /// Prefers exact match, then the smallest texture that's larger than needed.
    /// </summary>
    private static Texture2D? FindBestCachedTexture(int targetRadius)
    {
        // Exact match
        if (FilledCircleCache.TryGetValue(targetRadius, out var exact))
            return exact;

        // Find smallest cached texture larger than target (scaling down looks better than up)
        Texture2D? best = null;
        int bestRadius = int.MaxValue;
        foreach (var kvp in FilledCircleCache)
        {
            if (kvp.Key >= targetRadius && kvp.Key < bestRadius)
            {
                bestRadius = kvp.Key;
                best = kvp.Value;
            }
        }

        // Fall back to the largest cached texture (scaling down is fine)
        if (best == null)
        {
            int largestRadius = 0;
            foreach (var kvp in FilledCircleCache)
            {
                if (kvp.Key > largestRadius)
                {
                    largestRadius = kvp.Key;
                    best = kvp.Value;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Draw a terrain tile using the spritesheet, falling back to a colored rect if unavailable.
    /// The spritesheet column index matches TileType ordinal value (HighGround=0, Path=1, Rock=2, HighGroundVariant=3).
    /// </summary>
    public static void DrawTile(SpriteBatch spriteBatch, Rectangle destRect, TileType tileType)
    {
        if (TerrainTileset != null)
        {
            int col = (int)tileType;
            var sourceRect = new Rectangle(
                col * GameSettings.TileSize,
                0,
                GameSettings.TileSize,
                GameSettings.TileSize
            );
            spriteBatch.Draw(TerrainTileset, destRect, sourceRect, Color.White);
        }
        else
        {
            spriteBatch.Draw(Pixel, destRect, TileData.GetStats(tileType).Color);
        }
    }

    /// <summary>
    /// Generate and cache a filled circle texture with anti-aliased edges.
    /// Edge pixels use alpha blending for a smooth appearance.
    /// </summary>
    private static void GenerateFilledCircleTexture(GraphicsDevice graphicsDevice, int radius)
    {
        if (FilledCircleCache.ContainsKey(radius))
            return;

        int size = radius * 2;
        var texture = new Texture2D(graphicsDevice, size, size);
        Color[] data = new Color[size * size];

        int centerX = radius;
        int centerY = radius;
        float radiusSq = radius * radius;
        // Anti-alias: pixels within 1px of the edge get partial alpha
        float innerRadiusSq = (radius - 1f) * (radius - 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distSq = dx * dx + dy * dy;

                if (distSq <= innerRadiusSq)
                {
                    data[y * size + x] = Color.White;
                }
                else if (distSq <= radiusSq)
                {
                    // Smooth edge: lerp alpha based on distance within the 1px border
                    float dist = System.MathF.Sqrt(distSq);
                    float alpha = radius - dist; // 1.0 at inner edge, 0.0 at outer edge
                    data[y * size + x] = Color.White * alpha;
                }
                else
                {
                    data[y * size + x] = Color.Transparent;
                }
            }
        }

        texture.SetData(data);
        FilledCircleCache[radius] = texture;
    }
}
