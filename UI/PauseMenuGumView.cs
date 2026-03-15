using System;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum;
using RenderingLibrary.Graphics;

namespace StarterTD.UI;

/// <summary>
/// Gum view model for pause menu interactions.
/// Encapsulates button creation, layout updates, and cleanup.
/// </summary>
internal sealed class PauseMenuGumView : IDisposable
{
    private static readonly GumMenuButtonStyle TransparentBlockerStyle = new(
        BackgroundColor: Color.Transparent,
        FocusedIndicatorColor: Color.Transparent,
        ForegroundColor: Color.Transparent
    );

    private static readonly GumMenuButtonStyle OverlayStyle = new(
        BackgroundColor: Color.Black * 0.4f,
        FocusedIndicatorColor: Color.Transparent,
        ForegroundColor: Color.Transparent
    );

    private static readonly GumMenuButtonStyle ResumeStyle = new(
        BackgroundColor: new Color(58, 76, 96),
        FocusedIndicatorColor: new Color(188, 206, 224),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle MapSelectionStyle = new(
        BackgroundColor: new Color(74, 56, 44),
        FocusedIndicatorColor: new Color(220, 190, 160),
        ForegroundColor: Color.White
    );

    private readonly Panel _rootPanel;
    private readonly Button _overlayVisualButton;
    private readonly Button _inputBlockerButton;
    private readonly Button _resumeButton;
    private readonly Button _mapSelectionButton;
    private readonly EventHandler _inputBlockerClickHandler;
    private readonly EventHandler _resumeClickHandler;
    private readonly EventHandler _mapSelectionClickHandler;
    private bool _isDisposed;

    public PauseMenuGumView(Action onResumeClicked, Action onMapSelectionClicked)
    {
        _rootPanel = new Panel
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Name = "PauseMenuRoot",
        };

        // Full-screen translucent overlay. This is non-interactive so it never changes hover state.
        _overlayVisualButton = GumMenuButtonFactory.Create(string.Empty, 1f, 1f, OverlayStyle);
        _overlayVisualButton.Visual.HasEvents = false;
        _inputBlockerButton = GumMenuButtonFactory.Create(
            string.Empty,
            1f,
            1f,
            TransparentBlockerStyle
        );
        _resumeButton = GumMenuButtonFactory.Create("Resume (P/ESC)", 1f, 1f, ResumeStyle);
        _mapSelectionButton = GumMenuButtonFactory.Create(
            "Map Selection",
            1f,
            1f,
            MapSelectionStyle
        );

        _inputBlockerClickHandler = (_, _) => { };
        _resumeClickHandler = (_, _) => onResumeClicked();
        _mapSelectionClickHandler = (_, _) => onMapSelectionClicked();
        _inputBlockerButton.Click += _inputBlockerClickHandler;
        _resumeButton.Click += _resumeClickHandler;
        _mapSelectionButton.Click += _mapSelectionClickHandler;

        _rootPanel.AddChild(_overlayVisualButton);
        _rootPanel.AddChild(_inputBlockerButton);
        _rootPanel.AddChild(_resumeButton);
        _rootPanel.AddChild(_mapSelectionButton);
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

    public void UpdateLayout(Rectangle resumeBounds, Rectangle mapSelectionBounds)
    {
        if (_isDisposed)
            return;

        SetButtonBounds(
            _overlayVisualButton,
            new Rectangle(
                0,
                0,
                Math.Max(1, (int)_rootPanel.Width),
                Math.Max(1, (int)_rootPanel.Height)
            )
        );
        SetButtonBounds(
            _inputBlockerButton,
            new Rectangle(
                0,
                0,
                Math.Max(1, (int)_rootPanel.Width),
                Math.Max(1, (int)_rootPanel.Height)
            )
        );
        SetButtonBounds(_resumeButton, resumeBounds);
        SetButtonBounds(_mapSelectionButton, mapSelectionBounds);
        _rootPanel.Visual.UpdateLayout();
        _rootPanel.Visual.UpdateToFontValues();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _inputBlockerButton.Click -= _inputBlockerClickHandler;
        _resumeButton.Click -= _resumeClickHandler;
        _mapSelectionButton.Click -= _mapSelectionClickHandler;

        if (_rootPanel.Visual.Parent != null)
            _rootPanel.Visual.Parent.RemoveChild(_rootPanel.Visual);

        _rootPanel.Visual.RemoveFromManagers();
        _isDisposed = true;
    }

    private static void SetButtonBounds(Button button, Rectangle bounds)
    {
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
}
