namespace Defense {
public sealed class BurningTower : StatusEffectTower {
    protected override void ApplyStatusEffect(TargetStatusEffects target, float damage) {
        target.ApplyBurn(StatusToken,Definition.BurnDamagePerSecond * (Definition.Damage > 0 ? damage / Definition.Damage : 1),Definition.BurnDuration);
    }
}
}
