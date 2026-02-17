namespace StarterTD.Entities;

/// <summary>
/// State machine states for tower behavior.
/// Active = normal targeting/firing, Moving = walking to a new grid cell,
/// Cooldown = post-move delay before resuming attacks.
/// </summary>
public enum TowerState
{
    Active,
    Moving,
    Cooldown,
}
