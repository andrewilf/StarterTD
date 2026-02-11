using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Managers;

namespace StarterTD.UI;

/// <summary>
/// Right-side UI panel showing player stats, tower selection buttons,
/// and selected tower info. Uses SpriteFont-free rendering (colored rectangles
/// and the TextureManager pixel). Text is drawn using a simple bitmap approach
/// — for MVP, we show colored indicators instead of text labels.
///
/// NOTE: MonoGame requires SpriteFont for text. We'll use a loaded font.
/// For the MVP, we generate a basic SpriteFont or skip text entirely.
/// This implementation uses placeholder colored blocks for UI elements.
/// </summary>
public class UIPanel
{
    private readonly int _x;
    private readonly int _width;
    private readonly int _height;
    private readonly ChampionManager? _championManager;

    /// <summary>Which tower type the player has selected to place (null = none).</summary>
    public TowerType? SelectedTowerType { get; set; }

    // Button rectangles for tower selection
    private readonly Rectangle _gunButton;
    private readonly Rectangle _cannonButton;
    private readonly Rectangle _championGunButton;
    private readonly Rectangle _championCannonButton;
    private readonly Rectangle _startWaveButton;

    /// <summary>Whether the "Start Wave" button was clicked this frame.</summary>
    public bool StartWaveClicked { get; private set; }

    private SpriteFont? _font;

    public UIPanel(int screenWidth, int screenHeight, ChampionManager? championManager = null)
    {
        _width = GameSettings.UIPanelWidth;
        _x = screenWidth - _width;
        _height = screenHeight;
        _championManager = championManager;

        int buttonWidth = _width - 20;
        int buttonHeight = 50;
        int startY = 120;
        int gap = 10;

        _gunButton = new Rectangle(_x + 10, startY, buttonWidth, buttonHeight);
        _cannonButton = new Rectangle(
            _x + 10,
            startY + buttonHeight + gap,
            buttonWidth,
            buttonHeight
        );

        // Champion buttons positioned below Generic towers
        int championStartY = _cannonButton.Bottom + 60;
        _championGunButton = new Rectangle(_x + 10, championStartY, buttonWidth, buttonHeight);
        _championCannonButton = new Rectangle(
            _x + 10,
            championStartY + buttonHeight + gap,
            buttonWidth,
            buttonHeight
        );

        _startWaveButton = new Rectangle(_x + 10, _height - 70, buttonWidth, buttonHeight);
    }

    /// <summary>
    /// Set the SpriteFont for text rendering.
    /// </summary>
    public void SetFont(SpriteFont font)
    {
        _font = font;
    }

    /// <summary>
    /// Get the current font (may be null if not loaded).
    /// </summary>
    public SpriteFont? GetFont()
    {
        return _font;
    }

