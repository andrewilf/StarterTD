using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework;

namespace StarterTD.UI;

internal readonly record struct GumMenuButtonStyle(
    Color BackgroundColor,
    Color FocusedIndicatorColor,
    Color ForegroundColor
);

internal static class GumMenuButtonFactory
{
    public static Button Create(string label, float width, float height, GumMenuButtonStyle style)
    {
        var visual = new ButtonVisual(fullInstantiation: true, tryCreateFormsObject: false)
        {
            BackgroundColor = style.BackgroundColor,
            FocusedIndicatorColor = style.FocusedIndicatorColor,
            ForegroundColor = style.ForegroundColor,
        };

        return new Button(visual)
        {
            Text = label,
            Width = width,
            Height = height,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute,
        };
    }

    public static void ApplyStyle(Button button, GumMenuButtonStyle style)
    {
        if (button.Visual is not ButtonVisual visual)
            return;

        visual.BackgroundColor = style.BackgroundColor;
        visual.FocusedIndicatorColor = style.FocusedIndicatorColor;
        visual.ForegroundColor = style.ForegroundColor;
        button.UpdateState();
    }
}
