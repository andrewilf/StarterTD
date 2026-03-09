using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StarterTD.Interfaces;

namespace StarterTD.Entities;

public enum HealingChampionMode
{
    Healing,
    Attack,
}

/// <summary>
/// ChampionHealing-specific behavior.
/// Passive: regenerate 2 HP every full second while damaged.
/// </summary>
public sealed class HealingChampionTower : Tower
{
    private const float PassiveRegenTickSeconds = 1f;
    private const int PassiveRegenAmount = 2;
    private const float HealingModeRange = 0f;
    private const float HealingModeDamage = 0f;
    private const int RailgunShotCount = 5;
    private const float RailgunFireIntervalMultiplier = 1.5f;
    private const float RailgunTargetDamageMultiplier = 1.75f;
    private const float RailgunPierceDamageMultiplier = 0.5f;
    private const float RailgunImpactAoeDamageMultiplier = 0.5f;
    private const float RailgunBeamHitRadius = 10f;
    private static readonly float RailgunBaseDamage = ChampionHealingTowerStats.Stats.Damage;
    private static readonly float RailgunImpactRadius = CannonTowerStats.Stats.AOERadius;
    private static readonly float RailgunImpactRadiusSquared =
        RailgunImpactRadius * RailgunImpactRadius;

    public const float ModeToggleCooldownSeconds = 4f;

    private float _passiveRegenAccumulator;
    private float _modeToggleCooldownRemaining;
    private int _railgunShotsRemaining;

    public HealingChampionMode Mode { get; private set; } = HealingChampionMode.Healing;
    public float ModeToggleCooldownRemaining => _modeToggleCooldownRemaining;
    public int RailgunShotsRemaining => _railgunShotsRemaining;
    public bool IsRailgunEmpowered => _railgunShotsRemaining > 0;

    public HealingChampionTower(Point gridPosition)
        : base(TowerType.ChampionHealing, gridPosition)
    {
        ApplyModeCombatProfile(resetFireCooldown: false);
    }

    public bool TryToggleMode()
    {
        var nextMode =
            Mode == HealingChampionMode.Healing
                ? HealingChampionMode.Attack
                : HealingChampionMode.Healing;
        return TrySetMode(nextMode);
    }

    public bool TrySetMode(HealingChampionMode nextMode)
    {
        if (nextMode == Mode || _modeToggleCooldownRemaining > 0f)
            return false;

        Mode = nextMode;
        if (Mode == HealingChampionMode.Healing)
            CancelRailgunShots();

        ApplyModeCombatProfile(resetFireCooldown: true);
        _modeToggleCooldownRemaining = ModeToggleCooldownSeconds;
        return true;
    }

    public void ActivateRailgunShots()
    {
        if (Mode != HealingChampionMode.Attack || IsDead)
            return;

        _railgunShotsRemaining = RailgunShotCount;
        ResetFireCooldown();
    }

    public void CancelRailgunShots()
    {
        _railgunShotsRemaining = 0;
    }

    protected override void OnUpdateStart(float dt)
    {
        if (_modeToggleCooldownRemaining > 0f)
            _modeToggleCooldownRemaining = MathF.Max(0f, _modeToggleCooldownRemaining - dt);

        if (IsDead)
            return;

        // Do not bank hidden regen while already full.
        if (CurrentHealth >= MaxHealth)
        {
            _passiveRegenAccumulator = 0f;
            return;
        }

        _passiveRegenAccumulator += dt;
        while (_passiveRegenAccumulator >= PassiveRegenTickSeconds)
        {
            _passiveRegenAccumulator -= PassiveRegenTickSeconds;
            Heal(PassiveRegenAmount);

            if (CurrentHealth >= MaxHealth)
            {
                _passiveRegenAccumulator = 0f;
                return;
            }
        }
    }

    protected override float GetNextFireIntervalSeconds()
    {
        if (Mode == HealingChampionMode.Attack && IsRailgunEmpowered)
        {
            float railgunInterval =
                ChampionHealingTowerStats.Stats.FireRate * RailgunFireIntervalMultiplier;
            return MathF.Max(railgunInterval, MinFireIntervalSeconds);
        }

        return base.GetNextFireIntervalSeconds();
    }

    protected override bool TryResolveCustomShot(IEnemy target, List<IEnemy> enemies)
    {
        if (Mode != HealingChampionMode.Attack || !IsRailgunEmpowered)
            return false;

        Vector2 beamStart = WorldPosition;
        Vector2 impactPoint = target.Position;

        target.TakeDamage(RailgunBaseDamage * RailgunTargetDamageMultiplier);

        float beamDamage = RailgunBaseDamage * RailgunPierceDamageMultiplier;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == target || enemy.IsDead || enemy.ReachedEnd)
                continue;

            if (IsPointNearSegment(beamStart, impactPoint, enemy.Position, RailgunBeamHitRadius))
                enemy.TakeDamage(beamDamage);
        }

        float splashDamage = RailgunBaseDamage * RailgunImpactAoeDamageMultiplier;
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == target || enemy.IsDead || enemy.ReachedEnd)
                continue;

            if (Vector2.DistanceSquared(enemy.Position, impactPoint) <= RailgunImpactRadiusSquared)
                enemy.TakeDamage(splashDamage);
        }

        OnAOEImpact?.Invoke(impactPoint, RailgunImpactRadius);
        OnRailgunShot?.Invoke(beamStart, impactPoint);

        _railgunShotsRemaining = Math.Max(0, _railgunShotsRemaining - 1);
        return true;
    }

    private void ApplyModeCombatProfile(bool resetFireCooldown)
    {
        if (Mode == HealingChampionMode.Attack)
        {
            ApplyCombatProfile(
                ChampionHealingTowerStats.Stats.Range,
                ChampionHealingTowerStats.Stats.Damage,
                ChampionHealingTowerStats.Stats.FireRate,
                resetFireCooldown
            );
            return;
        }

        ApplyCombatProfile(
            HealingModeRange,
            HealingModeDamage,
            ChampionHealingTowerStats.Stats.FireRate,
            resetFireCooldown
        );
    }

    private void ApplyCombatProfile(
        float range,
        float damage,
        float fireRate,
        bool resetFireCooldown
    )
    {
        Range = MathF.Max(0f, range);
        Damage = MathF.Max(0f, damage);
        FireRate = MathF.Max(fireRate, MinFireIntervalSeconds);

        if (resetFireCooldown)
            ResetFireCooldown();
    }

    private static bool IsPointNearSegment(
        Vector2 segmentStart,
        Vector2 segmentEnd,
        Vector2 point,
        float maxDistance
    )
    {
        Vector2 segment = segmentEnd - segmentStart;
        float segmentLengthSquared = segment.LengthSquared();
        if (segmentLengthSquared <= float.Epsilon)
            return Vector2.DistanceSquared(point, segmentStart) <= maxDistance * maxDistance;

        float t = Vector2.Dot(point - segmentStart, segment) / segmentLengthSquared;
        t = Math.Clamp(t, 0f, 1f);

        Vector2 projection = segmentStart + segment * t;
        return Vector2.DistanceSquared(point, projection) <= maxDistance * maxDistance;
    }
}
