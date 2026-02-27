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
    private readonly Rectangle _timeSlowButton;
    private readonly Rectangle _timeSlowBarBg;

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

        // Debug buttons pushed up to leave room for time-slow button above Start Wave
        int debugStartY = _height - 270;
        _placeHighGroundButton = new Rectangle(_x + 10, debugStartY, buttonWidth, buttonHeight);
        _spawnEnemyButton = new Rectangle(
            _x + 10,
            debugStartY + buttonHeight + gap,
            buttonWidth,
            buttonHeight
        );

        _timeSlowButton = new Rectangle(_x + 10, _height - 140, buttonWidth, buttonHeight);
        _timeSlowBarBg = new Rectangle(
            _timeSlowButton.X,
            _timeSlowButton.Bottom + 4,
            _timeSlowButton.Width,
            6
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
    public bool HandleClick(Point mousePos, IReadOnlyDictionary<TowerType, float> cooldowns)
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

        if (_timeSlowButton.Contains(mousePos))
        {
            // Block activation when bank is below the minimum threshold.
            if (!IsTimeSlowed && !CanActivateTimeSlow)
                return true;
            IsTimeSlowed = !IsTimeSlowed;
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
