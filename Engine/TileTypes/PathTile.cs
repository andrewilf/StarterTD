using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

public static class PathTile
{
    public static readonly TileStats Stats = new(
        Name: "Path",
        MovementCost: 1,
        Color: Color.Tan,
        IsBuildable: true
    );
}
