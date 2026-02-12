using Microsoft.Xna.Framework;

namespace StarterTD.Engine;

/// <summary>
/// Immutable stat block shared by all tile types.
/// Each tile definition file creates one of these.
/// </summary>
public record TileStats(
    string Name,
    int MovementCost, // Base pathfinding cost (int.MaxValue = impassable)
    Color Color, // Visual display color
    bool IsBuildable // Whether towers can be placed on this tile type
);
