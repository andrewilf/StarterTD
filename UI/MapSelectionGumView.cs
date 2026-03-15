using System;
using System.Collections.Generic;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameGum;
using RenderingLibrary.Graphics;
using StarterTD.Engine;

namespace StarterTD.UI;

/// <summary>
/// Gum view for map-selection interactions.
/// Uses an explicit Back button plus transparent card overlays for per-map clicks.
/// </summary>
internal sealed class MapSelectionGumView : IDisposable
{
    private static readonly GumMenuButtonStyle BackStyle = new(
        BackgroundColor: new Color(58, 76, 96),
        FocusedIndicatorColor: new Color(188, 206, 224),
        ForegroundColor: Color.White
    );

    private static readonly GumMenuButtonStyle TransparentOverlayStyle = new(
        BackgroundColor: Color.Transparent,
        FocusedIndicatorColor: Color.Transparent,
        ForegroundColor: Color.Transparent
    );

    private readonly Panel _rootPanel;
    private readonly Button _backButton;
    private readonly List<Button> _mapCardButtons;
    private readonly EventHandler _backClickHandler;
    private readonly List<EventHandler> _mapClickHandlers;
    private bool _isDisposed;

    public MapSelectionGumView(
        IReadOnlyList<MapData> maps,
        Action onBackClicked,
        Action<int> onMapClicked
    )
    {
        _rootPanel = new Panel
        {
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
            Name = "MapSelectionRoot",
        };

        _backButton = GumMenuButtonFactory.Create("Back", 150f, 52f, BackStyle);
        _backClickHandler = (_, _) => onBackClicked();
        _backButton.Click += _backClickHandler;
        _rootPanel.AddChild(_backButton);

        _mapCardButtons = new List<Button>(maps.Count);
        _mapClickHandlers = new List<EventHandler>(maps.Count);
        for (int i = 0; i < maps.Count; i++)
        {
            int index = i;
            var button = GumMenuButtonFactory.Create(string.Empty, 1f, 1f, TransparentOverlayStyle);
            var clickHandler = new EventHandler((_, _) => onMapClicked(index));
            button.Click += clickHandler;
            _mapCardButtons.Add(button);
            _mapClickHandlers.Add(clickHandler);
            _rootPanel.AddChild(button);
        }
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

    public void UpdateLayout(IReadOnlyList<RectangleF> cardBounds, RectangleF backButtonBounds)
    {
        if (_isDisposed)
            return;

        if (cardBounds.Count != _mapCardButtons.Count)
            return;

        SetButtonBounds(_backButton, backButtonBounds);
        for (int i = 0; i < _mapCardButtons.Count; i++)
            SetButtonBounds(_mapCardButtons[i], cardBounds[i]);

        _rootPanel.Visual.UpdateLayout();
        _rootPanel.Visual.UpdateToFontValues();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _backButton.Click -= _backClickHandler;

        for (int i = 0; i < _mapCardButtons.Count; i++)
            _mapCardButtons[i].Click -= _mapClickHandlers[i];

        if (_rootPanel.Visual.Parent != null)
            _rootPanel.Visual.Parent.RemoveChild(_rootPanel.Visual);

        _rootPanel.Visual.RemoveFromManagers();
        _isDisposed = true;
    }

    private static void SetButtonBounds(Button button, RectangleF bounds)
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
