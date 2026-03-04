using System;
using Microsoft.Xna.Framework.Graphics;

namespace StarterTD.Engine;

/// <summary>
/// Central configuration for the game.
/// ScreenWidth/Height are set at startup from the monitor resolution.
/// </summary>
public static class GameSettings
{
    // --- Grid / Map ---
    public const int TileSize = 32; // display pixels per tile (change to 64 for high-detail art)

    /// <summary>
    /// Pixel size of each tile in terrain.png spritesheet.
    /// Decoupled from TileSize so the spritesheet can differ from display size.
    /// </summary>
    public const int TerrainSourceTileSize = 40;

    // --- Player ---
    public const int StartingLives = 5;

    // --- Window (set at startup via Initialize) ---
    public static int ScreenWidth { get; private set; }
    public static int ScreenHeight { get; private set; }

    // --- UI (scales proportionally with resolution) ---
    public static int UIPanelWidth => Math.Clamp(ScreenWidth * 200 / 1024, 160, 280);

    /// <summary>
    /// Read the monitor's native resolution. Call once from Game1.Initialize().
    /// </summary>
    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        ScreenWidth = graphicsDevice.Adapter.CurrentDisplayMode.Width;
        ScreenHeight = graphicsDevice.Adapter.CurrentDisplayMode.Height;
    }
}
