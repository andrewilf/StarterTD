using System;
using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

/// <summary>
/// Placed by walling towers to form a wall network. Has no range and never fires directly.
/// Features unique to this type: progressive max-HP growth while anchored, and decay
/// damage (1 HP/sec per exposed side) when disconnected from any walling anchor.
/// </summary>
public class WallSegmentTower : Tower
{
    // Accumulates fractional decay damage so 1 HP/sec is applied precisely across frames.
    private float _decayAccumulator;

    private bool _wallGrowthInitialized;
    private bool _wallGrowthActive;
    private bool _wallGrowthSyncCurrentWhenUndamaged;
    private int _wallGrowthTargetMaxHealth;
    private float _wallGrowthPerSecond;
    private float _wallGrowthAccumulator;

    public WallSegmentTower(Point gridPosition)
        : base(TowerType.WallSegment, gridPosition) { }

    /// <summary>
    /// Total HP capacity used for health bar rendering.
    /// Returns the growth target cap while growing; falls back to MaxHealth otherwise.
    /// </summary>
    public override int HealthBarCapacity =>
        _wallGrowthInitialized ? _wallGrowthTargetMaxHealth : MaxHealth;

    /// <summary>
    /// Configures progressive max-HP growth. MaxHealth starts at startMaxHealth and grows
    /// toward targetMaxHealth once StartWallGrowth is called.
    /// </summary>
    public void InitializeWallGrowth(
        int startMaxHealth,
        int targetMaxHealth,
        float growthPerSecond,
        bool syncCurrentWhileUndamaged
    )
    {
        int clampedStart = Math.Max(1, startMaxHealth);
        int clampedTarget = Math.Max(clampedStart, targetMaxHealth);

        _wallGrowthInitialized = true;
        _wallGrowthActive = false;
        _wallGrowthSyncCurrentWhenUndamaged = syncCurrentWhileUndamaged;
        _wallGrowthTargetMaxHealth = clampedTarget;
        _wallGrowthPerSecond = Math.Max(0f, growthPerSecond);
        _wallGrowthAccumulator = 0f;

        MaxHealth = clampedStart;
        CurrentHealth = Math.Min(CurrentHealth, MaxHealth);
        if (CurrentHealth <= 0)
            CurrentHealth = MaxHealth;
    }

    public void StartWallGrowth()
    {
        if (!_wallGrowthInitialized || IsDead)
            return;

        if (MaxHealth >= _wallGrowthTargetMaxHealth)
        {
            _wallGrowthActive = false;
            return;
        }

        _wallGrowthActive = true;
    }

    /// <summary>
    /// Stops active wall growth without removing the segment.
    /// Used when the owning walling anchor is removed so normal decay can take over.
    /// </summary>
    public void StopWallGrowth()
    {
        _wallGrowthActive = false;
    }

    public bool IsWallGrowthComplete =>
        !_wallGrowthInitialized || MaxHealth >= _wallGrowthTargetMaxHealth;

    /// <summary>
    /// Accumulates decay damage over time (1 HP/sec for disconnected wall segments).
    /// Uses an accumulator so fractional seconds don't get lost between frames.
    /// </summary>
    public void ApplyDecayDamage(float deltaSeconds)
    {
        _decayAccumulator += deltaSeconds;
        int damage = (int)_decayAccumulator;
        if (damage > 0)
        {
            TakeDamage(damage);
            _decayAccumulator -= damage;
        }
    }

    protected override void OnUpdateStart(float dt)
    {
        if (!_wallGrowthInitialized || !_wallGrowthActive || IsDead || dt <= 0f)
            return;

        if (MaxHealth >= _wallGrowthTargetMaxHealth)
        {
            _wallGrowthActive = false;
            return;
        }

        _wallGrowthAccumulator += dt * _wallGrowthPerSecond;
        int growthAmount = (int)_wallGrowthAccumulator;
        if (growthAmount <= 0)
            return;

        _wallGrowthAccumulator -= growthAmount;

        int previousMax = MaxHealth;
        int nextMax = Math.Min(_wallGrowthTargetMaxHealth, previousMax + growthAmount);
        int appliedGrowth = nextMax - previousMax;
        if (appliedGrowth <= 0)
        {
            _wallGrowthActive = false;
            return;
        }

        MaxHealth = nextMax;

        // Only mirror growth into current HP when the segment was full before this growth step.
        if (_wallGrowthSyncCurrentWhenUndamaged && CurrentHealth == previousMax)
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + appliedGrowth);

        if (MaxHealth >= _wallGrowthTargetMaxHealth)
            _wallGrowthActive = false;
    }
}