    /// <summary>
    /// Handle click input on the UI panel. Returns true if the click was consumed.
    /// </summary>
    public bool HandleClick(Point mousePos, int playerMoney)
    {
        StartWaveClicked = false;

        // Generic towers: check cost and champion alive requirement
        if (_gunButton.Contains(mousePos))
        {
            var stats = TowerData.GetStats(TowerType.Gun);
            bool canPlace = playerMoney >= stats.Cost && (_championManager?.CanPlaceGeneric(TowerType.Gun) ?? true);
            SelectedTowerType = canPlace ? TowerType.Gun : null;
            return true;
        }
        if (_cannonButton.Contains(mousePos))
        {
            var stats = TowerData.GetStats(TowerType.Cannon);
            bool canPlace = playerMoney >= stats.Cost && (_championManager?.CanPlaceGeneric(TowerType.Cannon) ?? true);
            SelectedTowerType = canPlace ? TowerType.Cannon : null;
            return true;
        }

        // Champion towers: check placement rules (free, so no cost check)
        if (_championGunButton.Contains(mousePos))
        {
            bool canPlace = _championManager?.CanPlaceChampion(TowerType.ChampionGun) ?? true;
            SelectedTowerType = canPlace ? TowerType.ChampionGun : null;
            return true;
        }
        if (_championCannonButton.Contains(mousePos))
        {
            bool canPlace = _championManager?.CanPlaceChampion(TowerType.ChampionCannon) ?? true;
            SelectedTowerType = canPlace ? TowerType.ChampionCannon : null;
            return true;
        }

        // Wave start button
        if (_startWaveButton.Contains(mousePos))
        {
            StartWaveClicked = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a screen position is within the UI panel area.
    /// </summary>
    public bool ContainsPoint(Point pos)
    {
        return pos.X >= _x;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        int money,
        int lives,
        int wave,
        int totalWaves,
        bool waveInProgress
    )
    {
        // Panel background
        TextureManager.DrawRect(spriteBatch, new Rectangle(_x, 0, _width, _height), Color.Black);

        // Separator line
        TextureManager.DrawRect(spriteBatch, new Rectangle(_x, 0, 2, _height), Color.Gray);

        // --- Stats area ---
        if (_font != null)
        {
            spriteBatch.DrawString(_font, $"Money: ${money}", new Vector2(_x + 10, 10), Color.Gold);
            spriteBatch.DrawString(
                _font,
                $"Lives: {lives}",
                new Vector2(_x + 10, 35),
                Color.LimeGreen
            );
            spriteBatch.DrawString(
                _font,
                $"Wave: {wave}/{totalWaves}",
                new Vector2(_x + 10, 60),
                Color.White
            );

            // --- Tower buttons ---
            DrawGenericTowerButton(
                spriteBatch,
                _gunButton,
                "Gun ($50)",
                TowerType.Gun,
                TowerType.ChampionGun,
                50,
                money
            );

            DrawGenericTowerButton(
                spriteBatch,
                _cannonButton,
                "Cannon ($80)",
                TowerType.Cannon,
                TowerType.ChampionCannon,
                80,
                money
            );

            // --- Champion buttons ---
            spriteBatch.DrawString(
                _font,
                "Champions (Free):",
                new Vector2(_x + 10, _cannonButton.Bottom + 30),
                Color.Yellow
            );

            DrawChampionButton(spriteBatch, _championGunButton, "Champ Gun", TowerType.ChampionGun);

            DrawChampionButton(
                spriteBatch,
                _championCannonButton,
                "Champ Cannon",
                TowerType.ChampionCannon
            );

            // --- Info text ---
            spriteBatch.DrawString(
                _font,
                "L-Click: Place",
                new Vector2(_x + 10, _championCannonButton.Bottom + 20),
                Color.LightGray
            );
            spriteBatch.DrawString(
                _font,
                "ESC: Deselect",
                new Vector2(_x + 10, _championCannonButton.Bottom + 45),
                Color.LightGray
            );

            // --- Start Wave button ---
            Color waveBtnColor = waveInProgress ? Color.Gray : Color.Green;
            TextureManager.DrawRect(spriteBatch, _startWaveButton, waveBtnColor);
            TextureManager.DrawRectOutline(spriteBatch, _startWaveButton, Color.White, 2);
            string waveText = waveInProgress ? "Wave Active..." : "Start Wave";
            Vector2 textSize = _font.MeasureString(waveText);
            spriteBatch.DrawString(
                _font,
                waveText,
                new Vector2(
                    _startWaveButton.X + (_startWaveButton.Width - textSize.X) / 2,
                    _startWaveButton.Y + (_startWaveButton.Height - textSize.Y) / 2
                ),
                Color.White
            );
        }
        else
        {
            // Fallback: no font loaded — draw colored blocks as indicators
            DrawButtonNoFont(spriteBatch, _gunButton, TowerType.Gun);
            DrawButtonNoFont(spriteBatch, _cannonButton, TowerType.Cannon);
            DrawButtonNoFont(spriteBatch, _championGunButton, TowerType.ChampionGun);
            DrawButtonNoFont(spriteBatch, _championCannonButton, TowerType.ChampionCannon);

            TextureManager.DrawRect(
                spriteBatch,
                _startWaveButton,
                waveInProgress ? Color.Gray : Color.Green
            );
            TextureManager.DrawRectOutline(spriteBatch, _startWaveButton, Color.White, 2);
        }
    }

    private void DrawButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        string label,
        TowerType type,
        bool canAfford,
        string? overlayText = null
    )
    {
        bool isSelected = SelectedTowerType == type;
        Color bgColor = isSelected ? Color.SlateGray : Color.DarkSlateGray;
        if (!canAfford)
            bgColor = Color.DarkGray;

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        // Tower color indicator
        var stats = TowerData.GetStats(type);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            stats.Color
        );

        if (_font != null)
        {
            spriteBatch.DrawString(
                _font,
                label,
                new Vector2(rect.X + 50, rect.Y + 15),
                canAfford ? Color.White : Color.DarkGray
            );

            // Draw overlay text if provided (e.g., "Champion Dead")
            if (overlayText != null && !canAfford)
            {
                spriteBatch.DrawString(
                    _font,
                    overlayText,
                    new Vector2(rect.X + 50, rect.Y + 30),
                    Color.Red
                );
            }
        }
    }

