namespace StarterTD.Engine;

/// <summary>
/// Central configuration constants for the game.
/// Change values here to tweak the entire game at once.
/// </summary>
public static class GameSettings
{
    // --- Window ---
    public const int ScreenWidth = 1024;
    public const int ScreenHeight = 768;

    // --- Grid / Map ---
    public const int TileSize = 40; // pixels per tile

    // --- Player ---
    public const int StartingMoney = 2000;
    public const int StartingLives = 5;

    // --- UI ---
    public const int UIPanelWidth = 200; // right-side panel width
}
