using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;

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

    /// <summary>Which tower type the player has selected to place (null = none).</summary>
    public TowerType? SelectedTowerType { get; set; }

    // Button rectangles for tower selection
    private readonly Rectangle _gunButton;
    private readonly Rectangle _cannonButton;
    private readonly Rectangle _startWaveButton;

    /// <summary>Whether the "Start Wave" button was clicked this frame.</summary>
    public bool StartWaveClicked { get; private set; }

    private SpriteFont? _font;

    public UIPanel(int screenWidth, int screenHeight)
    {
        _width = GameSettings.UIPanelWidth;
        _x = screenWidth - _width;
        _height = screenHeight;

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

        if (_gunButton.Contains(mousePos))
        {
            var stats = TowerData.GetStats(TowerType.Gun, 1);
            SelectedTowerType = playerMoney >= stats.Cost ? TowerType.Gun : null;
            return true;
        }
        if (_cannonButton.Contains(mousePos))
        {
            var stats = TowerData.GetStats(TowerType.Cannon, 1);
            SelectedTowerType = playerMoney >= stats.Cost ? TowerType.Cannon : null;
            return true;
        }
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
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(_x, 0, _width, _height),
            new Color(40, 40, 50)
        );

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
            DrawButton(spriteBatch, _gunButton, "Gun ($50)", TowerType.Gun, money >= 50);
            DrawButton(spriteBatch, _cannonButton, "Cannon ($80)", TowerType.Cannon, money >= 80);

            // --- Info text ---
            spriteBatch.DrawString(
                _font,
                "L-Click: Place",
                new Vector2(_x + 10, _cannonButton.Bottom + 20),
                Color.LightGray
            );
            spriteBatch.DrawString(
                _font,
                "R-Click: Upgrade",
                new Vector2(_x + 10, _cannonButton.Bottom + 45),
                Color.LightGray
            );
            spriteBatch.DrawString(
                _font,
                "ESC: Deselect",
                new Vector2(_x + 10, _cannonButton.Bottom + 70),
                Color.LightGray
            );

            // --- Start Wave button ---
            Color waveBtnColor = waveInProgress ? new Color(80, 80, 80) : new Color(0, 120, 0);
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

            TextureManager.DrawRect(
                spriteBatch,
                _startWaveButton,
                waveInProgress ? new Color(80, 80, 80) : new Color(0, 120, 0)
            );
            TextureManager.DrawRectOutline(spriteBatch, _startWaveButton, Color.White, 2);
        }
    }

    private void DrawButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        string label,
        TowerType type,
        bool canAfford
    )
    {
        bool isSelected = SelectedTowerType == type;
        Color bgColor = isSelected ? new Color(80, 80, 120) : new Color(60, 60, 70);
        if (!canAfford)
            bgColor = new Color(40, 40, 40);

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        // Tower color indicator
        var stats = TowerData.GetStats(type, 1);
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
        }
    }

    private void DrawButtonNoFont(SpriteBatch spriteBatch, Rectangle rect, TowerType type)
    {
        bool isSelected = SelectedTowerType == type;
        Color bgColor = isSelected ? new Color(80, 80, 120) : new Color(60, 60, 70);
        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        var stats = TowerData.GetStats(type, 1);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            stats.Color
        );
    }
}