    private void DrawButtonNoFont(SpriteBatch spriteBatch, Rectangle rect, TowerType type)
    {
        bool isSelected = SelectedTowerType == type;
        Color bgColor = isSelected ? Color.SlateGray : Color.DarkSlateGray;
        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        var stats = TowerData.GetStats(type);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            stats.Color
        );
    }

    /// <summary>
    /// Helper to draw a generic tower button with champion placement check.
    /// Shows "Champion Dead" overlay only if the champion was placed and died (respawn cooldown active).
    /// </summary>
    private void DrawGenericTowerButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        string label,
        TowerType towerType,
        TowerType championVariant,
        int cost,
        int playerMoney
    )
    {
        bool canAfford = playerMoney >= cost;
        bool canPlace = canAfford && (_championManager?.CanPlaceGeneric(towerType) ?? true);
        bool hasRespawnCooldown = (_championManager?.GetRespawnCooldown(championVariant) ?? 0f) > 0;
        string? overlay = (canAfford && !canPlace && hasRespawnCooldown) ? "Champion Dead" : null;

        DrawButton(spriteBatch, rect, label, towerType, canPlace, overlay);
    }

    private void DrawChampionButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        string label,
        TowerType championType
    )
    {
        bool isSelected = SelectedTowerType == championType;

        // Default fallback if no champion manager
        if (_championManager == null)
        {
            DrawButton(spriteBatch, rect, $"{label} (Free)", championType, canAfford: true);
            return;
        }

        // Determine button state and cooldown overlay text
        Color bgColor;
        Color textColor = Color.White;
        string? cooldownText = null;

        if (isSelected)
        {
            bgColor = Color.SlateGray;
        }
        else if (_championManager.IsChampionAlive(championType))
        {
            // Champion already placed - locked
            bgColor = Color.DarkGray;
            textColor = Color.DarkGray;
            cooldownText = "Limit Reached";
        }
        else
        {
            float globalCooldown = _championManager.GlobalCooldownRemaining;
            float respawnCooldown = _championManager.GetRespawnCooldown(championType);

            if (globalCooldown > 0)
            {
                bgColor = Color.DarkSlateGray;
                cooldownText = $"Global: {globalCooldown:F1}s";
            }
            else if (respawnCooldown > 0)
            {
                bgColor = Color.DarkSlateGray;
                cooldownText = $"Respawn: {respawnCooldown:F1}s";
            }
            else
            {
                // Available to place
                bgColor = Color.DarkSlateGray;
            }
        }

        // Draw button background and outline (matching DrawButton pattern)
        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        // Draw tower color indicator (matching DrawButton pattern)
        var stats = TowerData.GetStats(championType);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            stats.Color
        );

        // Draw label text
        if (_font != null)
        {
            spriteBatch.DrawString(
                _font,
                $"{label} (Free)",
                new Vector2(rect.X + 50, rect.Y + 10),
                textColor
            );

            // Draw cooldown overlay text if present
            if (cooldownText != null)
            {
                spriteBatch.DrawString(
                    _font,
                    cooldownText,
                    new Vector2(rect.X + 50, rect.Y + 30),
                    Color.Yellow
                );
            }
        }
    }
}
