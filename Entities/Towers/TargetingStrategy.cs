namespace StarterTD.Entities;

/// <summary>
/// Determines how a tower selects its attack target from enemies in range.
/// Assigned per tower type in TowerStats so each type can have distinct aggro behaviour.
/// </summary>
public enum TargetingStrategy
{
    /// <summary>Nearest alive enemy in range. Default for all towers.</summary>
    Closest,

    /// <summary>
    /// Lowest current HP in range.
    /// Prioritises finishing off weak enemies to reduce total threat count quickly.
    /// Used by Gun-type towers.
    /// </summary>
    LowestHP,

    /// <summary>
    /// Enemy with the most other alive enemies within AoERadius around it.
    /// Maximises splash value per shot.
    /// Tie-break: lowest HP.
    /// Used by Cannon-type towers.
    /// </summary>
    MostGrouped,
}
