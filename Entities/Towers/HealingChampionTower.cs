using System;
using Microsoft.Xna.Framework;

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

    public const float ModeToggleCooldownSeconds = 4f;

    private float _passiveRegenAccumulator;
    private float _modeToggleCooldownRemaining;

    public HealingChampionMode Mode { get; private set; } = HealingChampionMode.Healing;
    public float ModeToggleCooldownRemaining => _modeToggleCooldownRemaining;

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
        ApplyModeCombatProfile(resetFireCooldown: true);
        _modeToggleCooldownRemaining = ModeToggleCooldownSeconds;
        return true;
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
}
