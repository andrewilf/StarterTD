using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.Interfaces;
using StarterTD.Managers;

namespace StarterTD.UI;

/// <summary>
/// Selection mode for UI interactions.
/// </summary>
public enum UISelectionMode
{
    None,
    PlaceTower,
    PlaceHighGround,
    SpawnEnemy,
}

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

    /// <summary>Current UI selection mode.</summary>
    public UISelectionMode SelectionMode { get; private set; }

    // Consolidated tower buttons (each covers champion + generic for that type)
    private readonly Rectangle _gunTowerButton;
    private readonly Rectangle _gunAbilityButton;
    private readonly Rectangle _cannonTowerButton;
    private readonly Rectangle _cannonAbilityButton;

    // Walling tower button (champion-only; wall placement is handled via world-space button)
    private readonly Rectangle _wallTowerButton;

    private readonly Rectangle _startWaveButton;

    /// <summary>
    /// Fired when the player clicks a ready ability button.
    /// Passes the champion tower type (e.g. ChampionGun). GameplayScene wires this.
    /// </summary>
    public Action<TowerType>? OnAbilityTriggered;

    // Debug button rectangles
    private readonly Rectangle _placeHighGroundButton;
    private readonly Rectangle _spawnEnemyButton;

    /// <summary>Whether the "Start Wave" button was clicked this frame.</summary>
    public bool StartWaveClicked { get; private set; }

    /// <summary>Whether the "Spawn Enemy" button was clicked this frame.</summary>
    public bool SpawnEnemyClicked { get; private set; }

    private SpriteFont? _font;

    public UIPanel(int screenWidth, int screenHeight, ChampionManager? championManager = null)
    {
        _width = GameSettings.UIPanelWidth;
        _x = screenWidth - _width;
        _height = screenHeight;
        _championManager = championManager;

        int buttonWidth = _width - 20;
        int buttonHeight = 50;
        int abilityButtonHeight = 28;
        int startY = 120;
        int gap = 10;
        int abilityGap = 5;

        _gunTowerButton = new Rectangle(_x + 10, startY, buttonWidth, buttonHeight);
        _gunAbilityButton = new Rectangle(
            _x + 10,
            startY + buttonHeight + abilityGap,
            buttonWidth,
            abilityButtonHeight
        );

        int cannonStartY = startY + buttonHeight + abilityGap + abilityButtonHeight + gap;
        _cannonTowerButton = new Rectangle(_x + 10, cannonStartY, buttonWidth, buttonHeight);
        _cannonAbilityButton = new Rectangle(
            _x + 10,
            cannonStartY + buttonHeight + abilityGap,
            buttonWidth,
            abilityButtonHeight
        );

        int wallStartY = cannonStartY + buttonHeight + abilityGap + abilityButtonHeight + gap;
        _wallTowerButton = new Rectangle(_x + 10, wallStartY, buttonWidth, buttonHeight);

        // Debug buttons positioned below instructions (above Start Wave button)
        int debugStartY = _height - 200;
        _placeHighGroundButton = new Rectangle(_x + 10, debugStartY, buttonWidth, buttonHeight);
        _spawnEnemyButton = new Rectangle(
            _x + 10,
            debugStartY + buttonHeight + gap,
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
        SpawnEnemyClicked = false;

        // Debug buttons
        if (_placeHighGroundButton.Contains(mousePos))
        {
            SelectionMode = UISelectionMode.PlaceHighGround;
            SelectedTowerType = null;
            return true;
        }
        if (_spawnEnemyButton.Contains(mousePos))
        {
            SelectionMode = UISelectionMode.SpawnEnemy;
            SelectedTowerType = null;
            SpawnEnemyClicked = true;
            return true;
        }

        // Consolidated tower buttons: place champion if dead, generic if champion is alive
        if (_gunTowerButton.Contains(mousePos))
        {
            HandleConsolidatedTowerClick(TowerType.Gun, TowerType.ChampionGun, playerMoney);
            return true;
        }
        if (_gunAbilityButton.Contains(mousePos))
        {
            if (_championManager?.IsAbilityReady(TowerType.ChampionGun) ?? false)
                OnAbilityTriggered?.Invoke(TowerType.ChampionGun);
            return true;
        }
        if (_cannonTowerButton.Contains(mousePos))
        {
            HandleConsolidatedTowerClick(TowerType.Cannon, TowerType.ChampionCannon, playerMoney);
            return true;
        }
        if (_cannonAbilityButton.Contains(mousePos))
        {
            if (_championManager?.IsAbilityReady(TowerType.ChampionCannon) ?? false)
                OnAbilityTriggered?.Invoke(TowerType.ChampionCannon);
            return true;
        }

        if (_wallTowerButton.Contains(mousePos))
        {
            HandleWallChampionClick();
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

    // Walling champion has no generic variant — button only places/re-places the champion.
    private void HandleWallChampionClick()
    {
        bool championAlive = _championManager?.IsChampionAlive(TowerType.ChampionWalling) ?? false;
        if (championAlive)
            return; // Already on field; player uses world-space button to place walls

        bool canPlace = _championManager?.CanPlaceChampion(TowerType.ChampionWalling) ?? true;
        SelectedTowerType = canPlace ? TowerType.ChampionWalling : null;
        SelectionMode = canPlace ? UISelectionMode.PlaceTower : UISelectionMode.None;
    }

    // Champion dead → place champion. Champion alive → place generic.
    private void HandleConsolidatedTowerClick(
        TowerType genericType,
        TowerType championType,
        int playerMoney
    )
    {
        bool championAlive = _championManager?.IsChampionAlive(championType) ?? false;
        if (!championAlive)
        {
            bool canPlace = _championManager?.CanPlaceChampion(championType) ?? true;
            SelectedTowerType = canPlace ? championType : null;
            SelectionMode = canPlace ? UISelectionMode.PlaceTower : UISelectionMode.None;
        }
        else
        {
            var stats = TowerData.GetStats(genericType);
            bool canPlace =
                playerMoney >= stats.Cost
                && (_championManager?.CanPlaceGeneric(genericType) ?? true);
            SelectedTowerType = canPlace ? genericType : null;
            SelectionMode = canPlace ? UISelectionMode.PlaceTower : UISelectionMode.None;
        }
    }

    /// <summary>
    /// Clear all selections (called when ESC is pressed).
    /// </summary>
    public void ClearSelection()
    {
        SelectedTowerType = null;
        SelectionMode = UISelectionMode.None;
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
        bool waveInProgress,
        Tower? selectedTower = null,
        IEnemy? selectedEnemy = null
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

            // --- Tower buttons (champion if dead, generic if champion alive) ---
            DrawConsolidatedTowerButton(
                spriteBatch,
                _gunTowerButton,
                TowerType.Gun,
                TowerType.ChampionGun,
                50,
                money
            );
            DrawAbilityButton(spriteBatch, _gunAbilityButton, TowerType.ChampionGun);

            DrawConsolidatedTowerButton(
                spriteBatch,
                _cannonTowerButton,
                TowerType.Cannon,
                TowerType.ChampionCannon,
                80,
                money
            );
            DrawAbilityButton(spriteBatch, _cannonAbilityButton, TowerType.ChampionCannon);

            DrawWallChampionButton(spriteBatch, _wallTowerButton);

            // --- Info text ---
            spriteBatch.DrawString(
                _font,
                "L-Click: Place",
                new Vector2(_x + 10, _wallTowerButton.Bottom + 15),
                Color.LightGray
            );
            spriteBatch.DrawString(
                _font,
                "R-Click: Sell",
                new Vector2(_x + 10, _wallTowerButton.Bottom + 35),
                Color.LightGray
            );
            spriteBatch.DrawString(
                _font,
                "P: Pause",
                new Vector2(_x + 10, _wallTowerButton.Bottom + 55),
                Color.LightGray
            );
            spriteBatch.DrawString(
                _font,
                "ESC: Deselect",
                new Vector2(_x + 10, _wallTowerButton.Bottom + 75),
                Color.LightGray
            );

            // --- Debug section ---
            spriteBatch.DrawString(
                _font,
                "Debug Tools:",
                new Vector2(_x + 10, _placeHighGroundButton.Top - 30),
                Color.Orange
            );

            DrawDebugButton(
                spriteBatch,
                _placeHighGroundButton,
                "Place High Ground",
                UISelectionMode.PlaceHighGround
            );

            DrawDebugButton(
                spriteBatch,
                _spawnEnemyButton,
                "Spawn Enemy",
                UISelectionMode.SpawnEnemy
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
            // Fallback: no font loaded — draw colored blocks as indicators.
            // Show champion color when dead, generic color when alive.
            bool gunChampAlive = _championManager?.IsChampionAlive(TowerType.ChampionGun) ?? false;
            bool cannonChampAlive =
                _championManager?.IsChampionAlive(TowerType.ChampionCannon) ?? false;

            DrawButtonNoFont(
                spriteBatch,
                _gunTowerButton,
                gunChampAlive ? TowerType.Gun : TowerType.ChampionGun
            );
            DrawButtonNoFont(
                spriteBatch,
                _cannonTowerButton,
                cannonChampAlive ? TowerType.Cannon : TowerType.ChampionCannon
            );
            DrawButtonNoFont(spriteBatch, _wallTowerButton, TowerType.ChampionWalling);

            TextureManager.DrawRect(
                spriteBatch,
                _startWaveButton,
                waveInProgress ? Color.Gray : Color.Green
            );
            TextureManager.DrawRectOutline(spriteBatch, _startWaveButton, Color.White, 2);
        }

        if (selectedTower != null)
            DrawTowerInfoPanel(spriteBatch, selectedTower);
        else if (selectedEnemy != null)
            DrawEnemyInfoPanel(spriteBatch, selectedEnemy);
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
    /// Draws a single button that acts as the champion button when the champion is dead,
    /// and switches to the generic button once the champion is alive on the field.
    /// </summary>
    private void DrawConsolidatedTowerButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        TowerType genericType,
        TowerType championType,
        int genericCost,
        int playerMoney
    )
    {
        bool championAlive = _championManager?.IsChampionAlive(championType) ?? false;
        bool isSelected = SelectedTowerType == genericType || SelectedTowerType == championType;

        Color bgColor;
        Color textColor = Color.White;
        string mainLabel;
        string? subLabel = null;
        TowerType indicatorType;

        if (!championAlive)
        {
            indicatorType = championType;
            mainLabel = $"{genericType} (Free)";

            float globalCooldown = _championManager?.GlobalCooldownRemaining ?? 0f;
            float respawnCooldown = _championManager?.GetRespawnCooldown(championType) ?? 0f;

            if (globalCooldown > 0)
            {
                bgColor = Color.DarkSlateGray;
                subLabel = $"Global: {globalCooldown:F1}s";
                textColor = Color.DarkGray;
            }
            else if (respawnCooldown > 0)
            {
                bgColor = Color.DarkSlateGray;
                subLabel = $"Respawn: {respawnCooldown:F1}s";
                textColor = Color.DarkGray;
            }
            else
            {
                bgColor = Color.DarkSlateGray;
                subLabel = "Place Champion";
            }
        }
        else
        {
            indicatorType = genericType;
            bool canAfford = playerMoney >= genericCost;
            mainLabel = $"{genericType} (${genericCost})";
            bgColor = canAfford ? Color.DarkSlateGray : Color.DarkGray;
            textColor = canAfford ? Color.White : Color.DarkGray;
            if (!canAfford)
                subLabel = "Can't Afford";
        }

        if (isSelected)
            bgColor = Color.SlateGray;

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        var stats = TowerData.GetStats(indicatorType);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            stats.Color
        );

        spriteBatch.DrawString(_font, mainLabel, new Vector2(rect.X + 50, rect.Y + 10), textColor);

        if (subLabel != null)
        {
            Color subColor;
            if (subLabel == "Place Champion")
                subColor = Color.LightGreen;
            else if (subLabel == "Can't Afford")
                subColor = Color.Red;
            else
                subColor = Color.Yellow; // cooldown text
            spriteBatch.DrawString(
                _font,
                subLabel,
                new Vector2(rect.X + 50, rect.Y + 28),
                subColor
            );
        }
    }

    /// <summary>
    /// Draws a semi-transparent info panel at the bottom of the UI showing stats for the selected tower.
    /// Positioned above the Start Wave button.
    /// </summary>
    private void DrawTowerInfoPanel(SpriteBatch spriteBatch, Tower tower)
    {
        if (_font == null)
            return;

        const int padding = 8;
        const int lineHeight = 20;
        int panelWidth = _width - 12;
        int numLines = 6; // name + separator + HP + block + damage + fire rate
        int panelHeight = padding * 2 + numLines * lineHeight;
        int panelX = _x + 6;
        int panelY = _startWaveButton.Y - panelHeight - 10;

        // Semi-transparent background
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Black * 0.75f
        );
        TextureManager.DrawRectOutline(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Gray,
            1
        );

        int textX = panelX + padding;
        int y = panelY + padding;

        // Tower name with color indicator
        var colorRect = new Rectangle(textX, y + 2, 12, 12);
        TextureManager.DrawRect(spriteBatch, colorRect, tower.TowerColor);
        spriteBatch.DrawString(_font, tower.Name, new Vector2(textX + 18, y), Color.White);
        y += lineHeight;

        // Thin separator line
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(textX, y + 2, panelWidth - padding * 2, 1),
            Color.Gray
        );
        y += lineHeight - 4;

        // HP: current / max
        string hpText = $"HP: {tower.CurrentHealth} / {tower.MaxHealth}";
        spriteBatch.DrawString(_font, hpText, new Vector2(textX, y), Color.LimeGreen);
        y += lineHeight;

        // Block capacity: remaining / max
        int remaining = Math.Max(0, tower.BlockCapacity - tower.CurrentEngagedCount);
        string blockText = $"Block: {remaining} / {tower.BlockCapacity}";
        spriteBatch.DrawString(_font, blockText, new Vector2(textX, y), Color.CornflowerBlue);
        y += lineHeight;

        // Damage
        string dmgText = tower.IsAOE ? $"Damage: {tower.Damage} (AOE)" : $"Damage: {tower.Damage}";
        spriteBatch.DrawString(_font, dmgText, new Vector2(textX, y), Color.White);
        y += lineHeight;

        // Fire rate (show as attacks per second for readability)
        float aps = 1f / tower.FireRate;
        string fireText = $"Fire Rate: {aps:F1}/s";
        spriteBatch.DrawString(_font, fireText, new Vector2(textX, y), Color.White);
    }

    private void DrawEnemyInfoPanel(SpriteBatch spriteBatch, IEnemy enemy)
    {
        if (_font == null)
            return;

        const int padding = 8;
        const int lineHeight = 20;
        int panelWidth = _width - 12;
        int numLines = 6; // name + separator + health + speed + bounty + attack damage
        int panelHeight = padding * 2 + numLines * lineHeight;
        int panelX = _x + 6;
        int panelY = _startWaveButton.Y - panelHeight - 10;

        // Semi-transparent background
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Black * 0.75f
        );
        TextureManager.DrawRectOutline(
            spriteBatch,
            new Rectangle(panelX, panelY, panelWidth, panelHeight),
            Color.Gray,
            1
        );

        int textX = panelX + padding;
        int y = panelY + padding;

        // Enemy name with color indicator
        var colorRect = new Rectangle(textX, y + 2, 12, 12);
        TextureManager.DrawRect(spriteBatch, colorRect, Color.OrangeRed);
        spriteBatch.DrawString(_font, enemy.Name, new Vector2(textX + 18, y), Color.White);
        y += lineHeight;

        // Thin separator line
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(textX, y + 2, panelWidth - padding * 2, 1),
            Color.Gray
        );
        y += lineHeight - 4;

        // Health: current / max
        string healthText = $"Health: {enemy.Health:F1} / {enemy.MaxHealth:F1}";
        spriteBatch.DrawString(_font, healthText, new Vector2(textX, y), Color.LimeGreen);
        y += lineHeight;

        // Speed
        string speedText = $"Speed: {enemy.Speed:F1}";
        spriteBatch.DrawString(_font, speedText, new Vector2(textX, y), Color.CornflowerBlue);
        y += lineHeight;

        // Bounty
        string bountyText = $"Bounty: ${enemy.Bounty}";
        spriteBatch.DrawString(_font, bountyText, new Vector2(textX, y), Color.Gold);
        y += lineHeight;

        // Attack Damage
        string dmgText = $"Attack Dmg: {enemy.AttackDamage}";
        spriteBatch.DrawString(_font, dmgText, new Vector2(textX, y), Color.White);
    }

    /// <summary>
    /// Draws the walling champion button. Since it has no generic variant, the button always
    /// shows champion placement state (ready, on cooldown, or already placed).
    /// </summary>
    private void DrawWallChampionButton(SpriteBatch spriteBatch, Rectangle rect)
    {
        bool championAlive = _championManager?.IsChampionAlive(TowerType.ChampionWalling) ?? false;
        bool isSelected = SelectedTowerType == TowerType.ChampionWalling;

        Color bgColor = isSelected ? Color.SlateGray : Color.DarkSlateGray;
        Color textColor = Color.White;
        string mainLabel = "Walling (Free)";
        string? subLabel;

        if (championAlive)
        {
            subLabel = "On Field";
            textColor = Color.DarkGray;
            bgColor = Color.DarkSlateGray;
        }
        else
        {
            float globalCooldown = _championManager?.GlobalCooldownRemaining ?? 0f;
            float respawnCooldown =
                _championManager?.GetRespawnCooldown(TowerType.ChampionWalling) ?? 0f;

            if (globalCooldown > 0)
            {
                subLabel = $"Global: {globalCooldown:F1}s";
                textColor = Color.DarkGray;
            }
            else if (respawnCooldown > 0)
            {
                subLabel = $"Respawn: {respawnCooldown:F1}s";
                textColor = Color.DarkGray;
            }
            else
            {
                subLabel = "Place Champion";
            }
        }

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        var stats = TowerData.GetStats(TowerType.ChampionWalling);
        TextureManager.DrawRect(
            spriteBatch,
            new Rectangle(rect.X + 8, rect.Y + 8, 34, 34),
            stats.Color
        );

        if (_font != null)
        {
            spriteBatch.DrawString(
                _font,
                mainLabel,
                new Vector2(rect.X + 50, rect.Y + 10),
                textColor
            );

            Color subColor;
            if (subLabel == "Place Champion")
                subColor = Color.LightGreen;
            else if (subLabel == "On Field")
                subColor = Color.DarkGray;
            else
                subColor = Color.Yellow;
            spriteBatch.DrawString(
                _font,
                subLabel,
                new Vector2(rect.X + 50, rect.Y + 28),
                subColor
            );
        }
    }

    private void DrawAbilityButton(SpriteBatch spriteBatch, Rectangle rect, TowerType championType)
    {
        bool championAlive = _championManager?.IsChampionAlive(championType) ?? false;
        float cooldown = _championManager?.GetAbilityCooldownRemaining(championType) ?? 0f;

        Color bgColor;
        string label;
        Color textColor;
        Color outlineColor;

        if (!championAlive)
        {
            bgColor = new Color(40, 40, 40);
            label = "ABILITY (no champion)";
            textColor = Color.DarkGray;
            outlineColor = Color.DarkGray;
        }
        else if (cooldown > 0f)
        {
            bgColor = new Color(50, 50, 30);
            label = $"ABILITY CD: {cooldown:F1}s";
            textColor = Color.Yellow;
            outlineColor = Color.DarkGoldenrod;
        }
        else
        {
            bgColor = new Color(30, 80, 20);
            label = "USE ABILITY!";
            textColor = Color.LimeGreen;
            outlineColor = Color.LimeGreen;
        }

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(spriteBatch, rect, outlineColor, 1);

        if (_font != null)
        {
            Vector2 textSize = _font.MeasureString(label);
            spriteBatch.DrawString(
                _font,
                label,
                new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2f,
                    rect.Y + (rect.Height - textSize.Y) / 2f
                ),
                textColor
            );
        }
    }

    private void DrawDebugButton(
        SpriteBatch spriteBatch,
        Rectangle rect,
        string label,
        UISelectionMode mode
    )
    {
        bool isSelected = SelectionMode == mode;
        Color bgColor = isSelected ? Color.DarkOrange : new Color(60, 40, 20);

        TextureManager.DrawRect(spriteBatch, rect, bgColor);
        TextureManager.DrawRectOutline(
            spriteBatch,
            rect,
            isSelected ? Color.Yellow : Color.Gray,
            2
        );

        if (_font != null)
        {
            Vector2 textSize = _font.MeasureString(label);
            spriteBatch.DrawString(
                _font,
                label,
                new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2,
                    rect.Y + (rect.Height - textSize.Y) / 2
                ),
                Color.White
            );
        }
    }
}
