using System;
using System.Collections.Generic;
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
    private int _x;
    private readonly int _width;
    private int _height;
    private readonly ChampionManager? _championManager;

    /// <summary>Which tower type the player has selected to place (null = none).</summary>
    public TowerType? SelectedTowerType { get; set; }

    /// <summary>Current UI selection mode.</summary>
    public UISelectionMode SelectionMode { get; private set; }

    // Consolidated tower buttons (each covers champion + generic for that type)
    private Rectangle _gunTowerButton;
    private Rectangle _gunAbilityButton;
    private Rectangle _cannonTowerButton;
    private Rectangle _cannonAbilityButton;
    private Rectangle _wallTowerButton;
    private Rectangle _wallAbilityButton;
    private Rectangle _healingTowerButton;
    private Rectangle _healingAbilityButton;

    private Rectangle _timeSlowButton;
    private Rectangle _timeSlowBarBg;

    /// <summary>
    /// Fired when the player clicks a ready ability button.
    /// Passes the champion tower type (e.g. ChampionGun). GameplayScene wires this.
    /// </summary>
    public Action<TowerType>? OnAbilityTriggered;

    // Debug button rectangles
    private Rectangle _placeHighGroundButton;
    private Rectangle _spawnEnemyButton;

    /// <summary>Whether time-slow mode is currently active (persistent toggle).</summary>
    public bool IsTimeSlowed { get; private set; }

    /// <summary>
    /// Set by GameplayScene each frame. False when bank &lt; minimum — blocks activation.
    /// </summary>
    public bool CanActivateTimeSlow { get; set; }

    /// <summary>Called by GameplayScene when the bank hits 0 to force time-slow off.</summary>
    public void ForceDeactivateTimeSlow() => IsTimeSlowed = false;

    /// <summary>Whether the "Spawn Enemy" button was clicked this frame.</summary>
    public bool SpawnEnemyClicked { get; private set; }

    private SpriteFont? _font;

    public UIPanel(int screenWidth, int screenHeight, ChampionManager? championManager = null)
    {
        _width = GameSettings.UIPanelWidth;
        _championManager = championManager;
        ApplyLayout(screenWidth, screenHeight);
    }

    /// <summary>
    /// Rebuild the panel's screen-space layout for a new viewport size while keeping state.
    /// </summary>
    public void Resize(int screenWidth, int screenHeight)
    {
        ApplyLayout(screenWidth, screenHeight);
    }

    private void ApplyLayout(int screenWidth, int screenHeight)
    {
        _x = screenWidth - _width;
        _height = screenHeight;

        int buttonWidth = _width - 20;
        int buttonHeight = 50;
        int abilityButtonHeight = 28;
        int startY = 120;
        int gap = 10;
        int abilityGap = 5;
        int debugHeaderReservedHeight = 38;
        int debugBottomGap = 20;

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

        int healingStartY = wallStartY + buttonHeight + abilityGap + abilityButtonHeight + gap;
        _healingTowerButton = new Rectangle(_x + 10, healingStartY, buttonWidth, buttonHeight);
        _healingAbilityButton = new Rectangle(
            _x + 10,
            healingStartY + buttonHeight + abilityGap,
            buttonWidth,
            abilityButtonHeight
        );

        _timeSlowButton = new Rectangle(_x + 10, _height - 70, buttonWidth, buttonHeight);
        _timeSlowBarBg = new Rectangle(
            _timeSlowButton.X,
            _timeSlowButton.Bottom + 4,
            _timeSlowButton.Width,
            6
        );

        int minimumDebugStartY = _healingAbilityButton.Bottom + debugHeaderReservedHeight;
        int debugButtonsHeight = buttonHeight * 2 + gap;
        int preferredDebugStartY = _timeSlowButton.Top - debugButtonsHeight - debugBottomGap;
        int debugStartY = Math.Max(preferredDebugStartY, minimumDebugStartY);
        _placeHighGroundButton = new Rectangle(_x + 10, debugStartY, buttonWidth, buttonHeight);
        _spawnEnemyButton = new Rectangle(
            _x + 10,
            debugStartY + buttonHeight + gap,
            buttonWidth,
            buttonHeight
        );
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

    public Rectangle GunTowerButtonRect => _gunTowerButton;

    public Rectangle GunAbilityButtonRect => _gunAbilityButton;

    public Rectangle CannonTowerButtonRect => _cannonTowerButton;

    public Rectangle CannonAbilityButtonRect => _cannonAbilityButton;

    public Rectangle WallTowerButtonRect => _wallTowerButton;

    public Rectangle WallAbilityButtonRect => _wallAbilityButton;

    public Rectangle HealingTowerButtonRect => _healingTowerButton;

    public Rectangle HealingAbilityButtonRect => _healingAbilityButton;

    public Rectangle PlaceHighGroundButtonRect => _placeHighGroundButton;

    public Rectangle SpawnEnemyButtonRect => _spawnEnemyButton;

    public Rectangle TimeSlowButtonRect => _timeSlowButton;

    public Rectangle TimeSlowBarRect => _timeSlowBarBg;

    /// <summary>
    /// Handle click input on the UI panel. Returns true if the click was consumed.
    /// </summary>
    public bool HandleClick(Point mousePos, IReadOnlyDictionary<TowerType, float> cooldowns)
    {
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

        float championCooldown = cooldowns.GetValueOrDefault(TowerType.ChampionGun);

        // Consolidated tower buttons: place champion if dead, generic if champion is alive
        if (_gunTowerButton.Contains(mousePos))
        {
            HandleConsolidatedTowerClick(
                TowerType.Gun,
                TowerType.ChampionGun,
                cooldowns.GetValueOrDefault(TowerType.Gun),
                championCooldown
            );
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
            HandleConsolidatedTowerClick(
                TowerType.Cannon,
                TowerType.ChampionCannon,
                cooldowns.GetValueOrDefault(TowerType.Cannon),
                championCooldown
            );
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
            HandleConsolidatedTowerClick(
                TowerType.Walling,
                TowerType.ChampionWalling,
                cooldowns.GetValueOrDefault(TowerType.Walling),
                championCooldown
            );
            return true;
        }
        if (_wallAbilityButton.Contains(mousePos))
        {
            if (_championManager?.IsAbilityReady(TowerType.ChampionWalling) ?? false)
                OnAbilityTriggered?.Invoke(TowerType.ChampionWalling);
            return true;
        }

        if (_healingTowerButton.Contains(mousePos))
        {
            HandleChampionOnlyTowerClick(TowerType.ChampionHealing, championCooldown);
            return true;
        }
        if (_healingAbilityButton.Contains(mousePos))
        {
            if (_championManager?.IsAbilityReady(TowerType.ChampionHealing) ?? false)
                OnAbilityTriggered?.Invoke(TowerType.ChampionHealing);
            return true;
        }

        if (_timeSlowButton.Contains(mousePos))
        {
            // Block activation when bank is below the minimum threshold.
            if (!IsTimeSlowed && !CanActivateTimeSlow)
                return true;
            IsTimeSlowed = !IsTimeSlowed;
            return true;
        }

        return false;
    }

    // Champion dead → place champion (if champion pool ready). Champion alive → place generic (if generic pool ready).
    private void HandleConsolidatedTowerClick(
        TowerType genericType,
        TowerType championType,
        float genericCooldown,
        float championCooldown
    )
    {
        bool championAlive = _championManager?.IsChampionAlive(championType) ?? false;
        if (!championAlive)
        {
            if (championCooldown > 0f)
            {
                SelectedTowerType = null;
                SelectionMode = UISelectionMode.None;
                return;
            }
            bool canPlace = _championManager?.CanPlaceChampion(championType) ?? true;
            SelectedTowerType = canPlace ? championType : null;
            SelectionMode = canPlace ? UISelectionMode.PlaceTower : UISelectionMode.None;
        }
        else
        {
            if (genericCooldown > 0f)
            {
                SelectedTowerType = null;
                SelectionMode = UISelectionMode.None;
                return;
            }
            bool canPlace = _championManager?.CanPlaceGeneric(genericType) ?? true;
            SelectedTowerType = canPlace ? genericType : null;
            SelectionMode = canPlace ? UISelectionMode.PlaceTower : UISelectionMode.None;
        }
    }

    private void HandleChampionOnlyTowerClick(TowerType championType, float championCooldown)
    {
        if (championCooldown > 0f)
        {
            SelectedTowerType = null;
            SelectionMode = UISelectionMode.None;
            return;
        }

        bool canPlace = _championManager?.CanPlaceChampion(championType) ?? true;
        SelectedTowerType = canPlace ? championType : null;
        SelectionMode = canPlace ? UISelectionMode.PlaceTower : UISelectionMode.None;
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
