using System;
using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum;
using RenderingLibrary.Graphics;

namespace StarterTD.UI;

internal enum GameplayHudButtonId
{
    GunTower,
    GunAbility,
    CannonTower,
    CannonAbility,
    WallTower,
    WallAbility,
    HealingTower,
    HealingAbility,
    PlaceHighGround,
    SpawnEnemy,
    TimeSlow,
    Sell,
    HealingMode,
    WallPlacementMode,
}

internal sealed class GameplayHudGumView : IDisposable
{
    private readonly record struct ButtonStateSnapshot(
        string Text,
        bool IsEnabled,
        bool IsVisible,
        GumMenuButtonStyle Style
    );

    private readonly Panel _rootPanel;
    private readonly Dictionary<GameplayHudButtonId, Button> _buttons = new();
    private readonly List<(Button button, EventHandler handler)> _registeredHandlers = new();
    private readonly Dictionary<GameplayHudButtonId, Rectangle> _boundsCache = new();
    private readonly Dictionary<GameplayHudButtonId, ButtonStateSnapshot> _stateCache = new();
    private bool _isDisposed;

    public GameplayHudGumView(IReadOnlyDictionary<GameplayHudButtonId, Action> handlers)
    {
        _rootPanel = new Panel
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Name = "GameplayHudRoot",
        };

        CreateButton(
            GameplayHudButtonId.GunTower,
            "Gun",
            GameplayHudStyles.TowerPlacement,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.GunAbility,
            "Gun Ability",
            GameplayHudStyles.GunAbility,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.CannonTower,
            "Cannon",
            GameplayHudStyles.TowerPlacement,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.CannonAbility,
            "Cannon Ability",
            GameplayHudStyles.CannonAbility,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.WallTower,
            "Walling",
            GameplayHudStyles.TowerPlacement,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.WallAbility,
            "Walling Ability",
            GameplayHudStyles.WallAbility,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.HealingTower,
            "Healing Champion",
            GameplayHudStyles.TowerPlacement,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.HealingAbility,
            "Healing Ability",
            GameplayHudStyles.HealingAbility,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.PlaceHighGround,
            "Place High Ground",
            GameplayHudStyles.DebugButton,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.SpawnEnemy,
            "Spawn Enemy",
            GameplayHudStyles.DebugButton,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.TimeSlow,
            "Time Slow",
            GameplayHudStyles.TimeSlowBase,
            handlers
        );
        CreateButton(GameplayHudButtonId.Sell, "X", GameplayHudStyles.SellButton, handlers);
        CreateButton(GameplayHudButtonId.HealingMode, "M", GameplayHudStyles.HealingMode, handlers);
        CreateButton(
            GameplayHudButtonId.WallPlacementMode,
            "+",
            GameplayHudStyles.WallMode,
            handlers
        );

        // These in-world controls only show when a tower is selected.
        SetButtonVisible(GameplayHudButtonId.Sell, false);
        SetButtonVisible(GameplayHudButtonId.HealingMode, false);
        SetButtonVisible(GameplayHudButtonId.WallPlacementMode, false);
    }

    public void AttachToRoot()
    {
        if (_isDisposed)
            return;

        var gumRoot = GumService.Default.Root;
        if (gumRoot == null)
            return;

        if (_rootPanel.Visual.Parent == gumRoot)
            return;

        _rootPanel.Visual.Parent?.RemoveChild(_rootPanel.Visual);
        gumRoot.AddChild(_rootPanel.Visual);
        _rootPanel.Visual.UpdateLayout();
        _rootPanel.Visual.UpdateToFontValues();
    }

    public void ResizeToViewport(int viewportWidth, int viewportHeight)
    {
        if (_isDisposed)
            return;

        _rootPanel.Width = Math.Max(1, viewportWidth);
        _rootPanel.Height = Math.Max(1, viewportHeight);
        _rootPanel.Visual.UpdateLayout();
        _rootPanel.Visual.UpdateToFontValues();
    }

    public void SetButtonBounds(GameplayHudButtonId id, Rectangle bounds)
    {
        if (_isDisposed || !_buttons.TryGetValue(id, out var button))
            return;

        if (_boundsCache.TryGetValue(id, out Rectangle cachedBounds) && cachedBounds == bounds)
            return;

        button.XOrigin = HorizontalAlignment.Left;
        button.YOrigin = VerticalAlignment.Top;
        button.XUnits = GeneralUnitType.PixelsFromSmall;
        button.YUnits = GeneralUnitType.PixelsFromSmall;
        button.X = bounds.X;
        button.Y = bounds.Y;
        button.Width = bounds.Width;
        button.Height = bounds.Height;
        button.WidthUnits = DimensionUnitType.Absolute;
        button.HeightUnits = DimensionUnitType.Absolute;

        _boundsCache[id] = bounds;
    }

    public void SetButtonState(
        GameplayHudButtonId id,
        string text,
        bool isEnabled,
        GumMenuButtonStyle style,
        bool isVisible = true
    )
    {
        if (_isDisposed || !_buttons.TryGetValue(id, out var button))
            return;

        var state = new ButtonStateSnapshot(text, isEnabled, isVisible, style);
        bool hasCachedState = _stateCache.TryGetValue(id, out ButtonStateSnapshot cachedState);
        if (hasCachedState && cachedState == state)
            return;

        if (!hasCachedState)
        {
            button.Text = text;
            button.IsEnabled = isEnabled;
            button.IsVisible = isVisible;
            GumMenuButtonFactory.ApplyStyle(button, style);
            _stateCache[id] = state;
            return;
        }

        if (cachedState.Text != text)
            button.Text = text;

        if (cachedState.IsEnabled != isEnabled)
            button.IsEnabled = isEnabled;

        if (cachedState.IsVisible != isVisible)
            button.IsVisible = isVisible;

        if (cachedState.Style != style)
            GumMenuButtonFactory.ApplyStyle(button, style);

        _stateCache[id] = state;
    }

    public void SetButtonVisible(GameplayHudButtonId id, bool isVisible)
    {
        if (_isDisposed || !_buttons.TryGetValue(id, out var button))
            return;

        if (_stateCache.TryGetValue(id, out ButtonStateSnapshot cachedState))
        {
            if (cachedState.IsVisible == isVisible)
                return;

            _stateCache[id] = cachedState with { IsVisible = isVisible };
        }
        else if (button.IsVisible == isVisible)
        {
            return;
        }

        button.IsVisible = isVisible;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        foreach (var (button, handler) in _registeredHandlers)
            button.Click -= handler;

        if (_rootPanel.Visual.Parent != null)
            _rootPanel.Visual.Parent.RemoveChild(_rootPanel.Visual);

        _rootPanel.Visual.RemoveFromManagers();
        _boundsCache.Clear();
        _stateCache.Clear();
        _isDisposed = true;
    }

    private void CreateButton(
        GameplayHudButtonId id,
        string text,
        GumMenuButtonStyle style,
        IReadOnlyDictionary<GameplayHudButtonId, Action> handlers
    )
    {
        var button = GumMenuButtonFactory.Create(text, 1f, 1f, style);
        _buttons[id] = button;
        _stateCache[id] = new ButtonStateSnapshot(text, true, true, style);
        _rootPanel.AddChild(button);

        if (!handlers.TryGetValue(id, out var action))
            return;

        EventHandler handler = (_, _) => action();
        button.Click += handler;
        _registeredHandlers.Add((button, handler));
    }
}
