using Microsoft.Xna.Framework;

namespace StarterTD.Entities;

/// <summary>
/// ChampionHealing-specific behavior.
/// Passive: regenerate 2 HP every full second while damaged.
/// </summary>
public sealed class HealingChampionTower : Tower
{
    private const float PassiveRegenTickSeconds = 1f;
    private const int PassiveRegenAmount = 2;
    private float _passiveRegenAccumulator;

    public HealingChampionTower(Point gridPosition)
        : base(TowerType.ChampionHealing, gridPosition) { }

    protected override void OnUpdateStart(float dt)
    {
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
}
