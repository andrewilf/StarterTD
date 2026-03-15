using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StarterTD.Entities;
using StarterTD.UI;

namespace StarterTD.Scenes;

/// <summary>
/// Gum HUD integration for gameplay-side buttons.
/// Keeps button input/rendering in Gum while preserving existing gameplay behavior.
/// </summary>
public partial class GameplayScene
{
    private enum HudCooldownLabelKind
    {
        Locked,
        Global,
        Respawn,
        AbilityCooldown,
    }

    private readonly Dictionary<int, string> _cooldownTenthsTextCache = new();
    private readonly Dictionary<
        (GameplayHudButtonId ButtonId, HudCooldownLabelKind Kind, int Tenths),
        string
    > _cooldownLabelCache = new();

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
        _gameplayHudView.ResizeToViewport(_layoutWidth, _layoutHeight);
        _gameplayHudView.AttachToRoot();
        UpdateGameplayHudGum();
    }

    private void UpdateGameplayHudGum()
    {
        if (_gameplayHudView == null)
            return;

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
            baseStyle: GameplayHudStyles.TowerPlacement
        );

        UpdateConsolidatedTowerButton(
            GameplayHudButtonId.CannonTower,
            genericType: TowerType.Cannon,
            championType: TowerType.ChampionCannon,
            genericCooldown: _placementCooldowns.GetValueOrDefault(TowerType.Cannon),
            championCooldown,
            baseStyle: GameplayHudStyles.TowerPlacement
        );

        UpdateConsolidatedTowerButton(
            GameplayHudButtonId.WallTower,
            genericType: TowerType.Walling,
            championType: TowerType.ChampionWalling,
            genericCooldown: _placementCooldowns.GetValueOrDefault(TowerType.Walling),
            championCooldown,
            baseStyle: GameplayHudStyles.TowerPlacement
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
            text = GetCooldownLabel(
                GameplayHudButtonId.HealingTower,
                "Healing Champion",
                HudCooldownLabelKind.Locked,
                championCooldown
            );
        else if (globalCooldown > 0f)
            text = GetCooldownLabel(
                GameplayHudButtonId.HealingTower,
                "Healing Champion",
                HudCooldownLabelKind.Global,
                globalCooldown
            );
        else if (respawnCooldown > 0f)
            text = GetCooldownLabel(
                GameplayHudButtonId.HealingTower,
                "Healing Champion",
                HudCooldownLabelKind.Respawn,
                respawnCooldown
            );
        else
            text = "Healing Champion";

        GumMenuButtonStyle style = enabled
            ? GameplayHudStyles.TowerPlacement
            : GameplayHudStyles.DisabledButton;
        if (isSelected)
            style = GameplayHudStyles.SelectedButton;

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
            string championLabel = $"{genericType} Champion";

            if (championCooldown > 0f)
                text = GetCooldownLabel(
                    buttonId,
                    championLabel,
                    HudCooldownLabelKind.Locked,
                    championCooldown
                );
            else if (globalCooldown > 0f)
                text = GetCooldownLabel(
                    buttonId,
                    championLabel,
                    HudCooldownLabelKind.Global,
                    globalCooldown
                );
            else if (respawnCooldown > 0f)
                text = GetCooldownLabel(
                    buttonId,
                    championLabel,
                    HudCooldownLabelKind.Respawn,
                    respawnCooldown
                );
            else
                text = championLabel;

            enabled =
                championCooldown <= 0f
                && globalCooldown <= 0f
                && respawnCooldown <= 0f
                && _championManager.CanPlaceChampion(championType);
        }
        else
        {
            string genericLabel = genericType.ToString();
            text =
                genericCooldown > 0f
                    ? GetCooldownLabel(
                        buttonId,
                        genericLabel,
                        HudCooldownLabelKind.Locked,
                        genericCooldown
                    )
                    : genericLabel;
            enabled = genericCooldown <= 0f && _championManager.CanPlaceGeneric(genericType);
        }

        GumMenuButtonStyle style = enabled ? baseStyle : GameplayHudStyles.DisabledButton;
        if (isSelected)
            style = GameplayHudStyles.SelectedButton;

        _gameplayHudView!.SetButtonState(buttonId, text, enabled, style);
    }

    private void UpdateAbilityButtons()
    {
        UpdateAbilityButton(
            GameplayHudButtonId.GunAbility,
            TowerType.ChampionGun,
            "Gun Ability",
            GameplayHudStyles.GunAbility
        );
        UpdateAbilityButton(
            GameplayHudButtonId.CannonAbility,
            TowerType.ChampionCannon,
            "Cannon Ability",
            GameplayHudStyles.CannonAbility
        );
        UpdateAbilityButton(
            GameplayHudButtonId.WallAbility,
            TowerType.ChampionWalling,
            "Walling Ability",
            GameplayHudStyles.WallAbility
        );
        UpdateAbilityButton(
            GameplayHudButtonId.HealingAbility,
            TowerType.ChampionHealing,
            "Healing Ability",
            GameplayHudStyles.HealingAbility
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
            text = GetCooldownLabel(
                buttonId,
                label,
                HudCooldownLabelKind.AbilityCooldown,
                cooldown
            );

        bool enabled = ready;
        GumMenuButtonStyle style;
        if (ready)
            style = readyBaseStyle;
        else if (championAlive)
            style = GameplayHudStyles.AbilityCooldown;
        else
            style = GameplayHudStyles.AbilityUnavailable;

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
            isHighGroundSelected ? GameplayHudStyles.DebugSelected : GameplayHudStyles.DebugButton
        );
        _gameplayHudView.SetButtonState(
            GameplayHudButtonId.SpawnEnemy,
            "Spawn Enemy",
            true,
            isSpawnSelected ? GameplayHudStyles.DebugSelected : GameplayHudStyles.DebugButton
        );
    }

    private void UpdateTimeSlowButton()
    {
        bool canToggle = _uiPanel.IsTimeSlowed || _uiPanel.CanActivateTimeSlow;
        string text = _uiPanel.IsTimeSlowed ? ">> 0.5x Speed <<" : "Time Slow";
        GumMenuButtonStyle style;
        if (_uiPanel.IsTimeSlowed)
            style = GameplayHudStyles.TimeSlowActive;
        else if (canToggle)
            style = GameplayHudStyles.TimeSlowBase;
        else
            style = GameplayHudStyles.DisabledButton;

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
            GameplayHudStyles.SellButton,
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
                modeStyle = GameplayHudStyles.DisabledButton;
            else if (isAttackMode)
                modeStyle = GameplayHudStyles.HealingModeAttack;
            else
                modeStyle = GameplayHudStyles.HealingMode;

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
                _wallPlacementMode ? GameplayHudStyles.WallModeActive : GameplayHudStyles.WallMode,
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

    private string GetCooldownLabel(
        GameplayHudButtonId buttonId,
        string label,
        HudCooldownLabelKind kind,
        float seconds
    )
    {
        int tenths = QuantizeCooldownToTenths(seconds);
        var cacheKey = (buttonId, kind, tenths);
        if (_cooldownLabelCache.TryGetValue(cacheKey, out string? cached))
            return cached;

        string cooldownText = GetCachedTenthsText(tenths);
        string formatted = kind switch
        {
            HudCooldownLabelKind.Locked => $"{label} (Locked {cooldownText}s)",
            HudCooldownLabelKind.Global => $"{label} (Global {cooldownText}s)",
            HudCooldownLabelKind.Respawn => $"{label} (Respawn {cooldownText}s)",
            HudCooldownLabelKind.AbilityCooldown => $"{label} ({cooldownText}s)",
            _ => $"{label} ({cooldownText}s)",
        };

        _cooldownLabelCache[cacheKey] = formatted;
        return formatted;
    }

    private string GetCachedTenthsText(int tenths)
    {
        if (_cooldownTenthsTextCache.TryGetValue(tenths, out string? cached))
            return cached;

        string formatted = (tenths / 10f).ToString("0.0");
        _cooldownTenthsTextCache[tenths] = formatted;
        return formatted;
    }

    private static int QuantizeCooldownToTenths(float seconds)
    {
        float clamped = Math.Max(0f, seconds);
        return (int)MathF.Ceiling(clamped * 10f);
    }
}
