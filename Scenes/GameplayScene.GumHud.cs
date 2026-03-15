using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StarterTD.Engine;
using StarterTD.Entities;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Gum HUD integration for gameplay-side buttons.
/// Keeps button input/rendering in Gum while preserving existing gameplay behavior.
/// </summary>
public partial class GameplayScene
{
    private static readonly GumMenuButtonStyle SelectedButtonStyle = new(
        BackgroundColor: new Color(84, 86, 112),
        FocusedIndicatorColor: new Color(250, 230, 118),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle DisabledButtonStyle = new(
        BackgroundColor: new Color(52, 52, 52),
        FocusedIndicatorColor: new Color(102, 102, 102),
        ForegroundColor: new Color(150, 150, 150)
    );

    private static readonly GumMenuButtonStyle AbilityCooldownStyle = new(
        BackgroundColor: new Color(64, 64, 64),
        FocusedIndicatorColor: new Color(140, 140, 140),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle AbilityUnavailableStyle = new(
        BackgroundColor: new Color(48, 48, 48),
        FocusedIndicatorColor: new Color(110, 110, 110),
        ForegroundColor: new Color(150, 150, 150)
    );

    private static readonly GumMenuButtonStyle DebugButtonStyle = new(
        BackgroundColor: new Color(78, 58, 34),
        FocusedIndicatorColor: new Color(230, 198, 153),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle DebugSelectedStyle = new(
        BackgroundColor: new Color(112, 84, 44),
        FocusedIndicatorColor: new Color(250, 224, 160),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle TimeSlowBaseStyle = new(
        BackgroundColor: new Color(20, 60, 80),
        FocusedIndicatorColor: new Color(120, 220, 255),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle TimeSlowActiveStyle = new(
        BackgroundColor: new Color(0, 92, 128),
        FocusedIndicatorColor: new Color(140, 240, 255),
        ForegroundColor: new Color(232, 250, 255)
    );

    private static readonly GumMenuButtonStyle SellButtonStyle = new(
        BackgroundColor: new Color(130, 0, 0),
        FocusedIndicatorColor: new Color(214, 110, 110),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle HealingModeStyle = new(
        BackgroundColor: new Color(18, 55, 88),
        FocusedIndicatorColor: new Color(90, 235, 255),
        ForegroundColor: new Color(195, 255, 215)
    );

    private static readonly GumMenuButtonStyle HealingModeAttackStyle = new(
        BackgroundColor: new Color(85, 18, 18),
        FocusedIndicatorColor: new Color(255, 110, 70),
        ForegroundColor: new Color(255, 215, 120)
    );

    private static readonly GumMenuButtonStyle WallModeStyle = new(
        BackgroundColor: new Color(20, 60, 20),
        FocusedIndicatorColor: new Color(140, 220, 140),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle WallModeActiveStyle = new(
        BackgroundColor: new Color(8, 96, 24),
        FocusedIndicatorColor: new Color(180, 255, 180),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle TowerPlacementStyle = new(
        BackgroundColor: new Color(50, 70, 92),
        FocusedIndicatorColor: new Color(182, 212, 236),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle GunAbilityStyle = new(
        BackgroundColor: new Color(40, 64, 84),
        FocusedIndicatorColor: new Color(160, 205, 240),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle CannonAbilityStyle = new(
        BackgroundColor: new Color(56, 52, 40),
        FocusedIndicatorColor: new Color(212, 194, 147),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle WallAbilityStyle = new(
        BackgroundColor: new Color(34, 70, 40),
        FocusedIndicatorColor: new Color(156, 218, 164),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle HealingAbilityStyle = new(
        BackgroundColor: new Color(38, 74, 56),
        FocusedIndicatorColor: new Color(176, 232, 200),
        ForegroundColor: Color.White
    );

    private void InitializeGameplayHudGum()
    {
        var handlers = new Dictionary<GameplayHudButtonId, Action>
        {
            [GameplayHudButtonId.GunTower] = () =>
                HandleUiPanelButtonClick(_uiPanel.GunTowerButtonRect),
            [GameplayHudButtonId.GunAbility] = () =>
                HandleUiPanelButtonClick(_uiPanel.GunAbilityButtonRect),
            [GameplayHudButtonId.CannonTower] = () =>
                HandleUiPanelButtonClick(_uiPanel.CannonTowerButtonRect),
            [GameplayHudButtonId.CannonAbility] = () =>
                HandleUiPanelButtonClick(_uiPanel.CannonAbilityButtonRect),
            [GameplayHudButtonId.WallTower] = () =>
                HandleUiPanelButtonClick(_uiPanel.WallTowerButtonRect),
            [GameplayHudButtonId.WallAbility] = () =>
                HandleUiPanelButtonClick(_uiPanel.WallAbilityButtonRect),
            [GameplayHudButtonId.HealingTower] = () =>
                HandleUiPanelButtonClick(_uiPanel.HealingTowerButtonRect),
            [GameplayHudButtonId.HealingAbility] = () =>
                HandleUiPanelButtonClick(_uiPanel.HealingAbilityButtonRect),
            [GameplayHudButtonId.PlaceHighGround] = () =>
                HandleUiPanelButtonClick(_uiPanel.PlaceHighGroundButtonRect),
            [GameplayHudButtonId.SpawnEnemy] = () =>
                HandleUiPanelButtonClick(_uiPanel.SpawnEnemyButtonRect),
            [GameplayHudButtonId.TimeSlow] = () =>
                HandleUiPanelButtonClick(_uiPanel.TimeSlowButtonRect),
            [GameplayHudButtonId.Sell] = HandleSellButtonClick,
            [GameplayHudButtonId.HealingMode] = HandleHealingModeButtonClick,
            [GameplayHudButtonId.WallPlacementMode] = HandleWallModeButtonClick,
        };

        _gameplayHudView = new GameplayHudGumView(handlers);
        _gameplayHudView.ResizeToViewport(GameSettings.ScreenWidth, GameSettings.ScreenHeight);
        _gameplayHudView.AttachToRoot();
        UpdateGameplayHudGum();
    }

    private void UpdateGameplayHudGum()
    {
        if (_gameplayHudView == null)
            return;

        _gameplayHudView.ResizeToViewport(GameSettings.ScreenWidth, GameSettings.ScreenHeight);

        // Static panel layout from UIPanel.
        _gameplayHudView.SetButtonBounds(GameplayHudButtonId.GunTower, _uiPanel.GunTowerButtonRect);
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.GunAbility,
            _uiPanel.GunAbilityButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.CannonTower,
            _uiPanel.CannonTowerButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.CannonAbility,
            _uiPanel.CannonAbilityButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.WallTower,
            _uiPanel.WallTowerButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.WallAbility,
            _uiPanel.WallAbilityButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.HealingTower,
            _uiPanel.HealingTowerButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.HealingAbility,
            _uiPanel.HealingAbilityButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.PlaceHighGround,
            _uiPanel.PlaceHighGroundButtonRect
        );
        _gameplayHudView.SetButtonBounds(
            GameplayHudButtonId.SpawnEnemy,
            _uiPanel.SpawnEnemyButtonRect
        );
        _gameplayHudView.SetButtonBounds(GameplayHudButtonId.TimeSlow, _uiPanel.TimeSlowButtonRect);

        UpdateTowerPlacementButtons();
        UpdateAbilityButtons();
        UpdateDebugButtons();
        UpdateTimeSlowButton();
        UpdateWorldButtons();
    }

    private void UpdateTowerPlacementButtons()
    {
        float championCooldown = _placementCooldowns.GetValueOrDefault(TowerType.ChampionGun);

        UpdateConsolidatedTowerButton(
            GameplayHudButtonId.GunTower,
            genericType: TowerType.Gun,
            championType: TowerType.ChampionGun,
            genericCooldown: _placementCooldowns.GetValueOrDefault(TowerType.Gun),
            championCooldown,
            baseStyle: TowerPlacementStyle
        );

        UpdateConsolidatedTowerButton(
            GameplayHudButtonId.CannonTower,
            genericType: TowerType.Cannon,
            championType: TowerType.ChampionCannon,
            genericCooldown: _placementCooldowns.GetValueOrDefault(TowerType.Cannon),
            championCooldown,
            baseStyle: TowerPlacementStyle
        );

        UpdateConsolidatedTowerButton(
            GameplayHudButtonId.WallTower,
            genericType: TowerType.Walling,
            championType: TowerType.ChampionWalling,
            genericCooldown: _placementCooldowns.GetValueOrDefault(TowerType.Walling),
            championCooldown,
            baseStyle: TowerPlacementStyle
        );

        bool isSelected = _uiPanel.SelectedTowerType == TowerType.ChampionHealing;
        float globalCooldown = _championManager.GlobalCooldownRemaining;
        float respawnCooldown = _championManager.GetRespawnCooldown(TowerType.ChampionHealing);
        bool isAlive = _championManager.IsChampionAlive(TowerType.ChampionHealing);
        bool isCooldownLocked = championCooldown > 0f;
        bool enabled =
            !isAlive
            && !isCooldownLocked
            && globalCooldown <= 0f
            && respawnCooldown <= 0f
            && _championManager.CanPlaceChampion(TowerType.ChampionHealing);

        string text;
        if (isAlive)
            text = "Healing Champion (Active)";
        else if (isCooldownLocked)
            text = $"Healing Champion (Locked {championCooldown:F1}s)";
        else if (globalCooldown > 0f)
            text = $"Healing Champion (Global {globalCooldown:F1}s)";
        else if (respawnCooldown > 0f)
            text = $"Healing Champion (Respawn {respawnCooldown:F1}s)";
        else
            text = "Healing Champion";

        GumMenuButtonStyle style = enabled ? TowerPlacementStyle : DisabledButtonStyle;
        if (isSelected)
            style = SelectedButtonStyle;

        _gameplayHudView!.SetButtonState(GameplayHudButtonId.HealingTower, text, enabled, style);
    }

    private void UpdateConsolidatedTowerButton(
        GameplayHudButtonId buttonId,
        TowerType genericType,
        TowerType championType,
        float genericCooldown,
        float championCooldown,
        GumMenuButtonStyle baseStyle
    )
    {
        bool championAlive = _championManager.IsChampionAlive(championType);
        bool isSelected =
            _uiPanel.SelectedTowerType == genericType || _uiPanel.SelectedTowerType == championType;

        string text;
        bool enabled;
        if (!championAlive)
        {
            float globalCooldown = _championManager.GlobalCooldownRemaining;
            float respawnCooldown = _championManager.GetRespawnCooldown(championType);

            if (championCooldown > 0f)
                text = $"{genericType} Champion (Locked {championCooldown:F1}s)";
            else if (globalCooldown > 0f)
                text = $"{genericType} Champion (Global {globalCooldown:F1}s)";
            else if (respawnCooldown > 0f)
                text = $"{genericType} Champion (Respawn {respawnCooldown:F1}s)";
            else
                text = $"{genericType} Champion";

            enabled =
                championCooldown <= 0f
                && globalCooldown <= 0f
                && respawnCooldown <= 0f
                && _championManager.CanPlaceChampion(championType);
        }
        else
        {
            text =
                genericCooldown > 0f
                    ? $"{genericType} (Locked {genericCooldown:F1}s)"
                    : genericType.ToString();
            enabled = genericCooldown <= 0f && _championManager.CanPlaceGeneric(genericType);
        }

        GumMenuButtonStyle style = enabled ? baseStyle : DisabledButtonStyle;
        if (isSelected)
            style = SelectedButtonStyle;

        _gameplayHudView!.SetButtonState(buttonId, text, enabled, style);
    }

    private void UpdateAbilityButtons()
    {
        UpdateAbilityButton(
            GameplayHudButtonId.GunAbility,
            TowerType.ChampionGun,
            "Gun Ability",
            GunAbilityStyle
        );
        UpdateAbilityButton(
            GameplayHudButtonId.CannonAbility,
            TowerType.ChampionCannon,
            "Cannon Ability",
            CannonAbilityStyle
        );
        UpdateAbilityButton(
            GameplayHudButtonId.WallAbility,
            TowerType.ChampionWalling,
            "Walling Ability",
            WallAbilityStyle
        );
        UpdateAbilityButton(
            GameplayHudButtonId.HealingAbility,
            TowerType.ChampionHealing,
            "Healing Ability",
            HealingAbilityStyle
        );
    }

    private void UpdateAbilityButton(
        GameplayHudButtonId buttonId,
        TowerType championType,
        string label,
        GumMenuButtonStyle readyBaseStyle
    )
    {
        bool championAlive = _championManager.IsChampionAlive(championType);
        float cooldown = _championManager.GetAbilityCooldownRemaining(championType);
        bool ready = _championManager.IsAbilityReady(championType);

        string text;
        if (!championAlive)
            text = $"{label} (N/A)";
        else if (ready)
            text = $"{label} (Ready)";
        else
            text = $"{label} ({cooldown:F1}s)";

        bool enabled = ready;
        GumMenuButtonStyle style;
        if (ready)
            style = readyBaseStyle;
        else if (championAlive)
            style = AbilityCooldownStyle;
        else
            style = AbilityUnavailableStyle;

        _gameplayHudView!.SetButtonState(buttonId, text, enabled, style);
    }

    private void UpdateDebugButtons()
    {
        bool isHighGroundSelected = _uiPanel.SelectionMode == UISelectionMode.PlaceHighGround;
        bool isSpawnSelected = _uiPanel.SelectionMode == UISelectionMode.SpawnEnemy;

        _gameplayHudView!.SetButtonState(
            GameplayHudButtonId.PlaceHighGround,
            "Place High Ground",
            true,
            isHighGroundSelected ? DebugSelectedStyle : DebugButtonStyle
        );
        _gameplayHudView.SetButtonState(
            GameplayHudButtonId.SpawnEnemy,
            "Spawn Enemy",
            true,
            isSpawnSelected ? DebugSelectedStyle : DebugButtonStyle
        );
    }

    private void UpdateTimeSlowButton()
    {
        bool canToggle = _uiPanel.IsTimeSlowed || _uiPanel.CanActivateTimeSlow;
        string text = _uiPanel.IsTimeSlowed ? ">> 0.5x Speed <<" : "Time Slow";
        GumMenuButtonStyle style;
        if (_uiPanel.IsTimeSlowed)
            style = TimeSlowActiveStyle;
        else if (canToggle)
            style = TimeSlowBaseStyle;
        else
            style = DisabledButtonStyle;

        _gameplayHudView!.SetButtonState(GameplayHudButtonId.TimeSlow, text, canToggle, style);
    }

    private void UpdateWorldButtons()
    {
        var selectedTower = _towerManager.SelectedTower;
        if (selectedTower == null)
        {
            HideWorldButtons();
            return;
        }

        Rectangle sellScreenRect = WorldToScreenRect(GetSellButtonRect(selectedTower));
        _gameplayHudView!.SetButtonBounds(GameplayHudButtonId.Sell, sellScreenRect);
        _gameplayHudView.SetButtonState(
            GameplayHudButtonId.Sell,
            "X",
            true,
            SellButtonStyle,
            isVisible: true
        );

        if (selectedTower is HealingChampionTower healingChampionTower)
        {
            Rectangle modeScreenRect = WorldToScreenRect(
                GetHealingModeButtonRect(healingChampionTower)
            );
            _gameplayHudView.SetButtonBounds(GameplayHudButtonId.HealingMode, modeScreenRect);
            bool isCooldownActive = healingChampionTower.ModeToggleCooldownRemaining > 0f;
            bool isAttackMode = healingChampionTower.Mode == HealingChampionMode.Attack;
            string modeText;
            if (isCooldownActive)
                modeText = MathF
                    .Ceiling(healingChampionTower.ModeToggleCooldownRemaining)
                    .ToString("0");
            else if (isAttackMode)
                modeText = ">";
            else
                modeText = "+";

            GumMenuButtonStyle modeStyle;
            if (isCooldownActive)
                modeStyle = DisabledButtonStyle;
            else if (isAttackMode)
                modeStyle = HealingModeAttackStyle;
            else
                modeStyle = HealingModeStyle;

            _gameplayHudView.SetButtonState(
                GameplayHudButtonId.HealingMode,
                modeText,
                !isCooldownActive,
                modeStyle,
                isVisible: true
            );
        }
        else
        {
            _gameplayHudView.SetButtonVisible(GameplayHudButtonId.HealingMode, false);
        }

        if (
            selectedTower.TowerType.IsWallingChampion()
            || selectedTower.TowerType.IsWallingGeneric()
        )
        {
            Rectangle wallModeScreenRect = WorldToScreenRect(
                GetWallPlacementButtonRect(selectedTower)
            );
            _gameplayHudView.SetButtonBounds(
                GameplayHudButtonId.WallPlacementMode,
                wallModeScreenRect
            );
            _gameplayHudView.SetButtonState(
                GameplayHudButtonId.WallPlacementMode,
                "+",
                true,
                _wallPlacementMode ? WallModeActiveStyle : WallModeStyle,
                isVisible: true
            );
        }
        else
        {
            _gameplayHudView.SetButtonVisible(GameplayHudButtonId.WallPlacementMode, false);
        }
    }

    private void HandleUiPanelButtonClick(Rectangle buttonRect)
    {
        _uiPanel.HandleClick(buttonRect.Center, _placementCooldowns);

        if (_uiPanel.SelectedTowerType.HasValue)
            DeselectAll();

        if (_uiPanel.SpawnEnemyClicked)
            SpawnDebugEnemy();
    }

    private void HandleSellButtonClick()
    {
        var selectedTower = _towerManager.SelectedTower;
        if (selectedTower == null)
            return;

        float penalty = _towerManager.SellTower(selectedTower);
        var poolKey = GetCooldownPoolKey(selectedTower.TowerType);
        _placementCooldowns[poolKey] = Math.Max(0f, _placementCooldowns[poolKey] - penalty);
        DeselectAll();
    }

    private void HandleHealingModeButtonClick()
    {
        if (_towerManager.SelectedTower is not HealingChampionTower)
            return;

        _towerManager.TryToggleHealingChampionMode();
    }

    private void HandleWallModeButtonClick()
    {
        var selectedWalling = GetSelectedWallingAnchor();
        if (selectedWalling == null)
            return;

        _wallPlacementMode = !_wallPlacementMode;
        if (!_wallPlacementMode)
            CancelWallDrag();
    }

    private void HideWorldButtons()
    {
        _gameplayHudView!.SetButtonVisible(GameplayHudButtonId.Sell, false);
        _gameplayHudView.SetButtonVisible(GameplayHudButtonId.HealingMode, false);
        _gameplayHudView.SetButtonVisible(GameplayHudButtonId.WallPlacementMode, false);
    }

    private bool IsWorldGumButtonHit(Point screenPos)
    {
        if (_gameplayHudView == null)
            return false;

        var selectedTower = _towerManager.SelectedTower;
        if (selectedTower == null)
            return false;

        if (WorldToScreenRect(GetSellButtonRect(selectedTower)).Contains(screenPos))
            return true;

        if (
            selectedTower is HealingChampionTower healingChampionTower
            && WorldToScreenRect(GetHealingModeButtonRect(healingChampionTower)).Contains(screenPos)
        )
        {
            return true;
        }

        if (
            (
                selectedTower.TowerType.IsWallingChampion()
                || selectedTower.TowerType.IsWallingGeneric()
            ) && WorldToScreenRect(GetWallPlacementButtonRect(selectedTower)).Contains(screenPos)
        )
        {
            return true;
        }

        return false;
    }

    private Rectangle WorldToScreenRect(Rectangle worldRect)
    {
        int offsetX = (int)MathF.Round(_mapOffset.X);
        int offsetY = (int)MathF.Round(_mapOffset.Y);
        return new Rectangle(
            worldRect.X + offsetX,
            worldRect.Y + offsetY,
            worldRect.Width,
            worldRect.Height
        );
    }
}
