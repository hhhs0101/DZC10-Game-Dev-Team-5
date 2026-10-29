namespace Defense {
public sealed class BurningTower : StatusEffectTower {
    protected override void ApplyEffect(TargetStatusEffects effects, float damage) {
        effects.ApplyBurn(StatusToken,Definition.BurnDamagePerSecond * (Definition.Damage > 0 ? damage / Definition.Damage : 1),Definition.BurnDuration);
    }
}
}
