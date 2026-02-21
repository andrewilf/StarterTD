using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarterTD.Engine;
using StarterTD.Entities;
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
public partial class UIPanel
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
    private readonly Rectangle _wallAbilityButton;

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
        _wallAbilityButton = new Rectangle(
            _x + 10,
            wallStartY + buttonHeight + abilityGap,
            buttonWidth,
            abilityButtonHeight
        );

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
        if (_wallAbilityButton.Contains(mousePos))
        {
            if (_championManager?.IsAbilityReady(TowerType.ChampionWalling) ?? false)
                OnAbilityTriggered?.Invoke(TowerType.ChampionWalling);
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
}
