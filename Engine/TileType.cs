namespace StarterTD.Engine;

/// <summary>
/// Represents what a tile on the grid can be.
/// </summary>
public enum TileType
{
    /// <summary>High ground: towers can be placed, but enemies cannot walk through.</summary>
    HighGround,

    /// <summary>Walkable terrain. Enemies can traverse, towers can be placed.</summary>
    Path,

    /// <summary>Impassable and unbuildable rock terrain.</summary>
    Rock,

    /// <summary>High ground variant: identical behaviour to HighGround but uses a different sprite.</summary>
    HighGroundVariant,
}
