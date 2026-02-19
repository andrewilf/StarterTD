using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

public static class HighGroundVariantTile
{
    public static readonly TileStats Stats = new(
        Name: "High Ground Variant",
        MovementCost: int.MaxValue,
        Color: Color.OliveDrab,
        IsBuildable: true
    );
}
