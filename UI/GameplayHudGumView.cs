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
    private readonly Panel _rootPanel;
    private readonly Dictionary<GameplayHudButtonId, Button> _buttons = new();
    private readonly List<(Button button, EventHandler handler)> _registeredHandlers = new();
    private bool _isDisposed;

    public GameplayHudGumView(IReadOnlyDictionary<GameplayHudButtonId, Action> handlers)
    {
        var towerPlacementStyle = new GumMenuButtonStyle(
            new Color(50, 70, 92),
            new Color(182, 212, 236),
            Color.White
        );

        _rootPanel = new Panel
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Name = "GameplayHudRoot",
        };

        CreateButton(GameplayHudButtonId.GunTower, "Gun", towerPlacementStyle, handlers);
        CreateButton(
            GameplayHudButtonId.GunAbility,
            "Gun Ability",
            new GumMenuButtonStyle(new Color(40, 64, 84), new Color(160, 205, 240), Color.White),
            handlers
        );
        CreateButton(GameplayHudButtonId.CannonTower, "Cannon", towerPlacementStyle, handlers);
        CreateButton(
            GameplayHudButtonId.CannonAbility,
            "Cannon Ability",
            new GumMenuButtonStyle(new Color(56, 52, 40), new Color(212, 194, 147), Color.White),
            handlers
        );
        CreateButton(GameplayHudButtonId.WallTower, "Walling", towerPlacementStyle, handlers);
        CreateButton(
            GameplayHudButtonId.WallAbility,
            "Walling Ability",
            new GumMenuButtonStyle(new Color(34, 70, 40), new Color(156, 218, 164), Color.White),
            handlers
        );
        CreateButton(
            GameplayHudButtonId.HealingTower,
            "Healing Champion",
            towerPlacementStyle,
            handlers
        );
        CreateButton(
            GameplayHudButtonId.HealingAbility,
            "Healing Ability",
            new GumMenuButtonStyle(new Color(38, 74, 56), new Color(176, 232, 200), Color.White),
            handlers
        );
        CreateButton(
            GameplayHudButtonId.PlaceHighGround,
            "Place High Ground",
            new GumMenuButtonStyle(new Color(78, 58, 34), new Color(230, 198, 153), Color.White),
            handlers
        );
        CreateButton(
            GameplayHudButtonId.SpawnEnemy,
            "Spawn Enemy",
            new GumMenuButtonStyle(new Color(74, 46, 46), new Color(224, 148, 148), Color.White),
            handlers
        );
        CreateButton(
            GameplayHudButtonId.TimeSlow,
            "Time Slow",
            new GumMenuButtonStyle(new Color(20, 60, 80), new Color(120, 220, 255), Color.White),
            handlers
        );
        CreateButton(
            GameplayHudButtonId.Sell,
            "X",
            new GumMenuButtonStyle(new Color(130, 0, 0), new Color(214, 110, 110), Color.White),
            handlers
        );
        CreateButton(
            GameplayHudButtonId.HealingMode,
            "M",
            new GumMenuButtonStyle(new Color(18, 55, 88), new Color(90, 235, 255), Color.White),
            handlers
        );
        CreateButton(
            GameplayHudButtonId.WallPlacementMode,
            "+",
            new GumMenuButtonStyle(new Color(20, 60, 20), new Color(140, 220, 140), Color.White),
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

        button.Text = text;
        button.IsEnabled = isEnabled;
        button.IsVisible = isVisible;
        GumMenuButtonFactory.ApplyStyle(button, style);
    }

    public void SetButtonVisible(GameplayHudButtonId id, bool isVisible)
    {
        if (_isDisposed || !_buttons.TryGetValue(id, out var button))
            return;

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
        _rootPanel.AddChild(button);

        if (!handlers.TryGetValue(id, out var action))
            return;

        EventHandler handler = (_, _) => action();
        button.Click += handler;
        _registeredHandlers.Add((button, handler));
    }
}
