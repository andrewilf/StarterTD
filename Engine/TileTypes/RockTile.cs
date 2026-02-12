using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

public static class RockTile
{
    public static readonly TileStats Stats = new(
        Name: "Rock",
        MovementCost: int.MaxValue,
        Color: Color.DarkGray,
        IsBuildable: false
    );
}
