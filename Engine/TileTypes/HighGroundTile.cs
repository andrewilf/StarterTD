using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

public static class HighGroundTile
{
    public static readonly TileStats Stats = new(
        Name: "High Ground",
        MovementCost: int.MaxValue,
        Color: Color.ForestGreen,
        IsBuildable: true
    );
}
