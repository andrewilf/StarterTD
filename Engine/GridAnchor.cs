using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Grid anchor in half-tile units.
/// Example with TileSize=32:
/// - (1,1) is center of tile (0,0)
/// - (2,2) is the corner intersection between the first 2x2 tiles.
/// </summary>
public readonly record struct GridAnchor(int HalfX, int HalfY)
{
    public Vector2 ToWorld() =>
        new(HalfX * GameSettings.TileSize / 2f, HalfY * GameSettings.TileSize / 2f);
}
