using System;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum;

namespace StarterTD.UI;

/// <summary>
/// Gum view model for the start menu's button stack.
/// Encapsulates control creation, attachment, resize, and cleanup.
/// </summary>
internal sealed class StartMenuGumView : IDisposable
{
    private const float ButtonWidth = 280f;
    private const float ButtonHeight = 62f;
    private const float ButtonSpacing = 18f;

    private static readonly GumMenuButtonStyle StartStyle = new(
        BackgroundColor: new Color(26, 92, 92),
        FocusedIndicatorColor: new Color(120, 220, 210),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle SettingsStyle = new(
        BackgroundColor: new Color(58, 72, 92),
        FocusedIndicatorColor: new Color(176, 196, 222),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle ExitStyle = new(
        BackgroundColor: new Color(110, 44, 44),
        FocusedIndicatorColor: new Color(214, 124, 124),
        ForegroundColor: Color.White
    );

    private readonly Panel _rootPanel;
    private readonly Button _startButton;
    private readonly Button _settingsButton;
    private readonly Button _exitButton;

    private readonly EventHandler _startClickHandler;
    private readonly EventHandler _settingsClickHandler;
    private readonly EventHandler _exitClickHandler;
    private bool _isDisposed;

    public StartMenuGumView(Action onStartClicked, Action onSettingsClicked, Action onExitClicked)
    {
        _rootPanel = new Panel
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Name = "StartMenuRoot",
        };

        var buttonStack = new StackPanel
        {
            Width = ButtonWidth,
            Height = (ButtonHeight * 3f) + (ButtonSpacing * 2f),
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Orientation = Orientation.Vertical,
            Spacing = ButtonSpacing,
            Name = "StartMenuButtonStack",
        };
        buttonStack.Anchor(Anchor.Center);
        buttonStack.Y = 34f;

        _startButton = GumMenuButtonFactory.Create("Start", ButtonWidth, ButtonHeight, StartStyle);
        _settingsButton = GumMenuButtonFactory.Create(
            "Settings",
            ButtonWidth,
            ButtonHeight,
            SettingsStyle
        );
        _exitButton = GumMenuButtonFactory.Create("Exit", ButtonWidth, ButtonHeight, ExitStyle);

        _startClickHandler = (_, _) => onStartClicked();
        _settingsClickHandler = (_, _) => onSettingsClicked();
        _exitClickHandler = (_, _) => onExitClicked();
        _startButton.Click += _startClickHandler;
        _settingsButton.Click += _settingsClickHandler;
        _exitButton.Click += _exitClickHandler;

        buttonStack.AddChild(_startButton);
        buttonStack.AddChild(_settingsButton);
        buttonStack.AddChild(_exitButton);
        _rootPanel.AddChild(buttonStack);
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

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _startButton.Click -= _startClickHandler;
        _settingsButton.Click -= _settingsClickHandler;
        _exitButton.Click -= _exitClickHandler;
        DetachFromRoot();
        _isDisposed = true;
    }

    private void DetachFromRoot()
    {
        if (_rootPanel.Visual.Parent != null)
            _rootPanel.Visual.Parent.RemoveChild(_rootPanel.Visual);

        _rootPanel.Visual.RemoveFromManagers();
    }
}
